
extern alias SC;
using System;
using EVESharpCore.Logging;
using SC::SharedComponents.Py;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Lookup;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.EVE;
using SC::SharedComponents.Utility;
using EVESharpCore.Cache;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Framework
{
    extern alias SC;

    public class DirectItem : DirectInvType
    {
        #region Fields

        private DirectItemAttributes _attributes;

        private int? _flagId;
        private string _givenName;
        //private bool? _isDroneWeHaveLaunched;
        private bool? _isSingleton;

        private long? _itemId;

        private DirectLocation _location;

        //private long? _metaLevel;
        //private long? _techLevel;
        private long? _locationId;

        private string _locationName;

        private List<DirectItem> _materials;
        private int? _ownerId;
        private PyObject _pyItem;
        private PyObject? _pyDynamicItem;
        private int? _quantity;
        private int? _stacksize;

        #endregion Fields

        #region Constructors

        internal DirectItem(DirectEve directEve) : base(directEve)
        {
            PyItem = PySharp.PyZero;
        }

        #endregion Constructors

        #region Properties

        public DirectItemAttributes Attributes
        {
            get
            {
                if (_attributes == null && PyItem.IsValid)
                {
                    var pyItemId = PyItem.Attribute("itemID");
                    if (pyItemId.IsValid)
                        _attributes = new DirectItemAttributes(DirectEve, pyItemId);
                }

                _attributes = _attributes ?? new DirectItemAttributes(DirectEve, ItemId);
                return _attributes;
            }
        }

        public bool DoesIndustryWindowHaveThisBlueprintLoaded()
        {
            var industryWindow = ESCache.Instance.Windows.OfType<DirectIndustryWindow>().FirstOrDefault();

            if (industryWindow != null)
            {
                if (industryWindow.BlueprintTypeID != null)
                {
                    if (industryWindow.BlueprintItemID != null)
                    {
                        if (industryWindow.BlueprintTypeID == TypeId && industryWindow.BlueprintItemID == ItemId)
                        {
                            if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("if (industryWindow.BlueprintTypeID == TypeId && industryWindow.BlueprintItemID == ItemId) return true");
                            return true;
                        }

                        return false;
                    }

                    return false;
                }

                return false;
            }

            return false;
        }

        public bool UseBlueprint()
        {
            if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("UseBlueprint [" + TypeName + "] TypeId [" + TypeId + "] CategoryName [" + CategoryName + "] CategoryId [" + CategoryId + "]");
            if (CategoryId == (int)CategoryID.Blueprint)
            {
                if (!ESCache.Instance.OpenIndustryWindow()) return false;
                if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("OpenIndustryWindow returned true");
                var industryWindow = ESCache.Instance.Windows.OfType<DirectIndustryWindow>().FirstOrDefault();
                if (industryWindow != null)
                {
                    if (DebugConfig.DebugIndustryBehavior && industryWindow.BlueprintTypeID != null) Log.WriteLine("industryWindow.BlueprintTypeID [" + industryWindow.BlueprintTypeID + "]");
                    if (DebugConfig.DebugIndustryBehavior && industryWindow.BlueprintItemID != null) Log.WriteLine("industryWindow.BlueprintItemID [" + industryWindow.BlueprintItemID + "]");
                    if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("Blueprint to load: [" + TypeName + "] TypeID [" + TypeId + "] ItemID [" + ItemId + "]");

                    if (DoesIndustryWindowHaveThisBlueprintLoaded()) return true;


                    //example from ActivateShip which uses sm.StartService('station').TryActivateShip(invItem)
                    //
                    //return DirectEve.ThreadedLocalSvcCall("station", "TryActivateShip", PyItem);
                    //

                    //__builtin__.sm.services[menu]
                    //
                    var menuSvc = DirectEve.GetLocalSvc("menu");

                    if (!menuSvc.IsValid)
                    {
                        Log.WriteLine("Menu svc ref is not valid.");
                        return false;
                    }

                    //var call = DirectEve.GetLocalSvc("menu")["Use Blueprint"];
                    //PyObject pyShowInIndustryWindow = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.invItemFunctions").Attribute("ShowInIndustryWindow");


                    //if (!pyShowInIndustryWindow.IsValid)
                    //{
                    //    if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("if (!pyShowInIndustryWindow.IsValid");
                    //    return false;
                    //}

                    if (!this.PyItem.IsValid)
                    {
                        if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("if (!this.PyItem.IsValid");
                        return false;
                    }

                    //if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("PySharp.Import(\"eve.client.script.ui.services.menuSvcExtras.invItemFunctions\").Attribute(\"ShowInIndustryWindow\");");
                    //if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("var call = DirectEve.GetLocalSvc(\"menu\")[\"UseBlueprint\"];");

                    if (DirectEve.Interval(15000))
                    {
                        if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("Blueprint [" + TypeName + "][" + ItemId + "] Menu --> ShowInIndustryWindow");
                        //menuEntries += [[MenuLabel('UI/Industry/UseBlueprint'), self.ShowInIndustryWindow, [invItem]]]
                        //menuEntries += [[MenuLabel('UI/Inflight/POS/StoreVesselInSMA'), self.StoreVessel, (itemID, session.shipid)]]
                        //
                        ////if (DirectEve.ThreadedCall(pyShowInIndustryWindow, this.PyItem))
                        if (DirectEve.ThreadedCall(menuSvc["ShowInIndustryWindow"], this.PyItem))
                        {
                            if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("return true");
                            return true;
                        }

                        if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("return false");
                        return false;
                    }

                    if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("industryWindow not yet showing new blueprint");
                    return false;
                }

                if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("waiting: return false!");
                return false;
            }

            Log.WriteLine("[" + TypeName + "] CategoryId [" + CategoryId + "][" + CategoryName + "] != [9][Blueprint]");
            return false;
        }

        public bool UseFormula()
        {
            return false; //this is broken needs testing

            if (CategoryId != (int)CategoryID.Blueprint)
                return false;

            if (!ESCache.Instance.OpenIndustryWindow()) return false;
            var industryWindow = ESCache.Instance.Windows.OfType<DirectIndustryWindow>().FirstOrDefault();
            if (industryWindow != null)
            {
                if (industryWindow.BlueprintTypeID == null)
                    return false;

                if (industryWindow.BlueprintItemID == null)
                    return false;

                if (industryWindow.BlueprintTypeID != TypeId || industryWindow.BlueprintItemID != ItemId)
                {
                    if (industryWindow.jobID != null)
                    {
                        if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("if (industryWindow.jobID != null) This blueprint is already involved in a job?!");
                        return false;
                    }

                    if (DebugConfig.DebugIndustryBehavior) Log.WriteLine("if (industryWindow.BlueprintTypeID != TypeId || industryWindow.BlueprintItemID != ItemId) return false");
                    return false;
                }
            }

            PyObject pyUseFormula = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.menuFunctions").Attribute("UseFormula");

            if (pyUseFormula == null || !pyUseFormula.IsValid)
            {
                Log.WriteLine("UseFormula: if (pyUseFormula == null || !pyUseFormula.IsValid)");
                return false;
            }

            return DirectEve.ThreadedCall(pyUseFormula, PyItem);
        }

        public bool LaunchForSelf()
        {
            if (!DirectEve.Interval(4000, 5000))
                return false;

            if (this.GroupId == (int)Group.MobileTractor)
            {
                //
                // we should not launch this near a gate or stargate?!
                //
                //5k of an mtu
                //50k of a POS or Station, Stargates

                var call = DirectEve.GetLocalSvc("menu")["LaunchForSelf"];
                if (call.IsValid && this.PyItem.IsValid)
                {
                    if (DirectEve.ThreadedCall(call, new List<PyObject>() { this.PyItem }))
                    {
                        Time.Instance.LastLaunchForSelf = DateTime.UtcNow;
                        return true;
                    }
                }

                return false;
            }

            DirectEve.Log("Couldnt launch for self. Probably wrong type.");
            return false;
        }

        /// <summary>
        ///     Consume Booster
        /// </summary>
        /// <returns></returns>
        public bool ConsumeBooster()
        {
            if (GetBoosterConsumbableUntil() <= DateTime.UtcNow)
                return false;

            if (GroupId != (int)Group.Booster)
                return false;

            if (ItemId == 0 || !PyItem.IsValid)
                return false;

            PyObject consumeBooster = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.invItemFunctions").Attribute("ConsumeBooster");
            return DirectEve.ThreadedCall(consumeBooster, new List<PyObject> { PyItem });
        }


        //example: PyObject consumeBooster = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.invItemFunctions").Attribute("ConsumeBooster");
        //DeliverToCorpHangarFolder
        //DeliverToCorpMember
        //DeliverToMyself
        //DeliverToStructure
        //InjectSkillIntoBrain
        //Jettison
        //JettisonStructureFuel
        //LaunchShipOrContainerFromWreckOrContainer
        //LaunchSMAContents
        //LockDownBlueprint
        //RepackageItems
        //Reprocess
        //SplitStack
        //TrainNow
        //TrashInvItems
        //UnlockNlueprint

        //example: var call = DirectEve.GetLocalSvc("menu")["LaunchForSelf"];
        //
        // AbandonDrone
        // ActivateSkillExtractor
        // ActivateSkillInjector
        // AllToQuickBar - what is this?
        // AnchorObject
        // AnchorOrbital
        // AssembleAndBoardShip
        // AssembleShip
        // Assist
        // BoardSMAShip
        // BridgeToBreaconStructure
        // BridgeToFleetDeployableBeacon
        // BridgeToMember
        // CompressItemInSpace
        // CompressItemInStructure
        // CraftDynamicItem
        // DecompressGasInStructure
        // DeliverCourierContract
        // DeployStructure
        // DisbandFleet
        // Eject
        // EnterPOSPassword
        // fighters
        // FoundCourierContract
        // Guard
        // KickMember
        // LaunchForCorp
        // LaunchShipFromWreck
        // LeaveFleet
        // LeaveShip (in station?)
        // MineRepeatedly
        // OpenCrate
        // OpenMercenaryDen
        // OpenMoonMaterialsBay
        // OpenOrbitalSkyhookWindow
        // OpenPlanetCustomsOfficeImportWindow
        // OpenPOSFuelBay
        // OpenStrontiumBay
        // Scoop
        // ScoopAbandonedFighterFromSpace
        // ScoopSMA
        // ScoopToDroneBay
        // ScoopToFighterBay
        // ScooptToFleetHangar
        // SelfDestructShip
        // SelfDestructStructure
        // StoreVessel
        // StripFitting
        // TagItem
        // ViewPlanetaryProduction
        //


        public bool PlugInImplant()
        {
            if (CategoryId != (int)CategoryID.Implant)
                return false;

            if (ItemId == 0 || !PyItem.IsValid)
                return false;

            PyObject plugInImplant = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.invItemFunctions").Attribute("PlugInImplant");
            return DirectEve.ThreadedCall(plugInImplant, new List<PyObject> { PyItem });
        }

        public int FlagId
        {
            get
            {
                if (!_flagId.HasValue)
                    _flagId = (int)PyItem.Attribute("flagID");

                return _flagId.Value;
            }
            internal set => _flagId = value;
        }

        public string GivenName
        {
            get
            {
                if (_givenName == null)
                    _givenName = DirectEve.GetLocationName(ItemId);

                return _givenName;
            }
        }

        public bool IsCommonMissionItem => TypeId == 28260
                                           || TypeId == 3814
                                           || TypeId == 2076
                                           || TypeId == 25373
                                           || TypeId == 3810
                                           || TypeId == 24576
                                           || TypeId == 24766
                                           || CategoryId == (int)CategoryID.Asteroid
                                           || TypeId == (int)TypeID.AngelDiamondTag
                                           || TypeId == (int)TypeID.GuristasDiamondTag
                                           || TypeId == (int)TypeID.ImperialNavyGatePermit
                                           || GroupId == (int)Group.AccelerationGateKeys
                                           || GroupId == (int)Group.Livestock
                                           || GroupId == (int)Group.MiscSpecialMissionItems
                                           || GroupId == (int)Group.Kernite
                                           || GroupId == (int)Group.Omber
                                           || GroupId == (int)Group.Commodities
                                           //|| TypeId == (int)TypeID.MetalScraps
                                           //|| TypeId == (int)TypeID.ReinforcedMetalScraps
                                           || TypeId == (int)TypeID.Marines
                                           ;

        private List<AmmoType> _listOfAmmoTypesThisIsDefinedAs = null;

        public List<AmmoType> ListOfAmmoTypesThisIsDefinedAs
        {
            get
            {
                if (_listOfAmmoTypesThisIsDefinedAs != null)
                    return _listOfAmmoTypesThisIsDefinedAs;

                _listOfAmmoTypesThisIsDefinedAs = new List<AmmoType>();
                foreach (AmmoType definedAmmoType in DirectUIModule.DefinedAmmoTypes)
                {
                    //if (DebugConfig.DebugListOfAmmoTypesThisIsDefinedAs) Log.WriteLine("ListOfAmmoTypesThisIsDefinedAs: TypeId [" + definedAmmoType.TypeId + "] DamageType [" + definedAmmoType.DamageType + "] Range [" + definedAmmoType.Range + "]");
                    if (TypeId == definedAmmoType.TypeId)
                    {
                        _listOfAmmoTypesThisIsDefinedAs.Add(definedAmmoType);
                    }
                }

                return _listOfAmmoTypesThisIsDefinedAs;
            }
        }

        public string stringListOfAmmoTypesThisIsDefinedAs
        {
            get
            {
                if (ListOfAmmoTypesThisIsDefinedAs.Any())
                {
                    string _temp = string.Empty;
                    foreach (var AmmoTypeThisIsDefinedAs in ListOfAmmoTypesThisIsDefinedAs)
                    {
                        if (string.IsNullOrEmpty(_temp))
                        {
                            _temp = AmmoTypeThisIsDefinedAs.DamageType.ToString() + ";";
                            continue;
                        }

                        _temp = _temp + AmmoTypeThisIsDefinedAs.DamageType.ToString() + ";";
                        continue;
                    }

                    return _temp;
                }

                return string.Empty;
            }

        }

        private AmmoType _definedAsAmmoType = null;

        public AmmoType DefinedAsAmmoType
        {
            get
            {
                if (_definedAsAmmoType != null)
                    return _definedAsAmmoType;

                foreach (AmmoType definedAmmoType in DirectUIModule.DefinedAmmoTypes)
                {
                    if (DebugConfig.DebugDefinedAmmoTypes)
                    {
                        DirectItem definedAmmoType_DirectItem = new DirectItem(ESCache.Instance.DirectEve);
                        definedAmmoType_DirectItem.TypeId = definedAmmoType.TypeId;
                        Log.WriteLine("DefinedAsAmmoType: [" + definedAmmoType_DirectItem.TypeName + "] TypeId [" + definedAmmoType.TypeId + "] DamageType [" + definedAmmoType.DamageType + "] Range [" + definedAmmoType.Range + "]");
                    }
                    if (TypeId == definedAmmoType.TypeId)
                    {
                        _definedAsAmmoType = definedAmmoType;
                        return _definedAsAmmoType;
                    }
                }

                return null;
            }
        }

        public bool IsDefinedAmmoType
        {
            get
            {
                if (DirectUIModule.DefinedAmmoTypes.Count > 0)
                {
                    if (DirectUIModule.DefinedAmmoTypes.Any(i => i.TypeId == TypeId))
                        return true;
                }

                return false;
            }
        }

        public bool IsAbyssalLootItem => DirectEve.GetAbyssLootGroups() != null && DirectEve.GetAbyssLootGroups().Contains(GroupId);

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

        /**
        public bool IsDroneWeHaveLaunched
        {
            get
            {
                if (!_isDroneWeHaveLaunched.HasValue)
                    _isDroneWeHaveLaunched = DirectEve.DronesWeHaveLaunched.Contains(ItemId);

                return (bool) _isDroneWeHaveLaunched;
            }
        }
        **/

        public double? IskPerM3
        {
            get
            {
                if (AveragePrice() > 0 && Volume > 0)
                    return AveragePrice() / Volume;
                return 0;
            }
        }

        public bool IsSingleton
        {
            get
            {
                if (!_isSingleton.HasValue)
                    _isSingleton = (bool)PyItem.Attribute("singleton");

                if (!_isSingleton.HasValue && Quantity < 0 && Stacksize == 1)
                    _isSingleton = true;

                return _isSingleton.Value;
            }
            internal set => _isSingleton = value;
        }

        /**
        public bool IsCombatShip
        {
            get
            {
                //not in use!
                if (!IsValidShipToUse)
                    return false;

                if (GivenName.ToLower() != Combat.CombatShipName.ToLower())
                    return false;

                return true;
            }
        }
        **/
        public bool ShipNameMatches(string NameToFind)
        {
            if (CategoryId != (int)CategoryID.Ship)
                return false;

            if (!IsSingleton)
                return false;

            if (GivenName == null)
                return false;

            if (GivenName == string.Empty)
                return false;

            if (GivenName.ToLower() != NameToFind.ToLower())
                return false;

            return true;
        }
        public bool IsMiningShip
        {
            get
            {
                try
                {
                    //if (!IsValidShipToUse)
                    //    return false;

                    //if (IsModule && DirectEve.Modules.Where(i => !i.IsMiningModule)) //Is this a module? is it a mining module?
                    //    return false;

                    if (GroupId == (int)Group.TransportShip)
                        return false;

                    if (GroupId == (int)Group.Freighter)
                        return false;

                    if (GroupId == (int)Group.JumpFreighter)
                        return false;

                    if (GroupId == (int)Group.Dreadnaught)
                        return false;

                    if (GroupId == (int)Group.Carrier)
                        return false;

                    if (GivenName != null && !string.IsNullOrEmpty(Settings.Instance.MiningShipName) && GivenName.ToLower() == Settings.Instance.MiningShipName.ToLower())
                        return true;

                    return true;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        public bool IsModule
        {
            get
            {
                if (!DirectEve.Modules.Any())
                    return false;

                foreach (DirectUIModule module in DirectEve.Modules)
                {
                    if (ItemId == module.ItemId)
                        return true;
                }

                return false;
            }
        }

        public bool IsMiningModule
        {
            get
            {
                if (!DirectEve.Modules.Any())
                    return false;

                if (!IsModule) return false;

                if (GroupId == (int)Group.Miners)
                    return true;

                if (GroupId == (int)Group.StripMiners)
                    return true;

                if (GroupId == (int)Group.ModulatedStripMiners)
                    return true;

                return false;
            }
        }

        public bool IsStorylineHaulingShip
        {
            get
            {
                //if (!IsValidShipToUse)
                //    return false;

                if (GivenName.ToLower() != Settings.Instance.StorylineTransportShipName)
                    return false;

                return true;
            }
        }

        public bool IsValidShipToUse
        {
            get
            {
                if (string.IsNullOrEmpty(GivenName))
                    return false;

                //if (!Settings.Instance.AllowJumpFreighter)
                //{
                if (GroupId == (int)Group.JumpFreighter)
                    return false;
                //}

                //if (!Settings.Instance.AllowFreighter)
                //{
                if (GroupId == (int)Group.Freighter)
                    return false;
                //}

                //if (!Settings.Instance.AllowCarrier)
                //{
                //    if (GroupId == (int)GroupID.Carrier)
                //        return false;
                //}

                return true;
            }
        }

        public long ItemId
        {
            get
            {
                if (!_itemId.HasValue)
                    _itemId = (long)PyItem.Attribute("itemID");

                return _itemId.Value;
            }
            internal set => _itemId = value;
        }

        public DirectLocation Location
        {
            get
            {
                if (_location == null)
                {
                    _location = DirectEve.GetLocation((int)LocationId);
                    return _location;
                }

                return _location;
            }
        }

        public long LocationId
        {
            get
            {
                if (!_locationId.HasValue)
                    _locationId = (long)PyItem.Attribute("locationID");

                return _locationId.Value;
            }
            internal set => _locationId = value;
        }

        public string LocationName
        {
            get
            {
                if (string.IsNullOrEmpty(_locationName))
                {
                    _locationName = DirectEve.GetLocationName(LocationId);
                    return _locationName;
                }

                return _locationName;
            }
        }

        public List<DirectItem> Materials
        {
            get
            {
                if (_materials == null)
                {
                    _materials = new List<DirectItem>();
                    foreach (var pyMaterial in PySharp.Import("__builtin__").Attribute("cfg").Attribute("invtypematerials").DictionaryItem(TypeId).ToList())
                    {
                        var material = new DirectItem(DirectEve)
                        {
                            ItemId = -1,
                            Stacksize = -1,
                            OwnerId = -1,
                            LocationId = -1,
                            FlagId = 0,
                            IsSingleton = false,
                            TypeId = (int)pyMaterial.Attribute("materialTypeID"),
                            Quantity = (int)pyMaterial.Attribute("quantity")
                        };

                        _materials.Add(material);
                    }
                }

                return _materials;
            }
        }

        public int OwnerId
        {
            get
            {
                if (!_ownerId.HasValue)
                    _ownerId = (int)PyItem.Attribute("ownerID");

                return _ownerId.Value;
            }
            internal set => _ownerId = value;
        }

        private Vec3? _droneDamageState = null;

        public Vec3? GetDroneInBayDamageState()
        {
            if (DirectEve._entityHealthPercOverrides.TryGetValue(this.ItemId, out var res))
            {
                return new Vec3(res.Item1, res.Item2, res.Item3);
            }

            if (_droneDamageState == null)
            {
                var state = DirectEve.GetLocalSvc("tactical")["inBayDroneDamageTracker"]["droneDamageStatesByDroneIDs"];
                if (state.IsValid)
                {
                    var dict = state.ToDictionary<long>();
                    if (dict.TryGetValue(this.ItemId, out var result))
                    {
                        var timestamp = result["timestamp"].ToDateTime();
                        var msSince = (DateTime.UtcNow - timestamp).TotalMilliseconds;
                        var shieldHealth = (double)result["shieldHealth"].ToDouble(); // 0 .. 1.0
                        var rechargeRate = TryGet<float>("shieldRechargeRate"); // milliseconds
                        //var sMax = TryGet<float>("shieldCapacity");
                        var sMax = MaxShield.Value;
                        var sCurrent = sMax * shieldHealth;
                        var timeDiffMilliSeconds = msSince;
                        var rechargeTimeMilliSeconds = rechargeRate;
                        var shieldAtOffset = sMax * Math.Pow((1 + Math.Exp(5 * (-timeDiffMilliSeconds / rechargeTimeMilliSeconds)) * (Math.Sqrt(sCurrent / sMax) - 1)), 2f);
                        var percAtOffset = (shieldAtOffset / sMax);

                        //DirectEve.Log($"sMax {sMax} shieldHealth {shieldHealth} sCurrent {sCurrent} timeDiffMilliSeconds {timeDiffMilliSeconds} rechargeTimeMilliSeconds {rechargeTimeMilliSeconds} shieldAtOffset {shieldAtOffset} percAtOffset {percAtOffset}");

                        shieldHealth = percAtOffset;
                        var armorHealth = result["armorHealth"].ToFloat();
                        var hullHealth = result["hullHealth"].ToFloat();
                        //DirectEve.Log($"Timestamp {timestamp} msSince {msSince} rechargeRate {rechargeRate}");
                        _droneDamageState = new Vec3(shieldHealth, armorHealth, hullHealth);
                    }
                }
            }
            return _droneDamageState;
        }

        public static HashSet<long> RequestedDynamicItems { get; set; } = new HashSet<long>();
        public static HashSet<long> FinishedRemoteCallDynamicItems { get; set; } = new HashSet<long>();

        public static bool AllDynamicItemsLoaded => RequestedDynamicItems.Count == 0;

        public PyObject DynamicItem
        {
            get
            {
                if (_pyDynamicItem != null)
                    return _pyDynamicItem;

                if (IsDynamicItem)
                {
                    var dynamicItemSvc = DirectEve.GetLocalSvc("dynamicItemSvc");
                    if (!dynamicItemSvc.IsValid)
                        return _pyDynamicItem;

                    _pyDynamicItem = dynamicItemSvc["dynamicItemCache"].DictionaryItem(this.ItemId);

                    if ((_pyDynamicItem == null || !_pyDynamicItem.IsValid)
                        && !FinishedRemoteCallDynamicItems.Contains(this.ItemId)
                        && RequestedDynamicItems.Add(this.ItemId))
                    {
                        //DirectEve.Log($"Retrieving dynamic item settings for ItemId [{this.ItemId}]");
                        //DirectEve.ThreadedCall(dynamicItemSvc["GetDynamicItem"], this.ItemId);
                        return _pyDynamicItem;
                    }
                }
                return _pyDynamicItem;
            }
        }

        public bool IsDynamicInfoLoaded => IsDynamicItem && DynamicItem != null;

        public bool IsDynamicItem
        {
            get
            {
                var evetypes = PySharp.Import("evetypes");
                return evetypes.Call("IsDynamicType", this.TypeId).ToBool();

                //return this.TryGet<bool>("isDynamicType", true);
            }
        }

        public override T TryGet<T>(string keyname)
        {

            if (IsDynamicItem && DynamicItem != null)
            {
                var sourceTypeID = DynamicItem["sourceTypeID"].ToInt();
                var value = DirectEve.GetInvType(sourceTypeID).TryGet<T>(keyname);
                return value;
            }

            return base.TryGet<T>(keyname);
        }

        public DirectInvType OrignalDynamicItem
        {
            get
            {
                if (IsDynamicItem && DynamicItem != null)
                {
                    var sourceTypeID = DynamicItem["sourceTypeID"].ToInt();
                    return DirectEve.GetInvType(sourceTypeID);
                }
                return null;
            }
        }

        public int Quantity
        {
            get
            {
                if (!_quantity.HasValue)
                    _quantity = (int)PyItem.Attribute("quantity");

                return _quantity.Value;
            }
            internal set => _quantity = value;
        }

        public DirectSolarSystem SolarSystem
        {
            get
            {
                if (Station != null)
                {
                    return Station.SolarSystem;
                }

                return null;
            }
        }

        public string SolarSystemName
        {
            get
            {
                if (SolarSystem != null)
                {
                    return SolarSystem.Name;
                }

                return string.Empty;
            }
        }

        public long SolarSystemId
        {
            get
            {
                if (Station != null)
                {
                    return Station.SolarSystem.Id;
                }

                return 0;
            }
        }

        public int Stacksize
        {
            get
            {
                if (!_stacksize.HasValue)
                    _stacksize = (int)PyItem.Attribute("stacksize");

                return _stacksize.Value;
            }
            internal set => _stacksize = value;
        }

        public DirectStation Station
        {
            get
            {
                if (DirectEve.Stations.Count > 0)
                {
                    DirectStation _station = null;
                    DirectEve.Stations.TryGetValue((int)LocationId, out _station);
                    if (_station != null) return _station;
                    return null;
                }

                return null;
            }
        }

        public string StationName
        {
            get
            {
                if (Station != null)
                {
                    return Station.Name;
                }

                return string.Empty;
            }
        }

        public double TotalVolume => Volume * Quantity;

        public bool IsBlueprintOriginal
        {
            get
            {
                if (CategoryId == (int)CategoryID.Blueprint)
                {
                    if (Quantity == -1)
                        return true;

                    return false;
                }

                return false;
            }
        }

        public bool IsBlueprintCopy
        {
            get
            {
                if (CategoryId == (int)CategoryID.Blueprint)
                {
                    if (Quantity == -2)
                        return true;

                    return false;
                }

                return false;
            }
        }
        private PyObject _itemChecker = null;

        public PyObject ItemChecker => _itemChecker ??= PySharp.Import("menucheckers").Call("ItemChecker", PyItem);

        public bool IsTrashable()
        {
            if (ItemChecker.IsValid)
            {
                return ItemChecker.Call("OfferTrashIt").ToBool();
            }
            return false;
        }
        public new double Volume
        {
            get
            {
                var vol = base.Volume;

                if (TypeId == 3468) // plastic wraps
                    return vol;

                if (!IsSingleton) // get packaged vol
                {
                    var group = GetPackagedVolOverrideGroup(GroupId, DirectEve);
                    if (group.HasValue)
                        return group.Value;

                    var type = GetPackagedVolOverrideType(TypeId, DirectEve);
                    if (type.HasValue)
                        return type.Value;

                }

                return vol;
            }
        }

        private static Dictionary<int, float> _packagedOverridePerGroupId;

        private static Dictionary<int, float> _packagedOverridePerTypeId;

        private static float? GetPackagedVolOverrideGroup(int groupId, DirectEve de)
        {
            if (_packagedOverridePerGroupId == null)
            {
                _packagedOverridePerGroupId = new Dictionary<int, float>();
                //inventorycommon.util.packagedVolumeOverridesPerGroup
                var invCommon = de.PySharp.Import("inventorycommon");
                var dict = invCommon.Attribute("util").Attribute("packagedVolumeOverridesPerGroup").ToDictionary<int>();
                foreach (var kv in dict)
                {
                    _packagedOverridePerGroupId.Add(kv.Key, kv.Value.ToFloat());
                }
            }

            if (_packagedOverridePerGroupId.TryGetValue(groupId, out var val))
            {
                return val;
            }

            return null;
        }

        private static float? GetPackagedVolOverrideType(int typeId, DirectEve de)
        {
            if (_packagedOverridePerTypeId == null)
            {
                _packagedOverridePerTypeId = new Dictionary<int, float>();
                //inventorycommon.util.packagedVolumeOverridesPerType
                var invCommon = de.PySharp.Import("inventorycommon");
                var dict = invCommon.Attribute("util").Attribute("packagedVolumeOverridesPerType").ToDictionary<int>();
                foreach (var kv in dict)
                {
                    _packagedOverridePerTypeId.Add(kv.Key, kv.Value.ToFloat());
                }
            }

            if (_packagedOverridePerTypeId.TryGetValue(typeId, out var val))
            {
                return val;
            }

            return null;
        }

        internal PyObject PyItem
        {
            get => _pyItem;
            set
            {
                _pyItem = value;

                if (_pyItem != null && _pyItem.IsValid)
                    TypeId = (int)_pyItem.Attribute("typeID");
            }
        }

        #endregion Properties

        /**
                public int? Metalevel
                {
                    get
                    {
                        try
                        {
                            if (!_metaLevel.HasValue)
                                _metaLevel = Attributes.TryGet<int>("metalevel");

                            //(bool)PyItem.Attribute("singleton");

                            return (int)_metaLevel.Value;
                        }
                        catch (Exception)
                        {
                            return null;
                        }
                    }
                }

                public int? TechLevel
                {
                    get
                    {
                        try
                        {
                            if (!_techLevel.HasValue)
                                _techLevel = Attributes.TryGet<int>("techlevel");

                            return (int)_techLevel.Value;
                        }
                        catch (Exception)
                        {
                            return null;
                        }
                    }
                }

        **/

        #region Methods

        public bool ActivateRandomJumpKey
        {
            get
            {
                //todo fixme
                if (GroupId != (int)Group.AbyssalDeadspaceFilament)
                    return false;

                PyObject pyActivateRandomJumpKey = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.menuFunctions").Attribute("ActivateRandomJumpKey");

                if (pyActivateRandomJumpKey == null || !pyActivateRandomJumpKey.IsValid)
                {
                    Log.WriteLine("ActivateAbyssalKey: if (pyActivateRandomJumpKey == null || !pyActivateRandomJumpKey.IsValid)");
                    return false;
                }

                return DirectEve.ThreadedCall(pyActivateRandomJumpKey, PyItem);
            }
        }

        public bool ActivateVoidSpaceKey()
        {
            if (GroupId != (int)Group.VoidSpaceFilament)
                return false;

            PyObject pyActivateVoidSpaceKey = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.menuFunctions").Attribute("ActivateVoidSpaceKey");

            if (pyActivateVoidSpaceKey == null || !pyActivateVoidSpaceKey.IsValid)
            {
                Log.WriteLine("ActivateAbyssalKey: if (pyActivateVoidSpaceKey == null || !pyActivateVoidSpaceKey.IsValid)");
                return false;
            }

            return DirectEve.ThreadedCall(pyActivateVoidSpaceKey, PyItem);
        }

        public bool ActivateAbyssalKey()
        {
            if (GroupId != (int)Group.AbyssalDeadspaceFilament)
                return false;

            PyObject pyActivateAbyssalKey = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.menuFunctions").Attribute("ActivateAbyssalKey");

            if (pyActivateAbyssalKey == null || !pyActivateAbyssalKey.IsValid)
            {
                Log.WriteLine("ActivateAbyssalKey: if (pyActivateAbyssalKey == null || !pyActivateAbyssalKey.IsValid)");
                return false;
            }

            return DirectEve.ThreadedCall(pyActivateAbyssalKey, PyItem);
        }

        public bool IsSafeToUseAbyssalKeyHere
        {
            get
            {
                try
                {
                    if (!DirectEve.Session.IsInSpace || DirectEve.Session.IsInDockableLocation)
                    {
                        Log.WriteLine("IsSafeToUseAbyssalKeyHere: IsInSpace [" + DirectEve.Session.IsInSpace + "] IsInDockableLocation [" + DirectEve.Session.IsInDockableLocation + "]");
                        return false;
                    }

                    if (DirectEve.Entities == null || DirectEve.Entities.Count == 0)
                    {
                        Log.WriteLine("IsSafeToUseAbyssalKeyHere: if (DirectEve.Entities == null || !DirectEve.Entities.Any())");
                        return false;
                    }

                    // this is not really necessary: but it at least makes sense to not return true if we use this on a non-filament.
                    if (GroupId != (int)Group.AbyssalDeadspaceFilament)
                    {
                        Log.WriteLine("IsSafeToUseAbyssalKeyHere: [" + TypeName + "] TypeId [" + TypeId + "] GroupId [" + GroupId + "] is not a filament?");
                        return false;
                    }

                    if (DirectEve.Entities.Any(entity => entity != null && entity.Distance != 0 && (int)Distances.OnGridWithMe > entity.Distance))
                    {
                        if (DirectEve.Entities.Where(i => (int)Distances.OnGridWithMe > i.Distance).Any(entity => entity.GroupId == (int)Group.MobileDepot))
                        {
                            DirectEntity closestMobileDepot = DirectEve.Entities.Where(i => (int)Distances.OnGridWithMe > i.Distance).OrderBy(i => i.Distance).FirstOrDefault(entity => entity.GroupId == (int)Group.MobileDepot);
                            if (closestMobileDepot != null)
                            {
                                Log.WriteLine("IsSafeToUseAbyssalKeyHere: MobileDepot named [" + closestMobileDepot.GivenName + "] found [" + Math.Round(closestMobileDepot.Distance / 1000, 0) + "] k");
                                return false;
                            }

                            return false;
                        }

                        if (DirectEve.Entities.Where(i => (int)Distances.OnGridWithMe > i.Distance).Any(entity => entity.GroupId == (int)Group.MobileTractor))
                        {
                            DirectEntity closestMobileTractor = DirectEve.Entities.Where(i => (int)Distances.OnGridWithMe > i.Distance).OrderBy(i => i.Distance).FirstOrDefault(entity => entity.GroupId == (int)Group.MobileTractor);
                            if (closestMobileTractor != null)
                            {
                                Log.WriteLine("IsSafeToUseAbyssalKeyHere: MobileTractor named [" + closestMobileTractor.GivenName + "] found [" + Math.Round(closestMobileTractor.Distance / 1000, 0) + "] k");
                                return false;
                            }

                            return false;
                        }

                        if (DirectEve.Entities.Where(i => (int) Distances.OnGridWithMe > i.Distance).Any(entity => entity.CategoryId == (int)CategoryID.Station))
                        {
                            DirectEntity closestStation = DirectEve.Entities.Where(i => (int)Distances.OnGridWithMe > i.Distance).OrderBy(i => i.Distance).FirstOrDefault(entity => entity.CategoryId == (int)CategoryID.Station);
                            if (closestStation != null)
                            {
                                Log.WriteLine("IsSafeToUseAbyssalKeyHere: Station named [" + closestStation.GivenName + "] found [" + Math.Round(closestStation.Distance / 1000, 0) + "] k");
                                return false;
                            }

                            return false;
                        }

                        if (DirectEve.Entities.Where(i => (int) Distances.OnGridWithMe > i.Distance).Any(entity => entity.CategoryId == (int)CategoryID.Citadel))
                        {
                            DirectEntity closestCitadel = DirectEve.Entities.Where(i => (int)Distances.OnGridWithMe > i.Distance).OrderBy(i => i.Distance).FirstOrDefault(entity => entity.CategoryId == (int)CategoryID.Citadel);
                            if (closestCitadel != null)
                            {
                                Log.WriteLine("IsSafeToUseAbyssalKeyHere: Citadel named [" + closestCitadel.GivenName + "] found [" + Math.Round(closestCitadel.Distance / 1000, 0) + "] k");
                                return false;
                            }

                            return false;
                        }

                        if (DirectEve.Entities.Where(i => (int) Distances.OnGridWithMe > i.Distance).Any(entity => entity.GroupId == (int)Group.Stargate))
                        {
                            DirectEntity closestStargate = DirectEve.Entities.Where(i => (int)Distances.OnGridWithMe > i.Distance).OrderBy(i => i.Distance).FirstOrDefault(entity => entity.GroupId == (int)Group.CustomsOffice);
                            if (closestStargate != null)
                            {
                                Log.WriteLine("IsSafeToUseAbyssalKeyHere: CustomsOffice found on [" + Math.Round(closestStargate.Distance / 1000, 0) + "] k");
                                return false;
                            }

                            return false;
                        }

                        if (DirectEve.Entities.Where(i => (int)Distances.OnGridWithMe > i.Distance).Any(entity => entity.GroupId == (int)Group.POSControlTower))
                        {
                            DirectEntity closestPosControlTower = DirectEve.Entities.Where(i => (int)Distances.OnGridWithMe > i.Distance).OrderBy(i => i.Distance).FirstOrDefault(entity => entity.GroupId == (int)Group.POSControlTower);
                            if (closestPosControlTower != null)
                            {
                                Log.WriteLine("IsSafeToUseAbyssalKeyHere: CustomsOffice found on [" + Math.Round(closestPosControlTower.Distance / 1000, 0) + "] k");
                                return false;
                            }

                            return false;
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Log.WriteLine("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        //
        // todo: impliment: see menusvc.py - Note some of these are not for DirectItems but for DirectEntitys or other
        // LaunchForCorp
        // LeaveShip
        // ShowInfo (to read attributes?!)
        // MoveToDroneBay
        // FitToActiveShip
        // DropOffItem (used from space to deliver items to a hangar in a citadel you cant dock in)
        // Reprocess (already exists?)
        // AssembleShip
        // PlugInImplant
        // InjectSkill (already exists?)
        // TrashIt
        // DeliverCorpStuffTo (deliverTo Menu)
        // Compress
        // Open All kinds of bays: FighterBay, FleetHangar, FuelBay, OreHold, GasHold, MineralHold
        // SpitStack
        // UseBlueprint
        // UseFormula
        // Insure - UI/Insurance/InsuranceWindow/Commands/Insure
        // SetName
        // SimulateShip
        // StripFitting
        // BoardShip
        // LaunchFromBay
        // BoardShipFromBay
        // InviteToFleet - UI/Fleet/InvitePilotToFleet
        // FormFleetWith - UI/Fleet/FormFleetWith
        // KickFleetMember - UI/Fleet/KickFleetMember
        // MakeFleetLeader - UI/Fleet/MakeFleetLeader
        // AddPilotToWatchlist - UI/Fleet/AddPilotToWatchlist
        // LeaveFleet - UI/Fleet/LeaveMyFleet
        // TransferCorpCash - UI/Corporations/Common/TransferCorpCash
        // SendCorpInvite - UI/Corporations/Common/SendCorpInvite
        // SelfDestruct - UI/Inflight/SelfDestructShipOrPod
        // EnterPOSPassword - UI/Inflight/POS/EnterStarbasePassword
        // JumpTo - UI/Inflight/Submenus/JumpTo
        // BridgeTo - UI/Inflight/Submenus/BridgeTo
        // WarpFleet - UI/Fleet/WarpFleet
        // WarpFleetToWithin - UI/Fleet/FleetSubmenus/WarpFleetToWithin
        // AssumeStructureControl - UI/Inflight/POS/AssumeStructureControl
        // RelinquishPOSControl - UI/Inflight/POS/RelinquishPOSControl



        /// <summary>
        ///     Activate this ship
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     Fails if the current location is not the same as the current station and if its not a CategoryShip
        /// </remarks>
        public bool ActivateShip()
        {
            DirectSession.SetSessionNextSessionReady();

            if (LocationId != DirectEve.Session.StationId && !DirectEve.Session.Structureid.HasValue)
                return false;

            if (CategoryId != (int)DirectEve.Const.CategoryShip)
                return false;

            return DirectEve.ThreadedLocalSvcCall("station", "TryActivateShip", PyItem);
        }

        /// <summary>
        ///     Assembles this ship
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     Fails if the current location is not the same as the current station and if its not a CategoryShip and is not
        ///     allready assembled
        /// </remarks>
        public bool AssembleShip()
        {
            if (LocationId != DirectEve.Session.StationId && !DirectEve.Session.Structureid.HasValue)
                return false;

            if (CategoryId != (int)DirectEve.Const.CategoryShip)
                return false;

            if (IsSingleton)
                return false;

            var AssembleShip = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.invItemFunctions").Attribute("AssembleShip");
            return DirectEve.ThreadedCall(AssembleShip, new List<PyObject>() { PyItem });
        }

        public bool ActivateSkillExtractor()
        {
            if (LocationId != DirectEve.Session.StationId && !DirectEve.Session.Structureid.HasValue)
                return false;

            if (CategoryId != (int)DirectEve.Const.CategoryShip)
                return false;

            if (IsSingleton)
                return false;

            PyObject assembleShip = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.invItemFunctions").Attribute("AssembleShip");
            return DirectEve.ThreadedCall(assembleShip, new List<PyObject> { PyItem });
        }

        //public double AveragePrice()
        //{
        //    return (double)PySharp.Import("util").Call("GetAveragePrice", PyItem);
        //}

        /// <summary>
        ///     Board this ship from a ship maintenance bay!
        /// </summary>
        /// <returns>false if entity is player or out of range</returns>
        public bool BoardShipFromShipMaintBay()
        {
            if (CategoryId != (int)DirectEve.Const.CategoryShip)
                return false;

            if (IsSingleton)
                return false;

            var board = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.menuFunctions").Attribute("Board");
            return DirectEve.ThreadedCall(board, ItemId);
        }

        public bool CraftDynamicItem()
        {
            if (GroupId != (int)Group.Mutaplasmids)
                return false;

            if (ESCache.Instance.DirectEve.Session.LocationId != LocationId)
                return false;

            if (IsSingleton)
                return false;

            var board = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.menuFunctions").Attribute("CraftDynamicItem");
            return DirectEve.ThreadedCall(board, ItemId);
        }


        /// <summary>
        ///     Consume Booster
        /// </summary>
        /// <returns></returns>
        ///     Fit this item to your ship
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     Fails if the selected item is not of CategoryModule
        /// </remarks>
        public bool FitToActiveShip()
        {
            if (CategoryId != (int)DirectEve.Const.CategoryModule)
                return false;

            List<PyObject> data = new List<PyObject>
            {
                PyItem
            };

            return DirectEve.ThreadedLocalSvcCall("menu", "TryFit", data);
        }

        /// <summary>
        ///     Inject the skill into your brain
        /// </summary>
        /// <returns></returns>
        public bool InjectSkill()
        {
            if (CategoryId != (int)DirectEve.Const.CategorySkill)
                return false;

            if ((!DirectEve.Session.StationId.HasValue || LocationId != DirectEve.Session.StationId) && !DirectEve.Session.Structureid.HasValue)
                return false;

            if (ItemId == 0 || !PyItem.IsValid)
                return false;

            var injectSkillIntoBrain = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.invItemFunctions").Attribute("InjectSkillIntoBrain");
            return DirectEve.ThreadedCall(injectSkillIntoBrain, new List<PyObject> { PyItem });
        }

        public bool TrainNow()
        {
            if (CategoryId != (int)DirectEve.Const.CategorySkill)
                return false;

            if ((!DirectEve.Session.StationId.HasValue || LocationId != DirectEve.Session.StationId) && !DirectEve.Session.Structureid.HasValue)
                return false;

            if (ItemId == 0 || !PyItem.IsValid)
                return false;

            PyObject pyTrainNow = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.invItemFunctions").Attribute("TrainNow");
            return DirectEve.ThreadedCall(pyTrainNow, new List<PyObject> { PyItem });
        }

        /// <summary>
        ///     Open container window
        /// </summary>
        public bool OpenContainer()
        {
            //
            // See: eve\client\script\ui\services\menuSvcEctras\openFunctions.py\OpenCargoContainer(invItems):
            //
            if (ItemId == 0)
                return false;

            if (!IsContainerUsedToSortItemsInStations)
                return false;

            if (!IsSingleton)
                return false;

            if (!PyItem.IsValid)
                return false;

            if (!DirectEve.Session.IsInDockableLocation)
                return false;

            if (LocationId != DirectEve.Session.StationId)
                return false;

            if (Time.Instance.NextOpenCargoAction > DateTime.UtcNow)
                return false;

            PyObject pyOpenCargoContainer = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.openFunctions").Attribute("OpenCargoContainer");
            if (pyOpenCargoContainer.IsValid)
            {
                Time.Instance.NextOpenCargoAction = DateTime.UtcNow;
                return DirectEve.ThreadedCall(pyOpenCargoContainer, new List<PyObject> { PyItem });
            }

            return false;
        }

        //Is this actually needed now? Items can be sold without repackaging them so long as they have no damage?
        public bool RepackageItem()
        {
            if ((!DirectEve.Session.StationId.HasValue || LocationId != DirectEve.Session.StationId) && !DirectEve.Session.Structureid.HasValue)
                return false;

            if (ItemId == 0 || !PyItem.IsValid || !IsSingleton)
                return false;

            PyObject pyRepackageItems = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.invItemFunctions").Attribute("RepackageItem");
            return DirectEve.ThreadedCall(pyRepackageItems, new List<PyObject> { PyItem });
        }

        /// <summary>
        ///     Leave this ship
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        ///     Fails if the current location is not the same as the current station and if its not a CategoryShip
        /// </remarks>
        public bool LeaveShip()
        {
            if (ItemId != DirectEve.Session.ShipId)
                return false;

            if (Quantity > 0)
                return false;

            //if (LocationId != DirectEve.Session.StationId)
            //    return false;

            if (CategoryId != (int)DirectEve.Const.CategoryShip)
                return false;

            return DirectEve.ThreadedLocalSvcCall("station", "TryLeaveShip", PyItem);
        }

        public bool MoveToPlexVault()
        {
            if (TypeId != 29668 && TypeId != 44992)
                return false;

            var redeemCurrency = PySharp.Import("eve.client.script.ui.services.menuSvcExtras.menuFunctions").Attribute("RedeemCurrency");

            if (redeemCurrency == null || !redeemCurrency.IsValid)
                return false;

            return DirectEve.ThreadedCall(redeemCurrency, PyItem, Stacksize);
        }

        /// <summary>
        ///     Open up the quick-buy window to buy more of this item
        /// </summary>
        /// <returns></returns>
        public bool QuickBuy()
        {
            return DirectEve.ThreadedLocalSvcCall("marketutils", "Buy", TypeId, PyItem);
        }

        /// <summary>
        ///     Open up the quick-sell window to sell this item
        /// </summary>
        /// <returns></returns>
        public bool QuickSell()
        {
            return DirectEve.ThreadedLocalSvcCall("marketutils", "Sell", TypeId, PyItem);
        }

        public bool AssembleContainer()
        {
            if (!DirectEve.Interval(4000, 6000))
                return false;

            if (!DirectEve.Session.IsInDockableLocation)
                return false;

            if (IsSingleton)
                return false;

            if (DirectEve.ThreadedLocalSvcCall("menu", "AssembleContainer", new List<PyObject> { PyItem }))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Set the name of an item.  Be sure to call DirectEve.ScatterEvent("OnItemNameChange") shortly after calling this
        ///     function.  Do not call ScatterEvent from the same frame!!
        /// </summary>
        /// <remarks>See menuSvc.SetName</remarks>
        /// <param name="name">The new name for this item.</param>
        /// <returns>true if successful.  false if not.</returns>
        public bool SetName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return false;

            if (CategoryId != (int)DirectEve.Const.CategoryShip && name.Length > 20)
                return false;

            if (CategoryId != (int)DirectEve.Const.CategoryStructure && name.Length > 32)
                return false;

            if (name.Length > 100)
                return false;

            if (ItemId == 0 || !PyItem.IsValid)
                return false;

            var pyCall = DirectEve.GetLocalSvc("invCache").Call("GetInventoryMgr").Attribute("SetLabel");
            return DirectEve.ThreadedCall(pyCall, ItemId, name.Replace('\n', ' '));
        }

        /// <summary>
        ///     Drop items into People and Places
        /// </summary>
        /// <param name="directEve"></param>
        /// <param name="bookmarks"></param>
        /// <returns></returns>
        internal static bool DropInPlaces(DirectEve directEve, IEnumerable<DirectItem> bookmarks)
        {
            var data = new List<PyObject>();
            foreach (var bookmark in bookmarks)
                data.Add(directEve.PySharp.Import("eve.client.script.ui.util.uix").Call("GetItemData", bookmark.PyItem, "list"));

            return directEve.ThreadedLocalSvcCall("addressbook", "DropInPlaces", PySharp.PyNone, data);
        }

        internal static List<DirectItem> GetItems(DirectEve directEve, PyObject inventory, PyObject flag)
        {
            var items = new List<DirectItem>();
            var cachedItems = inventory.Attribute("cachedItems").ToDictionary();
            var pyItems = cachedItems.Values;

            foreach (var pyItem in pyItems)
            {
                var item = new DirectItem(directEve);
                item.PyItem = pyItem;

                // Do not add the item if the flags do not coincide
                if (flag.IsValid && (int)flag != item.FlagId)
                    continue;

                items.Add(item);
            }

            return items;
        }

        internal static bool RefreshItems(DirectEve directEve, PyObject inventory, PyObject flag)
        {
            return directEve.ThreadedCall(inventory.Attribute("InvalidateCache"));
        }

        #endregion Methods

        //        public bool ActivatePLEX()
        //        {
        //            if (TypeId != 29668)
        //                return false;
        //
        //            var ApplyPilotLicence = PySharp.Import("__builtin__").Attribute("sm").Call("RemoteSvc", "userSvc").Attribute("ApplyPilotLicence");
        //            return DirectEve.ThreadedCall(ApplyPilotLicence, ItemId);
        //        }
    }
}