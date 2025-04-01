extern alias SC;

using System;
using System.Collections.Generic;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;

namespace EVESharpCore.Cache
{
    public class ItemCache
    {
        #region Constructors

        public ItemCache(DirectItem item, bool cacheRefineOutput)
        {
            try
            {
                NameForSorting = item.TypeName.Replace("'", "");
                //GroupId = item.GroupId;
                CategoryId = item.CategoryId;
                BasePrice = item.BasePrice;
                Capacity = item.Capacity;
                MarketGroupId = item.MarketGroupId;
                PortionSize = item.PortionSize;

                QuantitySold = 0;

                RefineOutput = new List<ItemCache>();
                if (cacheRefineOutput)
                    foreach (DirectItem i in item.Materials)
                        RefineOutput.Add(new ItemCache(i, false));

                MaxVelocity = item.Attributes.TryGet<int>("maxVelocity");

                EmDamage = item.Attributes.TryGet<int>("emDamage");
                ExplosiveDamage = item.Attributes.TryGet<int>("explosiveDamage");
                KineticDamage = item.Attributes.TryGet<int>("explosiveDamage");
                ThermalDamage = item.Attributes.TryGet<int>("thermalDamage");
                MetaLevel = item.Attributes.TryGet<int>("metaLevel");
                Hp = item.Attributes.TryGet<int>("hp");
                TechLevel = item.Attributes.TryGet<int>("techLevel");
                Radius = item.Attributes.TryGet<int>("radius");

                AoeDamageReductionFactor = item.Attributes.TryGet<int>("aoeDamageReductionFactor");
                DetonationRange = item.Attributes.TryGet<int>("detonationRange");
                AoeCloudSize = item.Attributes.TryGet<int>("aoeCloudSize");
                AoeVelocity = item.Attributes.TryGet<int>("aoeVelocity");
                Agility = item.Attributes.TryGet<int>("agility");
                ExplosionDelay = item.Attributes.TryGet<int>("explosionDelay");
                MaxVelocityBonus = item.Attributes.TryGet<int>("maxVelocityBonus");

                FallofMultiplier = item.Attributes.TryGet<int>("fallofMultiplier");
                WeaponRangeMultiplier = item.Attributes.TryGet<int>("weaponRangeMultiplier");
                TrackingSpeedMultiplier = item.Attributes.TryGet<int>("trackingSpeedMultiplier");
                PowerNeedMultiplier = item.Attributes.TryGet<int>("powerNeedMultiplier");
            }
            catch (Exception exception)
            {
                Log.WriteLine("Exception [" + exception + "]");
            }
        }

        public ItemCache(DirectItem item)
        {
            DirectItem = item;
        }

        #endregion Constructors

        #region Properties

        public int Agility { get; set; }
        public int AoeCloudSize { get; set; }
        public int AoeDamageReductionFactor { get; set; }
        public int AoeVelocity { get; set; }
        public double BasePrice { get; }
        public double Capacity { get; }
        public int CategoryId { get; }
        public int DetonationRange { get; set; }
        public DirectItem DirectItem { get; }

        public int EmDamage { get; set; }
        public int ExplosionDelay { get; set; }
        public int ExplosiveDamage { get; set; }
        public int FallofMultiplier { get; set; }
        public int GroupId => DirectItem.GroupId;
        public int Hp { get; set; }
        public long Id => DirectItem.ItemId;
        public bool IsCommonMissionItem => TypeId == 28260 || TypeId == 3814 || TypeId == 2076 || TypeId == 25373 || TypeId == 3810;

        public bool IsContraband
        {
            get
            {
                bool result = false;
                result |= GroupId == (int)Group.Drugs;
                result |= GroupId == (int)Group.ToxicWaste;
                result |= TypeId == (int)TypeID.Slaves;
                result |= TypeId == (int)TypeID.Small_Arms;
                result |= TypeId == (int)TypeID.Ectoplasm;
                return result;
            }
        }

        public double? IskPerM3
        {
            get
            {
                try
                {
                    if (DirectItem != null)
                    {
                        if (DirectItem.AveragePrice() > 0)
                            return DirectItem.AveragePrice() / DirectItem.Volume;

                        return 0.001;
                    }

                    return 0.001;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public bool IsMissionItem
        {
            get
            {
                try
                {
                    if (State.CurrentQuestorState == QuestorState.CombatMissionsBehavior)
                    {
                        if (MissionSettings.MissionItems.Contains((Name ?? string.Empty).ToLower()))
                            return true;

                        if (ESCache.Instance.ListofMissionCompletionItemsToLoot.Contains((Name ?? string.Empty).ToLower()))
                            return true;

                        if (IsTypicalMissionCompletionItem)
                            return true;

                        return false;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        public bool IsTypicalMissionCompletionItem
        {
            get
            {
                if (TypeId == 25373) return true;
                if (TypeId == 3810) return true;
                if (TypeId == 2076) return true;
                if (TypeId == 24576) return true;
                if (TypeId == 28260) return true;
                if (TypeId == 3814) return true;
                if (TypeId == 24766) return true;

                return false;
            }
        }

        public int KineticDamage { get; set; }
        public int MarketGroupId { get; }
        public int MaxVelocity { get; set; }
        public int MaxVelocityBonus { get; set; }
        public int MetaLevel { get; set; }
        public string Name => DirectItem.TypeName;
        public string NameForSorting { get; }
        public int PortionSize { get; }
        public int PowerNeedMultiplier { get; set; }
        public int Quantity => DirectItem.Quantity;
        public int QuantitySold { get; set; }
        public int Radius { get; set; }
        public List<ItemCache> RefineOutput { get; }
        public int TechLevel { get; set; }
        public int ThermalDamage { get; set; }
        public double TotalVolume => DirectItem.Volume * Quantity;
        public int TrackingSpeedMultiplier { get; set; }
        public int TypeId => DirectItem.TypeId;

        public double? Value
        {
            get
            {
                try
                {
                    if (DirectItem != null)
                    {
                        if (DirectItem.AveragePrice() > 0)
                            return DirectItem.AveragePrice();

                        return 0.001;
                    }

                    return 0.001;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        public double Volume => DirectItem.Volume;
        public int WeaponRangeMultiplier { get; set; }

        public bool WontFitInContainers
        {
            get
            {
                if (TypeId == 41) return true;
                if (TypeId == 42) return true;
                if (TypeId == 42) return true;
                if (TypeId == 44) return true;
                if (TypeId == 45) return true;
                if (TypeId == 3673) return true;
                if (TypeId == 3699) return true;
                if (TypeId == 3715) return true;
                if (TypeId == 3717) return true;
                if (TypeId == 3721) return true;
                if (TypeId == 3723) return true;
                if (TypeId == 3725) return true;
                if (TypeId == 3727) return true;
                if (TypeId == 3729) return true;
                if (TypeId == 3771) return true;
                if (TypeId == 3773) return true;
                if (TypeId == 3775) return true;
                if (TypeId == 3777) return true;
                if (TypeId == 3779) return true;
                if (TypeId == 3804) return true;
                if (TypeId == 3806) return true;
                if (TypeId == 3808) return true;
                if (TypeId == 12865) return true;
                if (TypeId == 13267) return true;
                if (TypeId == 17765) return true;
                if (TypeId == 22208) return true;
                if (TypeId == 22209) return true;
                if (TypeId == 22210) return true;
                return false;
            }
        }

        #endregion Properties
    }
}