extern alias SC;

using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using System;
using System.Linq;

namespace EVESharpCore.Cache
{
    public partial class ESCache
    {
        #region Fields

        private DirectContainer _highTierLootContainer;
        private DirectContainer _lootContainer;

        #endregion Fields

        #region Properties

        public DirectContainer HighTierLootContainer
        {
            get
            {
                try
                {
                    if (string.IsNullOrEmpty(Settings.Instance.HighTierLootContainer))
                    {
                        Log.WriteLine("HighTierLootContainer: setting is empty or nul. Add <highValueLootContainer>NameOfMyHighValueLootContainerHere</highValueLootContainer> to your settings file.");
                        return null;
                    }

                    if (!InSpace && InStation)
                    {
                        if (Windows.Count > 0)
                        {
                            if (_highTierLootContainer == null)
                            {
                                DirectItem highTierLootContainerItem = null;
                                DirectContainer hangarToLootForContainerWithin = null;
                                hangarToLootForContainerWithin = ItemHangar;

                                foreach (DirectItem item in hangarToLootForContainerWithin.Items.Where(i => i.GivenName == Settings.Instance.HighTierLootContainer))
                                {
                                    if (DebugConfig.DebugLootContainer) Log.WriteLine("HighTierLootContainer: found container named [" + item.GivenName + "][" + item.TypeName + "] TypeId [" + item.TypeId + "]  which matches [" + Settings.Instance.LootContainerName + "]");
                                    highTierLootContainerItem = item;
                                }

                                if (highTierLootContainerItem != null)
                                {
                                    _highTierLootContainer = Instance.DirectEve.GetContainer(highTierLootContainerItem.ItemId);
                                    if (_highTierLootContainer != null)
                                        return _highTierLootContainer;

                                    return null;
                                }

                                if (DebugConfig.DebugLootContainer) Log.WriteLine("HighTierLootContainer: did not find any items with a GivenName [" + Settings.Instance.HighTierLootContainer + "]");
                                return null;
                            }

                            return _highTierLootContainer;
                        }

                        return null;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }

            set => _highTierLootContainer = value;
        }

        public DirectContainer HighTierLootCorpHangar => DirectEve.GetCorporationHangar(Settings.Instance.HighTierLootCorpHangarDivisionNumber);

        public DirectContainer AmmoCorpHangar => DirectEve.GetCorporationHangar(Settings.Instance.AmmoCorpHangarDivisionNumber);

        public DirectContainer AmmoHangar
        {
            get
            {
                if (DebugConfig.DebugLootCorpHangar) Log.WriteLine("AmmoHangar: DoesCorpHangarExistHere [" + DirectEve.DoesCorpHangarExistHere + "] UseCorpAmmoHangar [" + Settings.Instance.UseCorpAmmoHangar + "] LootCorpHangarDivisionNumber [" + Settings.Instance.LootCorpHangarDivisionNumber + "]");

                if (DirectEve.DoesCorpHangarExistHere && Settings.Instance.UseCorpAmmoHangar)
                {
                    return AmmoCorpHangar;
                }

                return ItemHangar;
            }
        }

        public DirectContainer LootHangar
        {
            get
            {
                if (DebugConfig.DebugLootCorpHangar) Log.WriteLine("LootHangar: DoesCorpHangarExistHere [" + DirectEve.DoesCorpHangarExistHere + "] UseCorpLootHangar [" + Settings.Instance.UseCorpLootHangar + "] AmmoCorpHangarDivisionNumber [" + Settings.Instance.AmmoCorpHangarDivisionNumber + "]");
                if (DirectEve.DoesCorpHangarExistHere && Settings.Instance.UseCorpLootHangar)
                {
                    return LootCorpHangar;
                }

                return ItemHangar;
            }
        }

        public DirectContainer ItemHangar => DirectEve.GetItemHangar();

        public DirectContainer LootContainer
        {
            get
            {
                try
                {
                    if (!Instance.InSpace && Instance.InStation)
                    {
                        if (Instance.Windows.Count > 0)
                        {
                            if (string.IsNullOrEmpty(Settings.Instance.LootContainerName))
                                return null;

                            if (Instance._lootContainer == null)
                            {
                                DirectItem lootContainerItem = null;
                                DirectContainer hangarToLootForContainerWithin = null;
                                hangarToLootForContainerWithin = ItemHangar;

                                foreach (DirectItem item in hangarToLootForContainerWithin.Items.Where(i => i.GivenName == Settings.Instance.LootContainerName))
                                {
                                    if (DebugConfig.DebugLootContainer) Log.WriteLine("LootContainer: found container named [" + item.GivenName + "][" + item.TypeName + "] TypeId [" + item.TypeId + "]  which matches [" + Settings.Instance.LootContainerName + "]");
                                    lootContainerItem = item;
                                }

                                if (lootContainerItem != null)
                                {
                                    Instance._lootContainer = Instance.DirectEve.GetContainer(lootContainerItem.ItemId);
                                    if (Instance._lootContainer != null)
                                        return Instance._lootContainer;

                                    return null;
                                }

                                if (DebugConfig.DebugLootContainer) Log.WriteLine("LootContainer: did not find any items with a GivenName [" + Settings.Instance.LootContainerName + "]");
                                return null;
                            }

                            return Instance._lootContainer;
                        }

                        return null;
                    }

                    return null;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return null;
                }
            }

            set => _lootContainer = value;
        }

        public DirectContainer LootCorpHangar => DirectEve.GetCorporationHangar(Settings.Instance.LootCorpHangarDivisionNumber);

        public DirectContainer ShipHangar => DirectEve.GetShipHangar();

        #endregion Properties

        #region Methods

        public bool StackHangar(DirectContainer hangarToStack)
        {
            if (hangarToStack == null) return false;

            try
            {
                if (Instance.InStation)
                {
                    if (DebugConfig.DebugHangars)
                        Log.WriteLine("if (Cache.Instance.InStation)");

                    try
                    {

                        Log.WriteLine("hangarToStack.StackAll()");
                        if (hangarToStack.StackAll()) return true;
                        return false;
                    }
                    catch (Exception exception)
                    {
                        Log.WriteLine("Stacking hangarToStack failed [" + exception + "]");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception exception)
            {
                Log.WriteLine("Unable to complete StackItemsHangarAsLootHangar [" + exception + "]");
                return true;
            }
        }

        public void InvalidateCache_Hangars()
        {
            _lootContainer = null;
            _highTierLootContainer = null;
        }

        #endregion Methods
    }
}