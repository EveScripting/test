extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Controllers
{
    public class CourierContractController : BaseController
    {
        #region Constructors

        public CourierContractController()
        {
            IgnorePause = false;
            IgnoreModal = false;
            State.CurrentCourierContractState = CourierContractState.Idle;
        }

        #endregion Constructors

        #region Fields

        public static readonly List<CourierProvider> CourierContractHaulingProviderList = new List<CourierProvider>
        {
            //
            // #haulers in game might have other options to consider
            //
            ESCache.Instance.DirectEve.DirectContract.RF_Freight,
            ESCache.Instance.DirectEve.DirectContract.PUSH_X
        };

        public static readonly List<CourierDestination> CourierDestinationMarketHubList = new List<CourierDestination>
        {
            ESCache.Instance.DirectEve.DirectContract.AMARR,
            ESCache.Instance.DirectEve.DirectContract.DODIXIE,
            ESCache.Instance.DirectEve.DirectContract.HEK,
            ESCache.Instance.DirectEve.DirectContract.JITA,
            ESCache.Instance.DirectEve.DirectContract.RENS
        };

        public static readonly Dictionary<CourierDestination, long> DictCourierDestinationToSystemId = new Dictionary<CourierDestination, long>
        {
            {ESCache.Instance.DirectEve.DirectContract.AMARR, ESCache.Instance.DirectEve.SolarSystems.FirstOrDefault(i => i.Value.Name.ToLower() == "Amarr".ToLower()).Key},
            {ESCache.Instance.DirectEve.DirectContract.DODIXIE, ESCache.Instance.DirectEve.SolarSystems.FirstOrDefault(i => i.Value.Name.ToLower() == "Dodixie".ToLower()).Key},
            {ESCache.Instance.DirectEve.DirectContract.HEK, ESCache.Instance.DirectEve.SolarSystems.FirstOrDefault(i => i.Value.Name.ToLower() == "Hek".ToLower()).Key},
            {ESCache.Instance.DirectEve.DirectContract.JITA, ESCache.Instance.DirectEve.SolarSystems.FirstOrDefault(i => i.Value.Name.ToLower() == "Jita".ToLower()).Key},
            {ESCache.Instance.DirectEve.DirectContract.RENS, ESCache.Instance.DirectEve.SolarSystems.FirstOrDefault(i => i.Value.Name.ToLower() == "Rens".ToLower()).Key}
        };

        private static string _courierContractDestinationStationID;

        private static bool _courierContractForCorp;

        private static string _courierContractHaulingProvider;

        private static int _courierContractMinimumDaysBetweenContracts = 8;

        private static DateTime _lastCourierAction = DateTime.UtcNow;

        private static List<InventoryItem> _listOfItemTypesToCourierContract = new List<InventoryItem>();

        private static long courierContractMaxValuePerContract = 1000000000;

        private static long courierContractMaxValuePerItem = 1600000000;

        private static long courierContractMinimumValuePerContract = 350000000;

        private static int CreateCourierContractAttempts;

        private static bool finishedStep1;

        private static bool finishedStep2;

        private static int numberOfJumpsToDestination;

        private static int reward;

        private List<DirectItem> _itemsToCourierContract = new List<DirectItem>();

        private long contractItemsWillBeWorthIsk;

        #endregion Fields

        #region Properties

        private static CourierProvider CourierContractHaulingProviderA
        {
            get
            {
                try
                {
                    if (CourierContractHaulingProviderList.Count > 0)
                    {
                        foreach (CourierProvider courierContractHaulingProvider in CourierContractHaulingProviderList)
                            if (courierContractHaulingProvider.Name == _courierContractHaulingProvider)
                                return courierContractHaulingProvider;

                        return null;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        private static CourierDestination CourierContractMarketHubDestinationA
        {
            get
            {
                try
                {
                    if (CourierDestinationMarketHubList.Count > 0)
                    {
                        foreach (CourierDestination courierDestinationMarketHub in CourierDestinationMarketHubList)
                            if (courierDestinationMarketHub.Id == double.Parse(_courierContractDestinationStationID))
                                return courierDestinationMarketHub;

                        return null;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        private static bool forCorp
        {
            get
            {
                try
                {
                    if (!DirectNpcInfo.NpcCorpIdsToNames.ContainsKey(ESCache.Instance.EveAccount.MyCorpId))
                    {
                        //
                        // if we are not in an NPC corp then we can possibly create the contract for corp so that when the contract if finished the items will be in corp deliveries
                        //
                        if (_courierContractForCorp)
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        #endregion Properties

        #region Methods

        public static bool ChangeCourierContractState(CourierContractState state)
        {
            try
            {
                if (State.CurrentCourierContractState != state)
                {
                    Log("New CourierContractController State [" + state + "]");
                    State.CurrentCourierContractState = state;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }

        public static void LoadSettings(XElement characterSettingsXml, XElement commonSettingsXml)
        {
            try
            {
                _courierContractHaulingProvider = (string)characterSettingsXml.Element("courierContractHaulingProvider") ??
                                                  (string)commonSettingsXml.Element("courierContractHaulingProvider") ?? null;
                _courierContractDestinationStationID = (string)characterSettingsXml.Element("courierContractDestinationStationID") ??
                                                       (string)commonSettingsXml.Element("courierContractDestinationStationID") ?? null;
                _courierContractMinimumDaysBetweenContracts = (int?)characterSettingsXml.Element("courierContractMinimumDaysBetweenContracts") ??
                                                              (int?)commonSettingsXml.Element("courierContractMinimumDaysBetweenContracts") ?? 6;
                _courierContractForCorp = (bool?)characterSettingsXml.Element("createCourierContractForCorp") ??
                                          (bool?)commonSettingsXml.Element("createCourierContractForCorp") ?? false;
                courierContractMaxValuePerContract = (long?)characterSettingsXml.Element("courierContractMaxValuePerContract") ??
                                                     (long?)commonSettingsXml.Element("courierContractMaxValuePerContract") ?? 1000000000;
                courierContractMaxValuePerItem = (long?)characterSettingsXml.Element("courierContractMaxValuePerItem") ??
                                                 (long?)commonSettingsXml.Element("courierContractMaxValuePerItem") ?? 1600000000;
                courierContractMinimumValuePerContract = (long?)characterSettingsXml.Element("courierContractMinimumValuePerContract") ??
                                                         (long?)commonSettingsXml.Element("courierContractMinimumValuePerContract") ?? 350000000;

                //
                // Item types to put into a courier contract
                //
                _listOfItemTypesToCourierContract = new List<InventoryItem>();
                XElement xmlListOfItemsToCourierContract = characterSettingsXml.Element("itemsToCourierContract") ?? commonSettingsXml.Element("itemsToCourierContract");
                if (xmlListOfItemsToCourierContract != null)
                {
                    int itemNum = 0;
                    foreach (XElement item in xmlListOfItemsToCourierContract.Elements("itemToCourierContract"))
                    {
                        itemNum++;
                        InventoryItem itemTypeToCourierContract = new InventoryItem(item);
                        Logging.Log.WriteLine("ListOfItemsToCourierContract: [" + itemNum + "][" + itemTypeToCourierContract.Name + "][" + itemTypeToCourierContract.TypeId + "][" + itemTypeToCourierContract.Quantity + "]");
                        _listOfItemTypesToCourierContract.Add(itemTypeToCourierContract);
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        public override void DoWork()
        {
            try
            {
                ProcessState();
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        public void Main()
        {
            try
            {
                if (Time.Instance.NextBuyLpItemAction > DateTime.UtcNow) return;
                if (ESCache.Instance.InSpace) return;
                if (!ESCache.Instance.InStation) return;
                if (Time.Instance.LastInWarp.AddSeconds(20) > DateTime.UtcNow) return;
                if (ESCache.Instance.ItemHangar == null) return;
                if (ESCache.Instance.ItemHangar.Items == null) return;

                //
                // do we have "all" of the items
                //
                // Fed Frog Cost Calc: http://red-frog.org/jumps.php
                //

                if (!ESCache.Instance.DirectEve.DirectContract.IsPageInfoLoaded())
                {
                    ESCache.Instance.DirectEve.DirectContract.LoadPageInfo();
                    return;
                }

                int contractsLeftToUse = ESCache.Instance.DirectEve.DirectContract.GetNumContractsLeft();
                if (contractsLeftToUse == -1) return;
                if (contractsLeftToUse == 0)
                {
                    Log("CourierContractController: We have no contracts available");
                    ChangeCourierContractState(CourierContractState.Done);
                    return;
                }

                bool? boolDoWeHaveEnoughItemsToCreateACourierContract = null;
                boolDoWeHaveEnoughItemsToCreateACourierContract = DoWeHaveEnoughItemsToCreateACourierContract();
                if (boolDoWeHaveEnoughItemsToCreateACourierContract == null)
                    return;

                if (boolDoWeHaveEnoughItemsToCreateACourierContract != null && (bool)!boolDoWeHaveEnoughItemsToCreateACourierContract)
                {
                    Log("CourierContractController: We do not yet have 500mil of items to create a courier contract yet.");
                    ChangeCourierContractState(CourierContractState.Done);
                    return;
                }

                if (CourierContractMarketHubDestinationA == null)
                {
                    Log("CourierContractController: Unable to find a defined CourierDestination with stationId of [" + _courierContractDestinationStationID + "] we currently only support have JITA/AMARR/RENS/DODIXIE/HEK. more can be added if needed");
                    ChangeCourierContractState(CourierContractState.Done);
                    return;
                }

                if (!DoWeHaveEnoughIskToAffordTheCourierContractFees())
                {
                    Log("CourierContractController: We do not yet have enough ISK to create the courier contract.");
                    ChangeCourierContractState(CourierContractState.Done);
                    return;
                }

                if (CourierContractHaulingProviderA == null)
                {
                    Log("CourierContractController: Unable to find a defined CourierProvider named [" + _courierContractHaulingProvider + "] we currently only support [ Red Frog Freight ] and [ Push Industries ]. more can be added if needed");
                    ChangeCourierContractState(CourierContractState.Done);
                    return;
                }

                //
                // Is the Create Contract Window Ooen? If not, start creating the contract (same as chosing the items, right click create contract)
                //
                if (!ESCache.Instance.DirectEve.DirectContract.IsCreateContractWindowOpen)
                {
                    Log("CourierContractController: Creating Contract with [" + _itemsToCourierContract.Count + "] Items. Contact Value: [" + contractItemsWillBeWorthIsk + "]");
                    ESCache.Instance.DirectEve.DirectContract.CreateContract(_itemsToCourierContract);
                    return;
                }

                if (ESCache.Instance.DirectEve.DirectContract.IsCreateContractWindowOpen)
                {
                    //
                    // If the Create Contract Window is open start filling out the info...
                    //

                    Log("CourierContractController: forCorp [" + forCorp + "]");
                    int collateral = (int)Math.Min(contractItemsWillBeWorthIsk, 1000000000);
                    Log("CourierContractController: collateral [" + collateral + "]");
                    int durationDays = 1;
                    if (numberOfJumpsToDestination >= 1 && reward > 0 && collateral > 100000000)
                    {
                        Log("CourierContractController: Setting Contract to be a Courier Contract");
                        State.CurrentCourierContractState = CourierContractState.FinishCourierContract;
                        ESCache.Instance.DirectEve.DirectContract.SetCourierContract(reward, collateral, durationDays, ExpireTime.ONE_WEEK, CourierContractMarketHubDestinationA, CourierContractHaulingProviderA, forCorp);
                        return;
                    }
                }

                //
                // We couldnt create the contract?
                //
                Log("CourierContractController: Unable to setup the contract with the above values. ");
                State.CurrentCourierContractState = CourierContractState.Done;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        public void ProcessState()
        {
            switch (State.CurrentCourierContractState)
            {
                case CourierContractState.Idle:
                    finishedStep1 = false;
                    finishedStep2 = false;
                    Log("CourierContractController: LastCreateContract [" + ESCache.Instance.EveAccount.LastCreateContract.ToShortDateString() + "]@[" + ESCache.Instance.EveAccount.LastCreateContract.ToShortTimeString() + "]");
                    Log("CourierContractController: LastCreateContractAttempt [" + ESCache.Instance.EveAccount.LastCreateContractAttempt.ToShortDateString() + "]@[" + ESCache.Instance.EveAccount.LastCreateContractAttempt.ToShortTimeString() + "]");
                    Log("CourierContractController: Current Time [" + DateTime.UtcNow.ToShortDateString() + "]@[" + DateTime.UtcNow.ToShortTimeString() + "] LastCreateContractAttempt [" + ESCache.Instance.EveAccount.LastCreateContractAttempt.ToShortDateString() + "]@[" + ESCache.Instance.EveAccount.LastCreateContractAttempt.ToShortTimeString() + "]");
                    if (DateTime.UtcNow > ESCache.Instance.EveAccount.LastCreateContract.AddDays(_courierContractMinimumDaysBetweenContracts) && DateTime.UtcNow > ESCache.Instance.EveAccount.LastCreateContractAttempt.AddHours(2))
                    {
                        Log("CourierContractController: It is time to try to make another courier contract.");
                        ChangeCourierContractState(CourierContractState.PrepareAndCalcToPrepareForCourierContract);
                        return;
                    }

                    Log("CourierContractController: It is not yet time to try to make another courier contract: done.");
                    ChangeCourierContractState(CourierContractState.Done);
                    return;

                case CourierContractState.PrepareAndCalcToPrepareForCourierContract:
                    Main();
                    return;

                case CourierContractState.FinishCourierContract:
                    if (!FinishCourierContract()) return;
                    State.CurrentCourierContractState = CourierContractState.Done;
                    return;

                case CourierContractState.Done:
                    WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LastCreateContractAttempt), DateTime.UtcNow);
                    ControllerManager.Instance.RemoveController(typeof(CourierContractController));
                    return;
            }
        }

        private static bool FinishCourierContract()
        {
            try
            {
                if (ESCache.Instance.DirectEve.DirectContract.CanFinishCourierContract)
                {
                    if (!finishedStep1)
                        finishedStep1 = ESCache.Instance.DirectEve.DirectContract.FinishStep1();
                    if (!finishedStep2)
                        finishedStep2 = ESCache.Instance.DirectEve.DirectContract.FinishStep2();

                    if (finishedStep1 && finishedStep2)
                        if (DateTime.UtcNow > _lastCourierAction.AddSeconds(10) && CreateCourierContractAttempts < 4)
                        {
                            CreateCourierContractAttempts++;
                            _lastCourierAction = DateTime.UtcNow;
                            if (true) return false; //disabled for now: DirectEve.DirectContract.CreateContract() does not create the contract, though it does look like its trying.
                            bool boolCreateContract = ESCache.Instance.DirectEve.DirectContract.CreateContract();
                            if (!boolCreateContract)
                            {
                                Log("CourierContractController: Called [ DirectContract.CreateContract() ]");
                                return false;
                            }
                            Log("CourierContractController: Courier Contract Created.");
                            WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LastCreateContract), DateTime.UtcNow);
                            return true;
                        }

                    Log("CourierContractController: finishedStep1 [" + finishedStep1 + "] finishedStep2 [" + finishedStep2 + "]");
                    return false;
                }

                Log("CanFinishCourierContract [" + ESCache.Instance.DirectEve.DirectContract.CanFinishCourierContract + "]");
                return false;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }

        private bool DoWeHaveEnoughIskToAffordTheCourierContractFees()
        {
            try
            {
                long destSolarsystemId = 0;
                DictCourierDestinationToSystemId.TryGetValue(CourierContractMarketHubDestinationA, out destSolarsystemId);
                if (destSolarsystemId != 0)
                {
                    numberOfJumpsToDestination = ESCache.Instance.DirectEve.GetDistanceBetweenSolarsystems((int)ESCache.Instance.DirectEve.Session.SolarSystemId, (int)destSolarsystemId);
                    Log("CourierContractController: numberOfJumpsToDestination [" + numberOfJumpsToDestination + "][" + CourierContractMarketHubDestinationA.Name + "]");
                    //
                    // http://api.red-frog.org/
                    // https://api.pushx.net/
                    //
                    //25 jumps = 41,000,000
                    //24 jumps = 39,500,000
                    //19 jumps = 32,000,000
                    //25 / 41 = 1,640,000 per jump
                    reward = (int)Math.Round((double)(1640000 * numberOfJumpsToDestination) / 500000, 0) * 500000;
                    Log("CourierContractController: reward [" + reward + "]");
                }

                if (ESCache.Instance.DirectEve.Me.Wealth == null || (ESCache.Instance.DirectEve.Me.Wealth ?? 0) > reward)
                    return true;

                Log("CourierContractController: Not enough ISK: | Wallet [ " + ESCache.Instance.DirectEve.Me.Wealth ?? 0 + " ] | Needed [" + reward + "]");
                return false;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }

        private bool? DoWeHaveEnoughItemsToCreateACourierContract()
        {
            try
            {
                _itemsToCourierContract = new List<DirectItem>();
                if (ESCache.Instance.ItemHangar.Items.Count > 0)
                {
                    if (_listOfItemTypesToCourierContract != null && _listOfItemTypesToCourierContract.Count > 0)
                    {
                        Log("CourierContractController: _listOfItemTypesToCourierContract [" + _listOfItemTypesToCourierContract.Count + "]");
                        contractItemsWillBeWorthIsk = 0;
                        foreach (InventoryItem itemTypeToCourierContract in _listOfItemTypesToCourierContract.OrderByDescending(i => i.SellOrderValue))
                        {
                            Log("CourierContractController: itemTypeToCourierContract [" + itemTypeToCourierContract.Name + "][" + itemTypeToCourierContract.TypeId + "] Quantity [" + itemTypeToCourierContract.Quantity + "] Priority [" + itemTypeToCourierContract.Priority + "]");
                            if (ESCache.Instance.ItemHangar.Items.Any(i => i.TypeId == itemTypeToCourierContract.TypeId))
                            {
                                Log("CourierContractController: Items In Hangar that match items we want to contract [" + ESCache.Instance.ItemHangar.Items.Any(i => i.TypeId == itemTypeToCourierContract.TypeId) + "]");
                                foreach (DirectItem itemToAdd in ESCache.Instance.ItemHangar.Items.Where(i => i.TypeId == itemTypeToCourierContract.TypeId))
                                {
                                    Log("CourierContractController: itemToAdd [" + itemToAdd.TypeName + "][" + itemToAdd.TypeId + "][" + itemToAdd.Quantity + "] - Deciding if we can add it");
                                    if (_itemsToCourierContract.Count == 0)
                                        if (itemTypeToCourierContract.SellOrderValue > courierContractMaxValuePerItem)
                                        {
                                            Log("CourierContractController: itemToAdd [" + itemToAdd.TypeName + "][" + itemToAdd.TypeId + "][" + itemToAdd.Quantity + "] is the first item in the contract is is worth less than [" + courierContractMaxValuePerItem + " isk]");
                                            _itemsToCourierContract.Add(itemToAdd);
                                            contractItemsWillBeWorthIsk += itemTypeToCourierContract.SellOrderValue * itemToAdd.Quantity;
                                            return true;
                                        }

                                    if (contractItemsWillBeWorthIsk + (long)itemTypeToCourierContract.SellOrderValue * itemToAdd.Quantity < courierContractMaxValuePerContract)
                                    {
                                        if (_itemsToCourierContract.Select(i => i.Volume).Sum() < 850000)
                                        {
                                            Log("CourierContractController: itemToAdd [" + itemToAdd.TypeName + "][" + itemToAdd.TypeId + "][" + itemToAdd.Quantity + "] adding");
                                            _itemsToCourierContract.Add(itemToAdd);
                                            contractItemsWillBeWorthIsk += itemTypeToCourierContract.SellOrderValue * itemToAdd.Quantity;
                                            Log("CourierContractController: Contract now contains [" + _itemsToCourierContract.Count + "] items [" + Math.Round(_itemsToCourierContract.Select(i => i.Volume).Sum(), 0) + "] m3 and [" + contractItemsWillBeWorthIsk + "] isk of items.");
                                            if (contractItemsWillBeWorthIsk > courierContractMaxValuePerContract)
                                                return true;

                                            continue;
                                        }

                                        Log("CourierContractController: Contract will contain [" + Math.Round(_itemsToCourierContract.Select(i => i.Volume).Sum(), 0) + "] m3 of items. done adding items");
                                        break;
                                    }
                                }

                                //return false;
                            }

                            //
                            // we have none of this item
                            //
                            //return false;
                        }

                        //
                        // we must have enough of all the items
                        //
                        if (contractItemsWillBeWorthIsk > courierContractMinimumValuePerContract)
                        {
                            Log("CourierContractController: Contracted items are worth [" + Math.Round((double)contractItemsWillBeWorthIsk, 0) + "] and minimum value to create a contract is set to [" + courierContractMinimumValuePerContract + "] isk.");
                            return true;
                        }

                        Log("CourierContractController: Contracted items are worth [" + Math.Round((double)contractItemsWillBeWorthIsk, 0) + "] isk. aborting creating a contract for now.");
                        return false;
                    }

                    //
                    // we have no items to contract defined
                    //
                    Log("CourierContractController: _listOfItemTypesToCourierContract [ 0 ]");
                    return false;
                }

                //
                // we have no items in the hangar
                //
                Log("CourierContractController: we have no items in the itemhangar");
                return null;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return null;
            }
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}