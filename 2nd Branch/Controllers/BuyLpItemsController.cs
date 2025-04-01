extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Buy LP Items using the character or common XML settings:
    ///     Quantity is the number we want ot have in our hangar, if we do not have this many we will buy enough to reach that
    ///     amount up to the limits of ISK/LP/and input items
    ///     Note: this can work in conjunction with
    ///     <itemsToKeepInStock>
    ///         to bring the items needed as inputs when buying ammo.
    ///         <itemsToBuyFromLpStore>
    ///             <itemToBuyFromLpStore description="Thukker Large Shield Extender" typeId="28744" quantity="2" />
    ///         </itemsToBuyFromLpStore>
    /// </summary>
    public class BuyLpItemsController : BaseController
    {
        #region Constructors

        public BuyLpItemsController()
        {
            IgnorePause = false;
            IgnoreModal = false;
            State.CurrentBuyLpItemsState = BuyLpItemsState.Idle;
        }

        #endregion Constructors

        #region Fields

        private double? _howManyLpOrdersShouldWeBuy;

        private double? _itemRequirement1LetsUsBuildThisMany;

        private double? _itemRequirement2LetsUsBuildThisMany;

        private double? _itemRequirement3LetsUsBuildThisMany;

        private double? _itemRequirement4LetsUsBuildThisMany;

        private double? _itemRequirement5LetsUsBuildThisMany;

        private double? _itemRequirementsAllowUsToBuyThisManyLpOrders;

        #endregion Fields

        #region Methods

        public static bool ChangeBuyLpItemsState(BuyLpItemsState state)
        {
            try
            {
                if (State.CurrentBuyLpItemsState != state)
                {
                    Log("New ArmState [" + state + "]");
                    State.CurrentBuyLpItemsState = state;
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }

        public void BuyLpItems()
        {
            try
            {
                if (Time.Instance.NextBuyLpItemAction > DateTime.UtcNow) return;
                if (ESCache.Instance.InSpace) return;
                if (!ESCache.Instance.InStation) return;
                if (Time.Instance.LastInWarp.AddSeconds(20) > DateTime.UtcNow) return;
                if (ESCache.Instance.AmmoHangar == null) return;
                if (ESCache.Instance.AmmoHangar.Items == null) return;
                if (ESCache.Instance.DirectEve.Me.Wealth == null) return;

                if (ESCache.Instance.LpStore != null)
                {
                    long loyaltyPointsLeftToSpend = ESCache.Instance.LpStore.LoyaltyPoints;
                    Log("Faction [" + MissionSettings.AgentToPullNextRegularMissionFrom.FactionName + "] LoyaltyPoints [" + loyaltyPointsLeftToSpend.ToString("N0") + "] Current Wallet Balance [" + Math.Round((double)(ESCache.Instance.DirectEve.Me.Wealth ?? 0), 0).ToString("N0") + "isk ]");
                    if (ESCache.Instance.DirectEve.Me.Wealth == null)
                    {
                        Log("LPStore: WalletBalance is null, we cannot buy anything from the LPStore without ISK");
                        return;
                    }

                    if (ESCache.Instance.DirectEve.Me.Wealth == 0)
                    {
                        Log("LPStore: WalletBalance is 0, we cannot buy anything from the LPStore without ISK");
                        return;
                    }

                    if (Settings.Instance.ListOfItemsToBuyFromLpStore.Count > 0)
                    {
                        foreach (InventoryItem itemToBuyFromLpStore in Settings.Instance.ListOfItemsToBuyFromLpStore.OrderBy(item => item.Priority))
                        {
                            Log("LPStore: ItemToBuy [" + itemToBuyFromLpStore.Name + "] TypeId [" + itemToBuyFromLpStore.TypeId + "] Quantity [" + itemToBuyFromLpStore.Quantity + "] Priority [" + itemToBuyFromLpStore.Priority + "] LPStore Offers [" + ESCache.Instance.LpStore.Offers.Count + "]");
                            if (ESCache.Instance.LpStore.Offers.Any(i => i.IskCost < ESCache.Instance.DirectEve.Me.Wealth && i.LoyaltyPointCost < loyaltyPointsLeftToSpend))
                                foreach (DirectLoyaltyPointOffer lpStoreOffer in ESCache.Instance.LpStore.Offers.Where(i => i.IskCost < ESCache.Instance.DirectEve.Me.Wealth && i.LoyaltyPointCost < loyaltyPointsLeftToSpend))
                                {
                                    if (DebugConfig.DebugBuyLpItem) Log("lpStoreOffer: OfferID [" + lpStoreOffer.OfferId + "][" + lpStoreOffer.TypeName + "] TypeId [" + lpStoreOffer.TypeId + "] Quantity [" + lpStoreOffer.Quantity + "] Priority [" + itemToBuyFromLpStore.Priority + "] Isk [" + lpStoreOffer.IskCost + "] LP [" + lpStoreOffer.LoyaltyPointCost + "]");
                                    if (lpStoreOffer.TypeId == itemToBuyFromLpStore.TypeId)
                                    {
                                        double quantityToAccept = HowManyLpOrdersShouldWeBuy(lpStoreOffer, ESCache.Instance.DirectEve.Me.Wealth ?? 0, loyaltyPointsLeftToSpend, itemToBuyFromLpStore);
                                        if (quantityToAccept > 0)
                                        {
                                            Log("LPOffer: Buying [" + quantityToAccept + "] x [" + lpStoreOffer.TypeName + "] OfferId [" + lpStoreOffer.OfferId + "] for [" + lpStoreOffer.LoyaltyPointCost * quantityToAccept + "] total LP and [" + lpStoreOffer.IskCost * quantityToAccept + "] ISK");
                                            if (lpStoreOffer.AcceptOfferFromWindow((int)Math.Min(quantityToAccept, 60)))
                                            {
                                                WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LastBuyLpItems), DateTime.UtcNow);
                                                Time.Instance.NextBuyLpItemAction = DateTime.UtcNow.AddSeconds(10);
                                                return;
                                            }
                                        }
                                    }
                                }
                            else
                                Log("LPStore: We found no orders we could purchase with WalletBalance [" + ESCache.Instance.DirectEve.Me.Wealth + "] and LoyaltyPoints [" + loyaltyPointsLeftToSpend + "]");
                        }
                    }
                    else
                    {
                        Log("LPStore: There are no itemsToBuyFromLpStore defined in your config to purchase at the LPStore. ");
                        Log("LPStore: <itemsToBuyFromLpStore>");
                        Log("LPStore:     <itemToTakeToMarket description=\"Caldari Navy Uranium Charge S\" typeId=\"23013\"  quantity=\"100\" />");
                        Log("LPStore: </itemsToBuyFromLpStore>");
                    }

                    WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LastBuyLpItemAttempt), DateTime.UtcNow);
                    Log("LPStore: Done processing orders.");
                    ChangeBuyLpItemsState(BuyLpItemsState.Done);
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

        public void ProcessState()
        {
            switch (State.CurrentBuyLpItemsState)
            {
                case BuyLpItemsState.Idle:
                    if (DateTime.UtcNow > ESCache.Instance.EveAccount.LastBuyLpItems.AddHours(4) || ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.CareerAgentController))
                    {
                        ChangeBuyLpItemsState(BuyLpItemsState.BuyLpItems);
                        return;
                    }

                    ControllerManager.Instance.RemoveController(typeof(BuyLpItemsController));
                    return;

                case BuyLpItemsState.BuyLpItems:
                    BuyLpItems();
                    return;

                case BuyLpItemsState.Done:
                    if (ESCache.Instance.LpStore != null)
                        ESCache.Instance.CloseLPStore();

                    WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LastBuyLpItems), DateTime.UtcNow);
                    ControllerManager.Instance.RemoveController(typeof(BuyLpItemsController));
                    return;
            }
        }

        private double HowManyLpOrdersShouldWeBuy(DirectLoyaltyPointOffer lpOffer, double myWalletBalance, double myLoyaltyPoints, InventoryItem itemToBuyFromLpStore)
        {
            try
            {
                double tempDesiredQuantityOfThisItemToStockFromLpStore = itemToBuyFromLpStore.Quantity;
                int weAlreadyHaveThisManyInOurHangar = 0;
                if (ESCache.Instance.AmmoHangar.Items.Any(item => item.TypeId == lpOffer.TypeId))
                    weAlreadyHaveThisManyInOurHangar = ESCache.Instance.AmmoHangar.Items.Where(item => item.TypeId == lpOffer.TypeId).Sum(item => item.Quantity);

                if (weAlreadyHaveThisManyInOurHangar > 0)
                {
                    if (weAlreadyHaveThisManyInOurHangar > itemToBuyFromLpStore.Quantity)
                    {
                        Log("LPStore ItemToBuy [" + itemToBuyFromLpStore.Name + "] We already have [" + weAlreadyHaveThisManyInOurHangar + "] and want [" + itemToBuyFromLpStore.Quantity + "]");
                        return 0;
                    }

                    tempDesiredQuantityOfThisItemToStockFromLpStore = Math.Min(weAlreadyHaveThisManyInOurHangar, itemToBuyFromLpStore.Quantity);
                }

                double tempLpAllowsUsToBuyThisManyLpOrders = LpAllowsUsToBuyThisManyLpOrders(lpOffer, myLoyaltyPoints);
                double tempIskAllowsUsToBuyThisManyLpOrders = IskAllowsUsToBuyThisManyLpOrders(lpOffer, myWalletBalance);
                double tempItemRequirementsAllowUsToBuyThisManyLpOrders = ItemRequirementsAllowUsToBuyThisManyLpOrders(lpOffer, ESCache.Instance.AmmoHangar.Items);
                if (tempLpAllowsUsToBuyThisManyLpOrders != 0 && tempIskAllowsUsToBuyThisManyLpOrders != 0 && tempItemRequirementsAllowUsToBuyThisManyLpOrders != 0)
                {
                    //
                    // the scarcest resource (item) sets how many we can buy
                    //
                    Log("LPStore ItemToBuy [" + itemToBuyFromLpStore.Name + "] Desired Quantity [" + tempDesiredQuantityOfThisItemToStockFromLpStore + "]  Isk Limits [" + tempIskAllowsUsToBuyThisManyLpOrders + "] Lp Limits [" + tempLpAllowsUsToBuyThisManyLpOrders + "] Material Limits [" + tempItemRequirementsAllowUsToBuyThisManyLpOrders + "]");
                    _howManyLpOrdersShouldWeBuy = Math.Min(tempItemRequirementsAllowUsToBuyThisManyLpOrders, Math.Min(tempIskAllowsUsToBuyThisManyLpOrders, tempLpAllowsUsToBuyThisManyLpOrders));
                    //
                    // we only want to buy as many as we have defined:
                    // <itemsToBuyFromLpStore>
                    //   <itemToBuyFromLpStore description="Caldari Navy Uranium Charge S" typeId="23013"  quantity="1" />
                    // </itemsToBuyFromLpStore>
                    //
                    _howManyLpOrdersShouldWeBuy = Math.Min((double)_howManyLpOrdersShouldWeBuy, tempDesiredQuantityOfThisItemToStockFromLpStore);
                    return _howManyLpOrdersShouldWeBuy ?? 0;
                }

                return 0;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return 0;
            }
        }

        private double IskAllowsUsToBuyThisManyLpOrders(DirectLoyaltyPointOffer lpOffer, double myWalletBalance)
        {
            try
            {
                double iskAllowsUsToBuyThisManyOrders = myWalletBalance / lpOffer.IskCost;
                if (iskAllowsUsToBuyThisManyOrders > 1)
                    return Math.Floor(iskAllowsUsToBuyThisManyOrders);

                return 0;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return 0;
            }
        }

        private double ItemRequirementsAllowUsToBuyThisManyLpOrders(DirectLoyaltyPointOffer lpOffer, List<DirectItem> itemsInHangar)
        {
            try
            {
                if (itemsInHangar == null) return 0;

                int i = 0;
                if (lpOffer.RequiredItems.Count > 0)
                    foreach (DirectLoyaltyPointOfferRequiredItem lpOfferRequiredItem in lpOffer.RequiredItems)
                    {
                        i++;

                        double numberOfRequiredItemInItemhangar = 0;
                        if (itemsInHangar.Any(item => item.TypeId == lpOfferRequiredItem.TypeId))
                        {
                            numberOfRequiredItemInItemhangar = itemsInHangar.Where(item => item.TypeId == lpOfferRequiredItem.TypeId).Sum(item => item.Quantity);
                            Log("lpOfferRequiredItem [" + lpOfferRequiredItem.TypeName + "] numberOfRequiredItemInItemhangar [" + numberOfRequiredItemInItemhangar + "]");
                        }

                        switch (i)
                        {
                            case 1:
                                _itemRequirement1LetsUsBuildThisMany = numberOfRequiredItemInItemhangar / lpOfferRequiredItem.Quantity;
                                if (_itemRequirement1LetsUsBuildThisMany >= 1)
                                {
                                    _itemRequirement1LetsUsBuildThisMany = Math.Floor((double)_itemRequirement1LetsUsBuildThisMany);
                                    Log("lpOfferRequiredItem [" + lpOfferRequiredItem.TypeName + "] would limit us to [" + _itemRequirement1LetsUsBuildThisMany + "] lpstore orders");
                                    break;
                                }

                                Log("lpOfferRequiredItem [" + lpOfferRequiredItem.TypeName + "] was missing. We can buy [" + _itemRequirement1LetsUsBuildThisMany + "] lpstore orders");
                                _itemRequirement1LetsUsBuildThisMany = 0;
                                break;

                            case 2:
                                _itemRequirement2LetsUsBuildThisMany = numberOfRequiredItemInItemhangar / lpOfferRequiredItem.Quantity;
                                if (_itemRequirement2LetsUsBuildThisMany >= 1)
                                {
                                    _itemRequirement2LetsUsBuildThisMany = Math.Floor((double)_itemRequirement2LetsUsBuildThisMany);
                                    Log("lpOfferRequiredItem [" + lpOfferRequiredItem.TypeName + "] would limit us to [" + _itemRequirement2LetsUsBuildThisMany + "] lpstore orders");
                                    break;
                                }

                                Log("lpOfferRequiredItem [" + lpOfferRequiredItem.TypeName + "] was missing. We can buy [" + _itemRequirement2LetsUsBuildThisMany + "] lpstore orders");
                                _itemRequirement2LetsUsBuildThisMany = 0;
                                break;

                            case 3:
                                _itemRequirement3LetsUsBuildThisMany = numberOfRequiredItemInItemhangar / lpOfferRequiredItem.Quantity;
                                if (_itemRequirement3LetsUsBuildThisMany >= 1)
                                {
                                    _itemRequirement3LetsUsBuildThisMany = Math.Floor((double)_itemRequirement3LetsUsBuildThisMany);
                                    Log("lpOfferRequiredItem [" + lpOfferRequiredItem.TypeName + "] would limit us to [" + _itemRequirement3LetsUsBuildThisMany + "] lpstore orders");
                                    break;
                                }

                                Log("lpOfferRequiredItem [" + lpOfferRequiredItem.TypeName + "] was missing. We can buy [" + _itemRequirement3LetsUsBuildThisMany + "] lpstore orders");
                                _itemRequirement3LetsUsBuildThisMany = 0;
                                break;

                            case 4:
                                _itemRequirement4LetsUsBuildThisMany = numberOfRequiredItemInItemhangar / lpOfferRequiredItem.Quantity;
                                if (_itemRequirement4LetsUsBuildThisMany >= 1)
                                {
                                    _itemRequirement4LetsUsBuildThisMany = Math.Floor((double)_itemRequirement4LetsUsBuildThisMany);
                                    Log("lpOfferRequiredItem [" + lpOfferRequiredItem.TypeName + "] would limit us to [" + _itemRequirement4LetsUsBuildThisMany + "] lpstore orders");
                                    break;
                                }

                                Log("lpOfferRequiredItem [" + lpOfferRequiredItem.TypeName + "] was missing. We can buy [" + _itemRequirement4LetsUsBuildThisMany + "] lpstore orders");
                                _itemRequirement4LetsUsBuildThisMany = 0;
                                break;

                            case 5:
                                _itemRequirement5LetsUsBuildThisMany = numberOfRequiredItemInItemhangar / lpOfferRequiredItem.Quantity;
                                if (_itemRequirement5LetsUsBuildThisMany >= 1)
                                {
                                    _itemRequirement5LetsUsBuildThisMany = Math.Floor((double)_itemRequirement5LetsUsBuildThisMany);
                                    Log("lpOfferRequiredItem [" + lpOfferRequiredItem.TypeName + "] would limit us to [" + _itemRequirement5LetsUsBuildThisMany + "] lpstore orders");
                                    break;
                                }

                                Log("lpOfferRequiredItem [" + lpOfferRequiredItem.TypeName + "] was missing. We can buy [" + _itemRequirement5LetsUsBuildThisMany + "] lpstore orders");
                                _itemRequirement5LetsUsBuildThisMany = 0;
                                break;

                            default:
                                break;
                        }
                    }

                if (_itemRequirement1LetsUsBuildThisMany != null)
                {
                    _itemRequirementsAllowUsToBuyThisManyLpOrders = _itemRequirement1LetsUsBuildThisMany;

                    if (_itemRequirement2LetsUsBuildThisMany != null)
                    {
                        _itemRequirementsAllowUsToBuyThisManyLpOrders = Math.Min((double)_itemRequirement1LetsUsBuildThisMany, (double)_itemRequirement2LetsUsBuildThisMany);

                        if (_itemRequirement3LetsUsBuildThisMany != null)
                        {
                            _itemRequirementsAllowUsToBuyThisManyLpOrders = Math.Min((double)_itemRequirement1LetsUsBuildThisMany, Math.Min((double)_itemRequirement2LetsUsBuildThisMany, (double)_itemRequirement3LetsUsBuildThisMany));

                            if (_itemRequirement4LetsUsBuildThisMany != null)
                            {
                                _itemRequirementsAllowUsToBuyThisManyLpOrders = Math.Min((double)_itemRequirement1LetsUsBuildThisMany, Math.Min((double)_itemRequirement2LetsUsBuildThisMany, Math.Min((double)_itemRequirement3LetsUsBuildThisMany, (double)_itemRequirement4LetsUsBuildThisMany)));

                                if (_itemRequirement5LetsUsBuildThisMany != null)
                                    _itemRequirementsAllowUsToBuyThisManyLpOrders = Math.Min((double)_itemRequirement1LetsUsBuildThisMany, Math.Min((double)_itemRequirement2LetsUsBuildThisMany, Math.Min((double)_itemRequirement3LetsUsBuildThisMany, Math.Min((double)_itemRequirement4LetsUsBuildThisMany, (double)_itemRequirement5LetsUsBuildThisMany))));

                                return (double)_itemRequirementsAllowUsToBuyThisManyLpOrders;
                            }

                            return (double)_itemRequirementsAllowUsToBuyThisManyLpOrders;
                        }

                        return (double)_itemRequirementsAllowUsToBuyThisManyLpOrders;
                    }

                    return (double)_itemRequirementsAllowUsToBuyThisManyLpOrders;
                }

                return 10000;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return 0;
            }
        }

        private double LpAllowsUsToBuyThisManyLpOrders(DirectLoyaltyPointOffer lpOffer, double myLoyaltyPoints)
        {
            try
            {
                double lpAllowsUsToBuyThisManyOrders = myLoyaltyPoints / lpOffer.LoyaltyPointCost;
                if (lpAllowsUsToBuyThisManyOrders > 1)
                    return Math.Floor(lpAllowsUsToBuyThisManyOrders);

                return 0;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return 0;
            }
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}