using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Questor.Activities
{
    public static class LoadItemsToHaul
    {
        #region Properties

        private static int CargoRetry { get; set; }
        private static int HangarRetry { get; set; }
        private static long CurrentLootValue { get; set; }
        private static DateTime LastLoadItemsToHaulAttempt { get; set; } = DateTime.MinValue;

        #endregion Properties

        #region Fields

        private static readonly int inventoryRetryLimit = 5;
        private static DateTime LastLoadItemToHaulAction { get; set; } = DateTime.MinValue;
        private static double _minimumLootHangarValueToHaul = 500000000; //Five Hundred Million

        #endregion Fields

        #region Methods

        public static Dictionary<int, int> MoveToCargoList = new Dictionary<int, int>();

        public static bool MoveHangarItems(DirectContainer fromContainer, DirectContainer toContainer, Dictionary<int, int> tempMoveToCargoList)
        {
            try
            {
                if (!ESCache.Instance.InStation)
                    return false;

                if (fromContainer == null)
                    return false;

                if (toContainer == null)
                    return false;

                IEnumerable<DirectItem> lootHangarItemsThatCouldBeMoved = fromContainer.Items.Where(i => tempMoveToCargoList.ContainsKey(i.TypeId)).ToList();

                if (lootHangarItemsThatCouldBeMoved.Any())
                {
                    //
                    // todo: this moves items one at a time and checks freespace after each item. we should work the math and move all items at once (but that means getting the math right! if its wrong we will have errors in eve if we try to overfill a ship!
                    //
                    DirectItem itemToPotentiallyMove = lootHangarItemsThatCouldBeMoved.FirstOrDefault();

                    if (itemToPotentiallyMove != null)
                    {
                        int maxUnitsBasedOnVolumeToTryToMove = Math.Min(itemToPotentiallyMove.Stacksize, tempMoveToCargoList[itemToPotentiallyMove.TypeId]);
                        int myToContainerFreeCargo = (int)toContainer.Capacity - (int)toContainer.UsedCapacity;
                        DirectInvType itemInvType = ESCache.Instance.DirectEve.GetInvType(itemToPotentiallyMove.TypeId);
                        Log.WriteLine("myToContainerFreeCargo [" + myToContainerFreeCargo + "] itemInvType [" + itemInvType.TypeName + "] Volume each [" + itemInvType.Volume + "] TypeID [" + itemInvType.TypeId + "] ");
                        if (myToContainerFreeCargo > 0 && itemInvType.Volume > 0)
                        {
                            int numofUnitsThatWillFitinCargo = (int)Math.Round(myToContainerFreeCargo / itemInvType.Volume, 0);
                            int maxVolumeToMove = Math.Min(maxUnitsBasedOnVolumeToTryToMove, numofUnitsThatWillFitinCargo);
                            if (maxVolumeToMove >= tempMoveToCargoList[itemToPotentiallyMove.TypeId])
                                tempMoveToCargoList.Remove(itemToPotentiallyMove.TypeId);

                            maxVolumeToMove = Math.Max(1, maxVolumeToMove);

                            if (numofUnitsThatWillFitinCargo <= 0)
                            {
                                Log.WriteLine("Nothing else will fit in the destination");
                                return true;
                            }

                            if (myToContainerFreeCargo <= 5)
                            {
                                Log.WriteLine("We havent yet totally filled up, but weo do have less than 5 m3 free.to avoid getting stuck we are going to consider this full enough");
                                return true;
                            }

                            Log.WriteLine("Moving item [" + itemToPotentiallyMove + "] Quantity [" + itemToPotentiallyMove.Quantity + "][" + itemToPotentiallyMove.TotalVolume + "]m3 to destination which had [" + Math.Round((double)myToContainerFreeCargo, 0) + "] m3 free");
                            if (!toContainer.Add(itemToPotentiallyMove, maxVolumeToMove)) return false;
                            return false;
                        }
                    }
                }

                Log.WriteLine("Done moving items to destination");
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static bool ChangeLoadItemsToHaulState(LoadItemsToHaulState state, bool wait = true)
        {
            try
            {
                if (State.CurrentLoadItemsToHaulState != state)
                {
                    ClearDataBetweenStates();
                    Log.WriteLine("New LoadItemsToHaulState [" + state + "]");
                    State.CurrentLoadItemsToHaulState = state;
                }

                if (wait)
                {
                    LastLoadItemToHaulAction = DateTime.UtcNow;
                    return true;
                }
                LastLoadItemToHaulAction = DateTime.UtcNow.AddMinutes(-1);
                ProcessState();
                return true;
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public static long CurrentLootValueInCurrentShipInventory()
        {
            return ValueOfItems(ItemsInCurrentShipInventory());
        }

        public static long CurrentValueInMyContainer(DirectContainer myContainer)
        {
            return ValueOfItems(ItemsInHangar(myContainer));
        }

        public static List<DirectItem> ItemsInCurrentShipInventory()
        {
            return ESCache.Instance.CurrentShipsCargo.Items.ToList();
        }

        public static List<DirectItem> ItemsInHangar(DirectContainer myContainer)
        {
            return myContainer.Items.ToList();
        }

        public static long ValueOfItems(List<DirectItem> items)
        {
            long lootValue = 0;
            foreach (DirectItem item in items)
                lootValue += (long)item.AveragePrice() * Math.Max(item.Quantity, 1);
            return lootValue;
        }

        public static void ProcessState()
        {
            if (!EveryUnloadLootPulse()) return;

            switch (State.CurrentLoadItemsToHaulState)
            {
                case LoadItemsToHaulState.Idle:
                    break;

                case LoadItemsToHaulState.Done:
                    break;

                case LoadItemsToHaulState.Begin:
                    //if (LastLoadItemsToHaulAttempt.AddMinutes(1) > DateTime.UtcNow)
                    //    ChangeLoadItemsToHaulState(LoadItemsToHaulState.Done, false);

                    LastLoadItemToHaulAction = DateTime.UtcNow.AddMinutes(-1);
                    LastLoadItemsToHaulAttempt = DateTime.UtcNow;
                    ChangeLoadItemsToHaulState(LoadItemsToHaulState.StackItemHangar, false);
                    CurrentLootValue = 0;
                    break;

                case LoadItemsToHaulState.StackItemHangar:
                    if (!ESCache.Instance.ItemHangar.StackItemHangar()) return;
                    ChangeLoadItemsToHaulState(LoadItemsToHaulState.StackLootHangar);
                    break;

                case LoadItemsToHaulState.StackLootHangar:
                    if (Settings.Instance.UseCorpLootHangar)
                        if (!ESCache.Instance.LootHangar.StackAmmoHangar()) return;

                    ChangeLoadItemsToHaulState(LoadItemsToHaulState.StackAmmoHangar);
                    break;

                case LoadItemsToHaulState.StackAmmoHangar:
                    if (Settings.Instance.UseCorpAmmoHangar && Settings.Instance.AmmoCorpHangarDivisionNumber != Settings.Instance.LootCorpHangarDivisionNumber)
                        if (!ESCache.Instance.AmmoHangar.StackAmmoHangar()) return;

                    ChangeLoadItemsToHaulState(LoadItemsToHaulState.CalcValueOfLootHangar);
                    break;

                case LoadItemsToHaulState.CalcValueOfLootHangar:
                    if (ESCache.Instance.LootHangar != null)
                    {
                        if (ESCache.Instance.LootHangar.Items.Any())
                        {
                            var tempLootHangarValue = CurrentValueInMyContainer(ESCache.Instance.LootContainer);
                            if (CurrentValueInMyContainer(ESCache.Instance.LootContainer) > _minimumLootHangarValueToHaul)
                            {
                                Log.WriteLine("Current value of LootHangar is [" + tempLootHangarValue + "] which is more than the minimum [" + _minimumLootHangarValueToHaul + "]");
                                ChangeLoadItemsToHaulState(LoadItemsToHaulState.MoveLootHangarItemsToCargo);
                                return;
                            }

                            Log.WriteLine("Current value of LootHangar is [" + tempLootHangarValue + "] which is less than the minimum [" + _minimumLootHangarValueToHaul + "]");
                            ChangeLoadItemsToHaulState(LoadItemsToHaulState.Done);
                            return;
                        }

                        ChangeLoadItemsToHaulState(LoadItemsToHaulState.Done);
                        return;
                    }

                    break;

                case LoadItemsToHaulState.MoveLootHangarItemsToCargo:
                    if (!MoveHangarItems(ESCache.Instance.LootHangar, ESCache.Instance.CurrentShipsCargo, MoveToCargoList)) return;
                    break;
            }
        }

        private static void ClearDataBetweenStates()
        {
            CargoRetry = 0;
            HangarRetry = 0;
        }

        private static bool EveryUnloadLootPulse()
        {
            try
            {
                if (!ESCache.Instance.InStation)
                    return false;

                if (ESCache.Instance.InSpace)
                    return false;

                return true;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
                return false;
            }
        }

        private static bool MoveItems(DirectContainer fromContainer, DirectContainer toContainer, List<DirectItem> itemsToMove, LoadItemsToHaulState nextState)
        {
            if (DateTime.UtcNow < LastLoadItemToHaulAction.AddMilliseconds(2000))
                return false;

            if (fromContainer == null)
            {
                CargoRetry++;
                Log.WriteLine("MoveItems: if (FromContainer == null)");
                if (CargoRetry > inventoryRetryLimit) ChangeLoadItemsToHaulState(nextState);
                return false;
            }

            if (toContainer == null)
            {
                HangarRetry++;
                Log.WriteLine("MoveItems: if (toContainer == null)");
                if (HangarRetry > inventoryRetryLimit) ChangeLoadItemsToHaulState(nextState);
                return false;
            }

            if (!fromContainer.Items.Any())
            {
                ChangeLoadItemsToHaulState(nextState, false);
                return true;
            }

            if (itemsToMove.Any())
            {
                CurrentLootValue = ValueOfItems(itemsToMove);
                Log.WriteLine("MoveItems: Moving [" + itemsToMove.Count + "] items from CargoHold to ItemHangar");
                if (!toContainer.Add(itemsToMove)) return false;
                LastLoadItemToHaulAction = DateTime.UtcNow;
                return false;
            }

            Log.WriteLine("MoveItems: No item(s) found to move");
            ChangeLoadItemsToHaulState(nextState, false);
            return false;
        }

        #endregion Methods
    }
}