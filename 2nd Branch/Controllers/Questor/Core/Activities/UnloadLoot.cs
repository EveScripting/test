extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Questor.Activities
{
    public static class UnloadLoot
    {
        #region Properties

        private static int CargoRetry { get; set; }
        private static int HangarRetry { get; set; }
        private static long CurrentLootValue { get; set; }
        private static DateTime LastUnloadLootAttempt { get; set; } = DateTime.MinValue;

        #endregion Properties

        #region Fields

        private const int inventoryRetryLimit = 5;
        private static DateTime LastHighTierLootContainerAction { get; set; } = DateTime.MinValue;
        private static DateTime LastAmmoHangarAction { get; set; } = DateTime.MinValue;
        private static DateTime LastLootContainerAction { get; set; } = DateTime.MinValue;
        private static DateTime LastItemHangarAction { get; set; } = DateTime.MinValue;

        #endregion Fields

        #region Methods

        public static bool ChangeUnloadLootState(UnloadLootState state, bool wait = true)
        {
            try
            {
                if (State.CurrentUnloadLootState != state)
                {
                    ClearDataBetweenStates();
                    Log.WriteLine("New UnloadLootState [" + state + "]");
                    State.CurrentUnloadLootState = state;
                }

                if (wait)
                {
                    return true;
                }

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
            return LootValueOfItems(LootItemsInCurrentShipInventory());
        }

        public static long CurrentLootValueInItemHangar()
        {
            return LootValueOfItems(LootItemsInLootHangar());
        }

        public static bool IsLootItem(DirectItem i)
        {
            if (DirectUIModule.DefinedAmmoTypes.Any(a => a.TypeId == i.TypeId))
                return false;

            if (ESCache.Instance.Modules.Any(x => x.TypeId == i.TypeId))
                return false;

            if (Settings.Instance.CapacitorInjectorScript == i.TypeId)
                return false;

            if (i.Volume == 0)
                return false;

            if (i.GroupId == (int)Group.Booster)
                return false;

            if (i.CategoryId == (int)CategoryID.Drone)
                return false;

            if (i.CategoryId == (int)CategoryID.Charge)
                return false;

            if (i.CategoryId == (int)CategoryID.Skill)
                return false;

            if (i.TypeId == (int)TypeID.AngelDiamondTag)
                return false;

            if (i.TypeId == (int)TypeID.GuristasDiamondTag)
                return false;

            if (i.TypeId == (int)TypeID.ImperialNavyGatePermit)
                return false;

            if (i.GroupId == (int)Group.AccelerationGateKeys)
                return false;

            if (i.GroupId == (int)Group.Livestock)
                return false;

            if (i.GroupId == (int)Group.MiscSpecialMissionItems)
                return false;

            if (i.GroupId == (int)Group.Kernite)
                return false;

            if (i.GroupId == (int)Group.Omber)
                return false;

            if (i.GroupId == (int)Group.Commodities)
                return false;

            if (i.TypeId == (int)TypeID.AncillaryShieldBoosterScript)
                return false;

            if (i.TypeId == (int)TypeID.CapacitorInjectorScript)
                return false;

            if (i.TypeId == (int)TypeID.FocusedWarpDisruptionScript)
                return false;

            if (i.TypeId == (int)TypeID.OptimalRangeDisruptionScript)
                return false;

            if (i.TypeId == (int)TypeID.OptimalRangeScript)
                return false;

            if (i.TypeId == (int)TypeID.ScanResolutionDampeningScript)
                return false;

            if (i.TypeId == (int)TypeID.ScanResolutionScript)
                return false;

            if (i.TypeId == (int)TypeID.TargetingRangeScript)
                return false;

            if (i.TypeId == (int)TypeID.TrackingSpeedDisruptionScript)
                return false;

            if (i.TypeId == (int)TypeID.TrackingSpeedScript)
                return false;

            if (i.CategoryId == (int)CategoryID.Ship)
                return false;

            if (i.CategoryId == (int)CategoryID.Asteroid)
                return false;

            if (i.IsCommonMissionItem)
                return false;

            if (ESCache.UnloadLootTheseItemsAreLootById != null && ESCache.UnloadLootTheseItemsAreLootById.Any() && ESCache.UnloadLootTheseItemsAreLootById.ContainsKey(i.TypeId))
                return false;

            return true;
        }

        public static bool IsNotLootItem(DirectItem i)
        {
            return DirectUIModule.DefinedAmmoTypes.All(a => a.TypeId == i.TypeId)
                   || (Settings.Instance.CapacitorInjectorScript == i.TypeId
                   && i.Volume != 0)
                   || i.TypeId == (int)TypeID.AngelDiamondTag
                   || i.TypeId == (int)TypeID.GuristasDiamondTag
                   || i.TypeId == (int)TypeID.ImperialNavyGatePermit
                   || i.GroupId == (int)Group.AccelerationGateKeys
                   || i.GroupId == (int)Group.Livestock
                   || i.GroupId == (int)Group.MiscSpecialMissionItems
                   || i.GroupId == (int)Group.Kernite
                   || i.GroupId == (int)Group.Omber
                   || i.GroupId == (int)Group.Commodities
                   || (ESCache.Instance.ActiveShip.IsFrigate && i.TypeId == (int) TypeID.MetalScraps)
                   || (ESCache.Instance.ActiveShip.IsFrigate && i.TypeId == (int)TypeID.ReinforcedMetalScraps)
                   || i.TypeId == (int) TypeID.Marines
                   //|| i.TypeId == (int) TypeID.AncillaryShieldBoosterScript
                   //|| i.TypeId == (int) TypeID.CapacitorInjectorScript
                   //|| i.TypeId == (int) TypeID.FocusedWarpDisruptionScript
                   //|| i.TypeId == (int) TypeID.OptimalRangeDisruptionScript
                   //|| i.TypeId == (int) TypeID.OptimalRangeScript
                   //|| i.TypeId == (int) TypeID.ScanResolutionDampeningScript
                   //|| i.TypeId == (int) TypeID.ScanResolutionScript
                   //|| i.TypeId == (int) TypeID.TargetingRangeDampeningScript
                   //|| i.TypeId == (int) TypeID.TargetingRangeScript
                   //|| i.TypeId == (int) TypeID.TrackingSpeedDisruptionScript
                   //|| i.TypeId == (int) TypeID.TrackingSpeedScript
                   //|| i.GroupId == (int) Group.CapacitorGroupCharge
                   || i.CategoryId == (int)CategoryID.Asteroid
                   || i.IsCommonMissionItem;
        }

        public static List<DirectItem> LootItemsInCurrentShipInventory()
        {
            return ESCache.Instance.CurrentShipsCargo.Items.Where(IsLootItem).ToList();
        }

        public static List<DirectItem> LootItemsInLootHangar()
        {
            return ESCache.Instance.LootHangar.Items.Where(IsLootItem).ToList();
        }

        public static List<DirectItem> LootItemsInItemHangar()
        {
            return ESCache.Instance.DirectEve.GetItemHangar().Items.Where(IsLootItem).ToList();
        }

        public static List<DirectItem> NonLootItemsInItemHangar()
        {
            return ESCache.Instance.DirectEve.GetItemHangar().Items.Where(i => !IsLootItem(i)).ToList();
        }

        public static long LootValueOfItems(List<DirectItem> items)
        {
            long lootValue = 0;
            foreach (DirectItem item in items)
                lootValue += (long)item.AveragePrice() * Math.Max(item.Quantity, 1);
            return lootValue;
        }

        public static bool MoveHighTierLoot(DirectContainer fromContainer, UnloadLootState nextState)
        {
            if (DateTime.UtcNow < LastHighTierLootContainerAction.AddMilliseconds(2000))
                return false;

            if (fromContainer == null)
            {
                CargoRetry++;
                Log.WriteLine("MoveHighTierLoot: if (FromContainer == null)");
                if (CargoRetry > inventoryRetryLimit) ChangeUnloadLootState(nextState);
                return false;
            }

            //
            // todo: this can and should be configurable to be a different hangar
            //
            if (ESCache.Instance.LootHangar == null)
            {
                HangarRetry++;
                Log.WriteLine("if (ESCache.Instance.LootHangar == null)");
                if (HangarRetry > inventoryRetryLimit) ChangeUnloadLootState(nextState);
                return false;
            }

            if (fromContainer.Items.Count == 0)
            {
                ChangeUnloadLootState(nextState, false);
                return true;
            }

            List<DirectItem> highTierLootToMove = null; //FromContainer.Items.Where(i => i.Metalevel >= 6).ToList();

            if (highTierLootToMove != null && highTierLootToMove.Count > 0)
                try
                {
                    CurrentLootValue = LootValueOfItems(highTierLootToMove);
                    if (ESCache.Instance.LootContainer != null)
                    {
                        Log.WriteLine("Moving HighTier [" + highTierLootToMove.Count + "] items from CargoHold to HighTierLootContainer");
                        if (!ESCache.Instance.HighTierLootContainer.Add(highTierLootToMove)) return false;
                        LastHighTierLootContainerAction = DateTime.UtcNow;
                        return false;
                    }
                    //else if (ESCache.Instance.LootCorpHangar != null)
                    //{
                    //    Log.WriteLine("Moving HighTier [" + highTierLootToMove.Count + "] items from CargoHold to HighTierLoothangar");
                    //    ESCache.Instance.HighTierLootCorpHangar.Add(highTierLootToMove);
                    //    LootIsBeingMoved = true;
                    //    _lastUnloadAction = DateTime.UtcNow;
                    //    return false;
                    //}
                    Log.WriteLine("Moving HighTier [" + highTierLootToMove.Count + "] items from CargoHold to ItemHangar");
                    if (!ESCache.Instance.HighTierLootContainer.Add(highTierLootToMove)) return false;
                    LastHighTierLootContainerAction = DateTime.UtcNow;
                    return false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    ChangeUnloadLootState(nextState, true);
                    return false;
                }
            ChangeUnloadLootState(nextState, false);

            return false;
        }

        public static void ProcessState()
        {
            if (!EveryUnloadLootPulse()) return;

            switch (State.CurrentUnloadLootState)
            {
                case UnloadLootState.Idle:
                    break;

                case UnloadLootState.Done:
                    break;

                case UnloadLootState.Begin:
                    if (LastUnloadLootAttempt.AddMinutes(1) > DateTime.UtcNow)
                    {
                        ChangeUnloadLootState(UnloadLootState.Done, false);
                        break;
                    }

                    if (ESCache.Instance.CurrentShipsCargo == null)
                        return;

                    if (!ESCache.Instance.CurrentShipsCargo.Items.Any())
                    {
                        ChangeUnloadLootState(UnloadLootState.Done, false);
                        break;
                    }

                    LastUnloadLootAttempt = DateTime.UtcNow;
                    ChangeUnloadLootState(UnloadLootState.OrganizeItemHangar, false);
                    CurrentLootValue = 0;
                    break;

                case UnloadLootState.OrganizeItemHangar:
                    if (ESCache.Instance.ItemHangar != null && !ESCache.Instance.ItemHangar.OrganizeItemHangar()) return;
                    ChangeUnloadLootState(UnloadLootState.OrganizeLootHangar, false);
                    break;

                case UnloadLootState.OrganizeLootHangar:
                    if (Settings.Instance.UseCorpLootHangar)
                        if (!ESCache.Instance.LootHangar.StackLootHangar()) return;

                    ChangeUnloadLootState(UnloadLootState.OrganizeAmmoHangar, false);
                    break;

                case UnloadLootState.OrganizeAmmoHangar:
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HighSecAnomalyController)) //"HighSecAnomalyController")
                    {
                        if (!ESCache.Instance.ItemHangar.StackItemHangar()) return;
                        ChangeUnloadLootState(UnloadLootState.MoveLoot, false);
                        break;
                    }

                    if (!ESCache.Instance.CurrentShipsCargo.Items.Any())
                    {
                        ChangeUnloadLootState(UnloadLootState.Done, false);
                        break;
                    }

                    if (Settings.Instance.UseCorpAmmoHangar && Settings.Instance.AmmoCorpHangarDivisionNumber != Settings.Instance.LootCorpHangarDivisionNumber)
                        if (!ESCache.Instance.AmmoHangar.StackAmmoHangar()) return;

                    ChangeUnloadLootState(UnloadLootState.MoveAmmoItems);
                    break;

                case UnloadLootState.MoveAmmoItems:
                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController))
                    {
                        if (!MoveAmmoItems(ESCache.Instance.CurrentShipsCargo, UnloadLootState.MoveLoot)) return;
                        break;
                    }

                    if (!ESCache.Instance.CurrentShipsCargo.Items.Any())
                    {
                        ChangeUnloadLootState(UnloadLootState.Done, false);
                        break;
                    }

                    if (!MoveAmmoItems(ESCache.Instance.CurrentShipsCargo, UnloadLootState.MoveAmmoItemsFromFleetHangar)) return;
                    break;

                case UnloadLootState.MoveAmmoItemsFromFleetHangar:
                    if (!ESCache.Instance.ActiveShip.HasFleetHangar)
                    {
                        ChangeUnloadLootState(UnloadLootState.MoveMissionCompletionItems);
                        return;
                    }

                    if (!ESCache.Instance.CurrentShipsCargo.Items.Any())
                    {
                        ChangeUnloadLootState(UnloadLootState.Done, false);
                        break;
                    }

                    if (!MoveAmmoItems(ESCache.Instance.CurrentShipsFleetHangar, UnloadLootState.MoveMissionCompletionItems)) return;
                    break;

                // Todo:
                /// case UnloadLootState.MoveMobileTractor:
                ///    if (!MoveMobileTractor(ESCache.Instance.CurrentShipsCargo, UnloadLootState.MoveMissionCompletionItems)) return;
                ///    break;

                case UnloadLootState.MoveMissionCompletionItems:
                    if (!ESCache.Instance.CurrentShipsCargo.Items.Any())
                    {
                        ChangeUnloadLootState(UnloadLootState.Done, false);
                        break;
                    }

                    if (!MoveMissionCompletionItems(ESCache.Instance.CurrentShipsCargo, UnloadLootState.MoveHighTierLoot)) return;
                    break;

                //case UnloadLootState.MoveMissionCompletionItemsFromFleetHangar:
                //    if (!ESCache.Instance.ActiveShip.IsShipWithFleetHangar)
                //    {
                //        ChangeUnloadLootState(UnloadLootState.MoveHighTierLoot);
                //        return;
                //    }
                //
                //    if (!MoveMissionCompletionItems(ESCache.Instance.CurrentShipsFleetHangar, UnloadLootState.MoveHighTierLoot)) return;
                //    break;

                case UnloadLootState.MoveHighTierLoot:
                    if (!ESCache.Instance.CurrentShipsCargo.Items.Any())
                    {
                        ChangeUnloadLootState(UnloadLootState.Done, false);
                        break;
                    }

                    if (!MoveHighTierLoot(ESCache.Instance.CurrentShipsCargo, UnloadLootState.MoveLoot)) return;
                    break;

                //case UnloadLootState.MoveHighTierLootFromFleetHangar:
                //    if (!ESCache.Instance.ActiveShip.IsShipWithFleetHangar)
                //    {
                //        ChangeUnloadLootState(UnloadLootState.MoveLoot);
                //        return;
                //    }
                //
                //    if (!MoveHighTierLoot(ESCache.Instance.CurrentShipsFleetHangar, UnloadLootState.MoveLoot)) return;
                //    break;

                case UnloadLootState.MoveLoot:
                    if (!ESCache.Instance.CurrentShipsCargo.Items.Any())
                    {
                        ChangeUnloadLootState(UnloadLootState.Done, false);
                        break;
                    }

                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HighSecAnomalyController))
                    {
                        Log.WriteLine("MoveLoot: CurrentShipsCargo to ItemHangar.");
                        if (!MoveLoot(ESCache.Instance.CurrentShipsCargo, UnloadLootState.Done, "CurrentShipsCargo")) return;
                        break;
                    }

                    if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.MiningController))
                    {
                        Log.WriteLine("MoveLoot: CurrentShipsOreHold to ItemHangar");
                        if (!MoveLoot(ESCache.Instance.CurrentShipsGeneralMiningHold, UnloadLootState.Done, "CurrentShipsOreHold")) return;
                        break;
                    }

                    Log.WriteLine("MoveLoot: CurrentShipsCargo to ItemHangar");
                    if (!MoveLoot(ESCache.Instance.CurrentShipsCargo, UnloadLootState.MoveRestOfCargo, "CurrentShipsCargo")) return;
                    break;

                case UnloadLootState.MoveRestOfCargo:
                    if (!ESCache.Instance.CurrentShipsCargo.Items.Any())
                    {
                        ChangeUnloadLootState(UnloadLootState.Done, false);
                        break;
                    }

                    if (!MoveRestOfCargo(ESCache.Instance.CurrentShipsCargo, UnloadLootState.Done)) return;
                    break;

                //case UnloadLootState.MoveLootFromFleetHangar:
                //    if (!ESCache.Instance.ActiveShip.IsShipWithFleetHangar)
                //    {
                //        ChangeUnloadLootState(UnloadLootState.Done);
                //        return;
                //    }
                //
                //    if (!MoveLoot(ESCache.Instance.CurrentShipsFleetHangar, UnloadLootState.Done)) return;
                //    break;
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

        private static bool MoveMobileTractor(DirectContainer fromContainer, UnloadLootState nextState)
        {
            if (DateTime.UtcNow < LastAmmoHangarAction.AddMilliseconds(2000))
                return false;

            if (fromContainer == null)
            {
                CargoRetry++;
                Log.WriteLine("if (FromContainer == null)");
                if (CargoRetry > inventoryRetryLimit) ChangeUnloadLootState(nextState);
                return false;
            }

            if (ESCache.Instance.AmmoHangar == null)
            {
                HangarRetry++;
                Log.WriteLine("if (ESCache.Instance.AmmoHangar == null)");
                if (HangarRetry > inventoryRetryLimit) ChangeUnloadLootState(nextState);
                return false;
            }

            if (fromContainer.Items.Count == 0)
            {
                ChangeUnloadLootState(nextState, false);
                return true;
            }

            List<DirectItem> mobileTractorToMove = null;
            mobileTractorToMove = fromContainer.Items.Where(i => i.GroupId == (int)Group.MobileTractor).ToList();

            if (mobileTractorToMove.Count > 0 && ESCache.Instance.AmmoHangar != null)
            {
                Log.WriteLine("Moving mobileTractor [" + mobileTractorToMove.Count + "] from CargoHold to AmmoHangar");
                if (!ESCache.Instance.AmmoHangar.Add(mobileTractorToMove)) return false;
                LastAmmoHangarAction = DateTime.UtcNow;
                return false;
            }
            ChangeUnloadLootState(nextState, false);

            return false;
        }

        private static bool MoveAmmoItems(DirectContainer fromContainer, UnloadLootState nextState)
        {
            if (DateTime.UtcNow < LastAmmoHangarAction.AddMilliseconds(1000))
                return false;

            if (!Settings.Instance.UseCorpAmmoHangar)
            {
                if (DateTime.UtcNow < LastItemHangarAction.AddMilliseconds(1000))
                    return false;
            }

            if (fromContainer == null)
            {
                CargoRetry++;
                Log.WriteLine("MoveAmmoItems: if (FromContainer == null)");
                if (CargoRetry > inventoryRetryLimit) ChangeUnloadLootState(nextState);
                return false;
            }

            if (ESCache.Instance.AmmoHangar == null)
            {
                HangarRetry++;
                Log.WriteLine("if (ESCache.Instance.AmmoHangar == null)");
                if (HangarRetry > inventoryRetryLimit) ChangeUnloadLootState(nextState);
                return false;
            }

            if (fromContainer.Items.Count == 0)
            {
                ChangeUnloadLootState(nextState, false);
                return true;
            }

            List<DirectItem> ammoItemsToMove = new List<DirectItem>();
            ammoItemsToMove = fromContainer.Items.Where(i => i.CategoryId == (int)CategoryID.Charge).ToList();

            List<DirectItem> boosterItemsToMove = new List<DirectItem>();
            boosterItemsToMove = fromContainer.Items.Where(i => i.GroupId == (int)Group.Booster).ToList();
            ammoItemsToMove.AddRange(boosterItemsToMove);

            if (ammoItemsToMove.Count > 0)
            {
                CurrentLootValue = LootValueOfItems(ammoItemsToMove);
                Log.WriteLine("MoveAmmoItems: Moving DefinedAmmoTypes [" + ammoItemsToMove.Count + "] items from CargoHold to ItemHangar");
                if (!ESCache.Instance.AmmoHangar.Add(ammoItemsToMove)) return false;
                LastAmmoHangarAction = DateTime.UtcNow;
                return false;
            }

            Log.WriteLine("MoveAmmoItems: Done moving Ammo");
            ChangeUnloadLootState(nextState, false);
            return false;
        }

        private static bool MoveLoot(DirectContainer fromContainer, UnloadLootState nextState, string fromContainerName)
        {
            if (DateTime.UtcNow < LastLootContainerAction.AddMilliseconds(1000))
                return false;

            if (!Settings.Instance.UseCorpAmmoHangar)
            {
                if (DateTime.UtcNow < LastAmmoHangarAction.AddMilliseconds(1000))
                    return false;
            }

            if (fromContainer == null)
            {
                CargoRetry++;
                Log.WriteLine("MoveLoot: if (FromContainer == null)");
                if (CargoRetry > inventoryRetryLimit) ChangeUnloadLootState(nextState);
                return false;
            }

            if (ESCache.Instance.LootHangar == null)
            {
                HangarRetry++;
                Log.WriteLine("if (ESCache.Instance.LootHangar == null)");
                if (HangarRetry > inventoryRetryLimit) ChangeUnloadLootState(nextState);
                return false;
            }

            //if (Settings.Instance.LootHangarCorpHangarDivisionNumber != null && Settings.Instance.LootHangarCorpHangarDivisionNumber != 0)
            //{
            //    if (ESCache.Instance.LootCorpHangar == null)
            //    {
            //        Log.WriteLine("if (ESCache.Instance.LootCorpHangar == null)");
            //        return false;
            //    }
            //}

            if (!string.IsNullOrEmpty(Settings.Instance.LootContainerName))
                if (ESCache.Instance.LootContainer == null)
                {
                    Log.WriteLine("if (ESCache.Instance.LootContainer == null)");
                    return false;
                }

            if (ESCache.Instance.SelectedController != nameof(EveAccount.AvailableControllers.MiningController))
            {
                if (fromContainer.Items.All(i => i.IsCommonMissionItem))
                {
                    Log.WriteLine("All items in the cargo hold are mission items");
                    ChangeUnloadLootState(nextState, false);
                    return true;
                }
            }

            List<DirectItem> lootToMove = new List<DirectItem>();
            if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.MiningController))
            {
                lootToMove = fromContainer.Items.ToList();
            }
            else lootToMove = fromContainer.Items.Where(i => i.GroupId != (int)Group.MobileTractor && !i.IsCommonMissionItem && !i.IsDefinedAmmoType && i.CategoryId != (int)CategoryID.Charge).ToList();

            if (DebugConfig.DebugUnloadLoot)
            {
                int intCount = 0;
                Log.WriteLine("lootToMove Count [" + lootToMove.Count + "] fromContainer WindowName [" + fromContainer.Window.Name + "] UsedCapacityPercentage [" + fromContainer.UsedCapacityPercentage + "] Items [" + fromContainer.Items.Count + "]");
                foreach (var item in fromContainer.Items)
                {
                    intCount++;
                    Log.WriteLine("fromContainer [" + intCount + "][" + item.TypeName + "] Quantity [" + item.Quantity + "] Volume [" + item.Volume + "] TotalVolume [" + item.TotalVolume + "] IsSingleton [" + item.IsSingleton + "]");
                }

                intCount = 0;
                foreach (var item in lootToMove)
                {
                    intCount++;
                    Log.WriteLine("lootToMove [" + intCount + "][" + item.TypeName + "] Quantity [" + item.Quantity + "] Volume [" + item.Volume + "] TotalVolume [" + item.TotalVolume + "] IsSingleton [" + item.IsSingleton + "]");
                }
            }

            if (lootToMove.Count > 0)
            {
                try
                {
                    CurrentLootValue = LootValueOfItems(lootToMove);
                    if (ESCache.Instance.LootContainer != null)
                    {
                        Log.WriteLine("Moving [" + lootToMove.Count + "] items worth [" + CurrentLootValue + "] from CargoHold to LootContainer [" + Settings.Instance.LootContainerName + "]");
                        if (!ESCache.Instance.LootContainer.Add(lootToMove)) return false;
                        LastLootContainerAction = DateTime.UtcNow;
                        return false;
                    }

                    //if (ESCache.Instance.LootCorpHangar != null)
                    //{
                    //    Log.WriteLine("Moving [" + lootToMove.Count + "] items worth [" + CurrentLootValue + "] from CargoHold to LootCorpHangar Division [" + Settings.Instance.LootHangarCorpHangarDivisionNumber + "]");
                    //    ESCache.Instance.LootCorpHangar.Add(lootToMove);
                    //    LootIsBeingMoved = true;
                    //    _lastUnloadAction = DateTime.UtcNow;
                    //    return false;
                    //}

                    if (ESCache.Instance.LootHangar != null)
                    {
                        Log.WriteLine("Moving [" + lootToMove.Count + "] items worth [" + CurrentLootValue + "] from [" + fromContainerName + "] to ItemHangar");
                        if (!ESCache.Instance.LootHangar.Add(lootToMove)) return false;
                        LastLootContainerAction = DateTime.UtcNow;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    ChangeUnloadLootState(nextState, false);
                    return false;
                }
            }
            else
            {
                ChangeUnloadLootState(nextState, false);
            }

            return false;
        }

        private static bool MoveRestOfCargo(DirectContainer fromContainer, UnloadLootState nextState)
        {
            if (DateTime.UtcNow < LastItemHangarAction.AddMilliseconds(2000))
                return false;

            if (fromContainer == null)
            {
                CargoRetry++;
                Log.WriteLine("MoveRestOfCargo: if (FromContainer == null)");
                if (CargoRetry > inventoryRetryLimit) ChangeUnloadLootState(nextState);
                return false;
            }

            if (ESCache.Instance.ItemHangar == null)
            {
                HangarRetry++;
                Log.WriteLine("if (ESCache.Instance.ItemHangar == null)");
                if (HangarRetry > inventoryRetryLimit) ChangeUnloadLootState(nextState);
                return false;
            }

            //if (Settings.Instance.LootHangarCorpHangarDivisionNumber != null && Settings.Instance.LootHangarCorpHangarDivisionNumber != 0)
            //{
            //    if (ESCache.Instance.LootCorpHangar == null)
            //    {
            //        Log.WriteLine("if (ESCache.Instance.LootCorpHangar == null)");
            //        return false;
            //    }
            //}

            if (fromContainer.Items.Count == 0)
            {
                ChangeUnloadLootState(nextState, false);
                return true;
            }

            List<DirectItem> itemsToMove = fromContainer.Items.ToList();

            if (itemsToMove.Count > 0)
            {
                try
                {
                    CurrentLootValue = LootValueOfItems(itemsToMove);
                    if (ESCache.Instance.ItemHangar != null)
                    {
                        Log.WriteLine("Moving [" + itemsToMove.Count + "] items worth [" + CurrentLootValue + "] from CargoHold to ItemHangar");
                        if (!ESCache.Instance.ItemHangar.Add(itemsToMove)) return false;
                        LastItemHangarAction = DateTime.UtcNow;
                        return false;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    ChangeUnloadLootState(nextState, false);
                    return false;
                }
            }
            else
            {
                ChangeUnloadLootState(nextState, false);
            }

            return false;
        }

        private static bool MoveMissionCompletionItems(DirectContainer fromContainer, UnloadLootState nextState)
        {
            if (DateTime.UtcNow < LastItemHangarAction.AddMilliseconds(2000))
                return false;

            if (fromContainer == null)
            {
                CargoRetry++;
                Log.WriteLine("MoveMissionCompletionItems: if (FromContainer == null)");
                if (CargoRetry > inventoryRetryLimit) ChangeUnloadLootState(nextState);
                return false;
            }

            if (ESCache.Instance.ItemHangar == null)
            {
                HangarRetry++;
                Log.WriteLine("if (ESCache.Instance.AmmoHangar == null)");
                if (HangarRetry > inventoryRetryLimit) ChangeUnloadLootState(nextState);
                return false;
            }

            if (fromContainer.Items.Count == 0 ||
                ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalDeadspaceController) ||
                //ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.FleetAbyssalDeadspaceController) ||
                ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.HydraController))
            {
                ChangeUnloadLootState(nextState, false);
                return true;
            }

            List<DirectItem> missionCompletionItemsToMove = null;
            if (ESCache.Instance.ListofMissionCompletionItemsToLoot != null && ESCache.Instance.ListofMissionCompletionItemsToLoot.Count > 0)
                missionCompletionItemsToMove = fromContainer.Items.Where(i => i.IsCommonMissionItem).ToList();

            if (missionCompletionItemsToMove != null && missionCompletionItemsToMove.Count > 0 && ESCache.Instance.AmmoHangar != null)
            {
                CurrentLootValue = LootValueOfItems(missionCompletionItemsToMove);
                Log.WriteLine("Moving MissionCompletionItems [" + missionCompletionItemsToMove.Count + "] items from CargoHold to ItemHangar");
                if (!ESCache.Instance.ItemHangar.Add(missionCompletionItemsToMove)) return false;
                LastItemHangarAction = DateTime.UtcNow;
                return false;
            }

            ChangeUnloadLootState(nextState, false);

            return false;
        }

        #endregion Methods
    }
}