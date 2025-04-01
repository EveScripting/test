extern alias SC;

using EVESharpCore.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers;
using EVESharpCore.Framework;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Traveller;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using EVESharpCore.Controllers.Abyssal;
using System.Xml.Linq;

namespace EVESharpCore.Questor.Behaviors
{
    public class TransportItemTypesBehavior
    {
        public TransportItemTypesBehavior()
        {
        }

        public static XElement xmlTransportItemIDs = null;
        public static XElement xmlTransportItemGroupIDs = null;
        public static XElement xmlTransportItemCategoryIDs = null;

        public static List<int> ListOfTypeIDs = new List<int>();
        public static List<int> ListOfGroupIDs = new List<int>();
        public static List<int> ListOfCategoryIDs = new List<int>();

        public static string FromStationBookmarkName = "HaulFrom";
        public static string ToStationBookmarkName = "HaulTo";

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            try
            {
                Log.WriteLine("LoadSettings: TransportItemTypes");

                //HomeBookmarkName =
                //    (string)CharacterSettingsXml.Element("HomeBookmarkName") ?? (string)CharacterSettingsXml.Element("HomeBookmarkName") ??
                //    (string)CommonSettingsXml.Element("HomeBookmarkName") ?? (string)CommonSettingsXml.Element("HomeBookmarkName") ?? "HomeBookmarkName";
                //Log.WriteLine("LoadSettings: IndustryBehavior: HomeBookmarkName [" + HomeBookmarkName + "]");
                /**
                AllowMiningInAsteroidBelts =
                    (bool?)CharacterSettingsXml.Element("allowMiningInAsteroidBelts") ??
                    (bool?)CommonSettingsXml.Element("allowMiningInAsteroidBelts") ?? true;
                Log.WriteLine("LoadSettings: MiningBehavior: allowMiningInAsteroidBelts [" + AllowMiningInAsteroidBelts + "]");
                AllowMiningInMiningAnomolies =
                    (bool?)CharacterSettingsXml.Element("allowMiningInMiningAnomolies") ??
                    (bool?)CommonSettingsXml.Element("allowMiningInMiningAnomolies") ?? true;
                Log.WriteLine("LoadSettings: MiningBehavior: allowMiningInMiningAnomolies [" + AllowMiningInMiningAnomolies + "]");
                AllowMiningInMiningSignatures =
                    (bool?)CharacterSettingsXml.Element("allowMiningInMiningSignatures") ??
                    (bool?)CommonSettingsXml.Element("allowMiningInMiningSignatures") ?? true;
                Log.WriteLine("LoadSettings: MiningBehavior: allowMiningInMiningSignatures [" + AllowMiningInMiningSignatures + "]");
                **/

                ListOfTypeIDs = new List<int>();
                ListOfGroupIDs = new List<int>();
                ListOfCategoryIDs = new List<int>();

                try
                {
                    xmlTransportItemIDs = Settings.CharacterSettingsXml.Element("TransportItemTypeIDs") ?? Settings.CommonSettingsXml.Element("TransportItemTypeIDs");
                    //check if the xmlTransportItemIDs is not null and not empty
                    if (xmlTransportItemIDs != null && !xmlTransportItemIDs.IsEmpty)
                    {
                        if (xmlTransportItemIDs.Elements("TransportItemTypeID") != null)
                        {
                            foreach (XElement xmlIndividualTransportItemTypeID in xmlTransportItemIDs.Elements("TransportItemTypeID"))
                            {
                                try
                                {
                                    DirectItem individualItemTypeIDToMove = new DirectItem(ESCache.Instance.DirectEve);
                                    individualItemTypeIDToMove.TypeId = (int)xmlIndividualTransportItemTypeID.Attribute("typeId");
                                    Log.WriteLine("Adding TypeID to Move [" + individualItemTypeIDToMove.TypeName + "] TypeId [" + individualItemTypeIDToMove.TypeId + "]");
                                    ListOfTypeIDs.Add(individualItemTypeIDToMove.TypeId);
                                }
                                catch (Exception ex)
                                {
                                    Log.WriteLine("Exception [" + ex + "]");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                try
                {
                    xmlTransportItemGroupIDs = Settings.CharacterSettingsXml.Element("TransportItemGroupIDs") ?? Settings.CommonSettingsXml.Element("TransportItemGroupIDs");
                    if (xmlTransportItemGroupIDs != null)
                    {
                        if (xmlTransportItemGroupIDs.Elements("TransportItemGroupID") != null)
                        {
                            foreach (XElement xmlIndividualTransportItemGroupID in xmlTransportItemGroupIDs.Elements("TransportItemGroupID"))
                            {
                                try
                                {
                                    //DirectItem individualItemGroupIDToMove = new DirectItem(ESCache.Instance.DirectEve);
                                    //individualItemGroupIDToMove.TypeId = (int)xmlTransportItemGroupIDs.Attribute("typeId");
                                    Log.WriteLine("Adding GroupID to move [" + xmlIndividualTransportItemGroupID.Attribute("Description") + "] GroupID [" + xmlIndividualTransportItemGroupID.Attribute("GroupID") + "]");
                                    ListOfGroupIDs.Add((int)xmlIndividualTransportItemGroupID.Attribute("GroupID"));
                                }
                                catch (Exception ex)
                                {
                                    Log.WriteLine("Exception [" + ex + "]");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }

                try
                {
                    xmlTransportItemCategoryIDs = Settings.CharacterSettingsXml.Element("TransportItemCategoryIDs") ?? Settings.CommonSettingsXml.Element("TransportItemCategoryIDs");
                    if (xmlTransportItemCategoryIDs != null)
                    {
                        if (xmlTransportItemCategoryIDs.Elements("TransportItemCategoryID") != null)
                        {
                            foreach (XElement xmlIndividualTransportItemCategoryID in xmlTransportItemCategoryIDs.Elements("TransportItemCategoryID"))
                            {
                                try
                                {
                                    //DirectItem individualItemGroupIDToMove = new DirectItem(ESCache.Instance.DirectEve);
                                    //individualItemGroupIDToMove.TypeId = (int)xmlTransportItemGroupIDs.Attribute("typeId");
                                    Log.WriteLine("Adding CategotyID to move [" + xmlIndividualTransportItemCategoryID.Attribute("Description") + "] CategoryID [" + xmlIndividualTransportItemCategoryID.Attribute("CategoryID") + "]");
                                    ListOfCategoryIDs.Add((int)xmlIndividualTransportItemCategoryID.Attribute("CategoryID"));
                                }
                                catch (Exception ex)
                                {
                                    Log.WriteLine("Exception [" + ex + "]");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        private static DateTime nextAction = DateTime.MinValue;

        public static TransportItemTypesBehaviorState CurrentTransportItemTypesBehaviorState { get; set; } // idle == default

        public static bool ChangeTransportItemTypesBehaviorState(TransportItemTypesBehaviorState StateToSet, bool wait = false)
        {
            try
            {
                if (CurrentTransportItemTypesBehaviorState != StateToSet)
                {
                    Log.WriteLine("New TransportHangarToMarketBehaviorState [" + StateToSet + "]");
                    CurrentTransportItemTypesBehaviorState = StateToSet;
                    if (!wait) ProcessState();
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }

            return true;
        }

        private static List<DirectItem> _stuffToMove = new List<DirectItem>();


        public static double TooMuchISKToMove = 400000000; //400m isk

        public static double NumOfItemsToMove(DirectItem itemInHangar)
        {
            //fixme

            if (itemInHangar == null)
                return 0;

            if (itemInHangar.Quantity == 0)
                return 0;

            //How many items by volume can we fit in CurrentShipsCargo?
            double numOfItemsToMove = (int)(ESCache.Instance.CurrentShipsCargo.FreeCapacity / itemInHangar.Volume);
            //round this down to the nearest whole number
            numOfItemsToMove = Math.Floor(numOfItemsToMove);

            //
            // Can we check the value of these items and not move them if they are above a cost threshold?
            // We may need to split the stack into smaller chunks (less value)
            double ValueOfThisStack = itemInHangar.AveragePrice() * numOfItemsToMove;
            if (ValueOfThisStack > TooMuchISKToMove)
            {
                Log.WriteLine("ValueOfThisStack [" + ValueOfThisStack + "] is greater than [" + TooMuchISKToMove + "] isk");
                if (numOfItemsToMove > 1)
                {
                    numOfItemsToMove = Math.Round(numOfItemsToMove / 2, 0);
                    ValueOfThisStack = itemInHangar.AveragePrice() * numOfItemsToMove;
                    if (ValueOfThisStack > TooMuchISKToMove)
                    {
                        Log.WriteLine("ValueOfThisStack [" + ValueOfThisStack + "] is greater than [" + TooMuchISKToMove + "] isk");
                        if (numOfItemsToMove > 1)
                        {
                            numOfItemsToMove = Math.Round(numOfItemsToMove / 2, 0);
                            ValueOfThisStack = itemInHangar.AveragePrice() * numOfItemsToMove;
                            Log.WriteLine("ValueOfThisStack [" + ValueOfThisStack + "] is greater than [" + TooMuchISKToMove + "] isk");
                            if (ValueOfThisStack > TooMuchISKToMove)
                            {
                                Log.WriteLine("ValueOfThisStack [" + ValueOfThisStack + "] is greater than [" + TooMuchISKToMove + "] isk");
                                if (numOfItemsToMove > 1)
                                {
                                    numOfItemsToMove = Math.Round(numOfItemsToMove / 2, 0);
                                    ValueOfThisStack = itemInHangar.AveragePrice() * numOfItemsToMove;
                                }
                            }
                        }
                    }
                }
            }

            return 1;
        }

        public static void ProcessState()
        {
            if (nextAction > DateTime.UtcNow)
                return;

            switch (CurrentTransportItemTypesBehaviorState)
            {
                case TransportItemTypesBehaviorState.Idle:
                    _stuffToMove = new List<DirectItem>();
                    ChangeTransportItemTypesBehaviorState(TransportItemTypesBehaviorState.ActivateTransportShip, false);
                    break;

                case TransportItemTypesBehaviorState.Prerequisites:
                    // Do we require a BlockadeRunner?
                    // How we we guarantee we have one? by groupID?

                    // What system is considered Market? Jita? Other?

                    // Do we have a clear path to Market? with no low sec?
                    //

                    // Is it safe to travel to Market?
                    // Any Wars?
                    // Any PVP Timers?
                    // Any kill rights?

                    // Do we have enough value in the loot hangar to justify a trip to market?
                    //
                    break;

                case TransportItemTypesBehaviorState.ActivateTransportShip:
                    ActivateTransportShip();
                    break;

                case TransportItemTypesBehaviorState.CalculateItemsToMove:
                    if (!ESCache.Instance.InStation)
                        return;

                    if (ESCache.Instance.ItemHangar == null) return;

                    if (ESCache.Instance.ItemHangar.Items.Count == 0)
                        ChangeTransportItemTypesBehaviorState(TransportItemTypesBehaviorState.LoadItems);

                    //
                    // go through the itemhangar and filter the items we want to move
                    //

                    foreach (DirectItem itemInHangar in ESCache.Instance.ItemHangar.Items.Where(x => ListOfTypeIDs.Any(i => i == x.TypeId)))
                    {
                        //if free capacity of cargo is less than 50m3, stop adding items
                        if (ESCache.Instance.CurrentShipsCargo.FreeCapacity < 5)
                        {
                            ChangeTransportItemTypesBehaviorState(TransportItemTypesBehaviorState.TravelToToLocation);
                            return;
                        }

                        if (ESCache.Instance.CurrentShipsCargo.FreeCapacity > itemInHangar.TotalVolume)
                        {
                            //
                            // Can we check the value of these items and not move them if they are above a cost threshold?
                            //
                            ESCache.Instance.ItemHangar.Add(itemInHangar);
                            continue;
                        }

                        //
                        // if the stack of itemInHangar is more m3 than ESCache.Instance.CurrentShipsCargo.FreeCapacity
                        // split the stack and add the smaller stack to the cargo
                        //
                        if (itemInHangar.Quantity > 1)
                        {
                            //...

                            //NumOfItemsToMove = Math.Floor(NumOfItemsToMove);
                            //ESCache.Instance.ItemHangar.Add(itemInHangar, (int)numOfItemsToMove);
                            return;
                        }

                        continue;
                    }

                    ChangeTransportItemTypesBehaviorState(TransportItemTypesBehaviorState.LoadItems);

                    break;

                case TransportItemTypesBehaviorState.LoadItems:
                    if (!ESCache.Instance.InStation)
                        return;

                    if (!ESCache.Instance.CurrentShipsCargo.IsReady)
                        return;

                    if (ESCache.Instance.LootHangar == null)
                        return;

                    //LoadItemsToHaul.MoveToCargoList

                    if (ESCache.Instance.CurrentShipsCargo.FreeCapacity >= _stuffToMove.Sum(i => i.TotalVolume))
                    {
                        if (ESCache.Instance.CurrentShipsCargo.Add(_stuffToMove))
                        {
                            ChangeTransportItemTypesBehaviorState(TransportItemTypesBehaviorState.WaitForItemsToMove);
                            return;
                        }
                    }

                    break;

                case TransportItemTypesBehaviorState.WaitForItemsToMove:
                    if (!ESCache.Instance.CurrentShipsCargo.IsReady)
                        return;

                    ChangeTransportItemTypesBehaviorState(TransportItemTypesBehaviorState.TravelToToLocation);
                    break;

                case TransportItemTypesBehaviorState.TravelToToLocation:

                    if (ESCache.Instance.InSpace && ESCache.Instance.MyShipEntity != null && ESCache.Instance.MyShipEntity.HasInitiatedWarp)
                        return;

                    Traveler.ProcessState();

                    if (State.CurrentTravelerState == TravelerState.AtDestination)
                    {
                        Log.WriteLine("Arrived at destination");
                        Traveler.Destination = null;
                        return;
                    }

                    if (State.CurrentTravelerState == TravelerState.Error)
                    {
                        if (Traveler.Destination != null)
                            Log.WriteLine("Stopped traveling, traveller threw an error...");

                        Traveler.Destination = null;
                    }

                    break;

                case TransportItemTypesBehaviorState.Error:
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.QuestorController))
                        CombatMissionsBehavior.ChangeCombatMissionBehaviorState(CombatMissionsBehaviorState.DelayedGotoBase, true, null);
                    Arm.ChangeArmState(ArmState.Idle, true, null);
                    Log.WriteLine("ERROR. BuyAmmo should stay disabled while this session is still active.");
                    ControllerManager.Instance.RemoveController(typeof(BuyItemsController));
                    break;
            }
        }

        private static void ActivateTransportShip()
        {
            if (!ESCache.Instance.InStation)
                return;


            if (ESCache.Instance.DirectEve.GetShipHangar() == null)
            {
                Log.WriteLine("Shiphangar is null.");
                return;
            }

            if (ESCache.Instance.DirectEve.GetItemHangar() == null)
            {
                Log.WriteLine("ItemHangar is null.");
                return;
            }

            var ShipsWithCorrectTransportShipName = ESCache.Instance.DirectEve.GetShipHangar().Items.Where(i => i.IsSingleton
                                                             && i.GivenName != null
                                                             && i.GivenName == Settings.Instance.TransportShipName).ToList();
            if (ESCache.Instance.ActiveShip == null)
            {
                Log.WriteLine("Active ship is null.");
                return;
            }

            Log.WriteLine("ActiveShip is [" + ESCache.Instance.ActiveShip.GivenName + "] TypeID [" + ESCache.Instance.ActiveShip.TypeId + "]");

            if (ESCache.Instance.ActiveShip.GivenName == Settings.Instance.TransportShipName)
            {
                ChangeTransportItemTypesBehaviorState(TransportItemTypesBehaviorState.CalculateItemsToMove);
                Log.WriteLine("We are in our transport ship now.");
                return;
            }

            if (ShipsWithCorrectTransportShipName.Any())
            {
                var transportShip = ShipsWithCorrectTransportShipName.OrderByDescending(i => i.GroupId == (int)Group.TransportShip).FirstOrDefault();
                if (transportShip != null)
                {
                    transportShip.ActivateShip();
                    Log.WriteLine("Found our transport ship named [" + transportShip.GivenName + "]. Making it active.");
                    //LocalPulse = DateTime.UtcNow.AddSeconds(Time.Instance.SwitchShipsDelay_seconds);
                    return;
                }
                else Log.WriteLine("if (transportShip == null) !");
            }
            else
            {
                Log.WriteLine("TransportShipName [" + Settings.Instance.TransportShipName + "] was not found in Ship Hangar");
                ControllerManager.Instance.SetPause(true);
            }
        }
    }
}