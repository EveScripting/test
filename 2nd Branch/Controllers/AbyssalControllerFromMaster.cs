extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
//using System.Text;
//using System.Threading.Tasks;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Combat;
using SC::SharedComponents.EVE;
//using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.Utility;
//using SharpDX.Direct2D1;

namespace EVESharpCore.Controllers
{
    public enum AbyssalState
    {
        Start,
        BuyItems,
        Arm,
        TravelToFilamentSpot,
        TravelToJita,
        TravelToHomeStation,
        ReplaceShip,
        ActivateShip,
        UseFilament,
        AbyssalEnter,
        AbyssalClear,
        UnloadLoot,
    }

    public enum MarketGroup
    {
        LightScoutDrone = 837,
        MediumScoutDrone = 838,
        HeavyAttackDrone = 839
    }


    public class AbyssalControllerFromMaster : BaseController
    {
        private AbyssalState _prevState;
        private AbyssalState _state;
        public AbyssalState State
        {
            get => _state;
            set
            {
                if (_state != value)
                {
                    _prevState = _state;
                    _state = value;
                    Log($"State changed from [{_prevState}] to [{_state}]");
                }
            }
        }

        public DirectEve DirectEve => ESCache.Instance.DirectEve;

        //public ClientSetting ClientSetting => ESCache.Instance.EveAccount.ClientSetting;

        //private int _homeStationId => ClientSetting.AMS.HomeStationId;

        private string _combatShipName => Combat.CombatShipName;

        private string _transportShipName => Settings.Instance.TransportShipName;

        private int _maximumLockedTargets => Math.Min(DirectEve.Me.MaxLockedTargets, DirectEve.ActiveShip.MaxLockedTargetsWithShipAndSkills);

        private IEnumerable<DirectEntity> _currentLockedTargets => DirectEve.Entities.Where(e => e.IsTargeting || e.IsTarget);

        private DirectEntity _trigItemCache => DirectEve.Entities.FirstOrDefault(e => e.TypeId == 47951);

        private DirectEntity _trigItemCacheWreck =>
            DirectEve.Entities.FirstOrDefault(e => e.BracketType == BracketType.Wreck_NPC && e.TypeName.Contains("Cache"));

        private double _maxTargetRange => DirectEve.ActiveShip.MaxTargetRange;

        private double _maxWeaponRange = 29000;
        private int _ammoTypeId = 27361;
        private double _propModMinCapPerc = 35;
        private double _moduleMinCapPerc = 5;
        private double _shieldBoosterMinCapPerc = 10;
        private double _shieldBoosterEnableCapPerc = 101;
        private double _droneRecoverShieldPerc = 35;

        private double _maxDroneRange => 100000;

        //private int _filementTypeId = 47764; // gamma t1
        //private int _filementTypeId = 47900; // gamma t2
        private int _filementTypeId = AbyssalDeadspaceFilamentTypeId ?? 0; // gamma t3
        //private int _filementTypeId = 47902; // gamma t4

        private bool _areDronesIdle => DirectEve.ActiveDrones.All(i => i.DroneState == 0);

        private bool _areDronesInFight => DirectEve.ActiveDrones.All(i => i.DroneState == 1);

        private DirectEntity _currentTarget;

        private bool _areDronesReturning => DirectEve.ActiveDrones.All(i => i.DroneState == 4);

        private long? _currentDroneTarget => DirectEve.ActiveDrones?.FirstOrDefault()?.FollowId;

        //private DirectStation HomeStation =>
        //    DirectEve.Stations.ContainsKey(_homeStationId) ? DirectEve.Stations[_homeStationId] : null;

        private DirectEntity MidGate =>
            DirectEve.Entities.FirstOrDefault(e => e.TypeId == 47685 && e.BracketType == BracketType.Warp_Gate);

        private DirectEntity EndGate =>
            DirectEve.Entities.FirstOrDefault(e => e.TypeId == 47686 && e.BracketType == BracketType.Warp_Gate);


        private List<DirectItem> GetDronesInBay(MarketGroup marketGroup) => DirectEve?.GetShipsDroneBay()?.Items?.Where(d => d.MarketGroupId == (int)marketGroup)?.ToList() ?? new List<DirectItem>();

        private List<DirectEntity> GetDronesInSpace(MarketGroup marketGroup) => DirectEve?.ActiveDrones?.Where(d => d.MarketGroupId == (int)marketGroup)?.ToList() ?? new List<DirectEntity>();

        private bool _isInLastRoom => EndGate != null;

        private DateTime _lastFilamentActivation;

        private DirectItem GetMTUInBay => ESCache.Instance.DirectEve.GetShipsCargo().Items.FirstOrDefault(i => i.GroupId == 1250);

        private DirectEntity GetMTUInSpace => ESCache.Instance.DirectEve.Entities.FirstOrDefault(i => i.GroupId == 1250);

        private DateTime _lastMTULaunch;

        private bool _MTUReady => _lastMTULaunch.AddSeconds(11) < DateTime.UtcNow;


        public bool LaunchMTU()
        {
            var mtu = GetMTUInBay;

            if (mtu != null)
            {
                _lastMTULaunch = DateTime.UtcNow;
                mtu.LaunchForSelf();
            }
            return false;
        }

        public bool ScoopMTU()
        {
            var mtu = GetMTUInSpace;
            if (mtu != null)
            {
                mtu.Scoop();
            }
            return false;
        }

        public AbyssalControllerFromMaster()
        {
            //ESCache.Instance.InitInstances();
        }

        private void DronesEngageTarget(DirectEntity target)
        {
            //var smallDronesInBay = GetDronesInBay(MarketGroup.LightScoutDrone);
            var mediumDronesInBay = GetDronesInBay(MarketGroup.MediumScoutDrone);
            var largeDronesInBay = GetDronesInBay(MarketGroup.HeavyAttackDrone);
            //var smallDronesInSpace = GetDronesInSpace(MarketGroup.LightScoutDrone);
            var mediumDronesInSpace = GetDronesInSpace(MarketGroup.MediumScoutDrone);
            var largeDronesInSpace = GetDronesInSpace(MarketGroup.HeavyAttackDrone);

            //if (target.IsNPCFrigate && largeDronesInSpace.Any() && (mediumDronesInSpace.Any() || mediumDronesInBay.Any()))
            //{
            //    if (!_areDronesReturning && DirectEve.Interval(2500, 3500))
            //    {
            //        Log($"Calling large drones to return to the bay.");
            //        DirectEve.ActiveShip.ReturnDronesToBay(largeDronesInSpace
            //            .Select(e => e.Id).ToList());
            //    }
            //    return;
            //}

            //if (((target.IsNPCCruiser || target.IsNPCBattlecruiser ||
            //      target.IsNPCBattleship) && mediumDronesInSpace.Any() && (largeDronesInSpace.Any() || largeDronesInBay.Any())))
            //{
            //    if (!_areDronesReturning && DirectEve.Interval(2500, 3500))
            //    {
            //        Log($"Calling medium drones to return to the bay.");
            //        DirectEve.ActiveShip.ReturnDronesToBay(mediumDronesInSpace
            //            .Select(e => e.Id).ToList());
            //    }
            //    return;
            //}

            if (DirectEve.ActiveShip.GetRemainingDroneBandwidth() > 0)
            {
                if (DirectEve.Interval(1500, 2500))
                {
                    DirectEve.Log("Launching drones.");
                    //DirectEve.ActiveShip.LaunchDrones(target.IsNPCFrigate && mediumDronesInBay.Any() ? mediumDronesInBay.FirstOrDefault().TypeId : largeDronesInBay?.FirstOrDefault()?.TypeId);
                    DirectEve.ActiveShip.LaunchDrones(largeDronesInBay?.FirstOrDefault()?.TypeId);
                }
            }

            if (DirectEve.Interval(1500, 2500))
            {
                if (target.MakeActiveTarget(false))
                {
                    Log($"Marked active Target. Engaging drones on target Id [{target.Id}] TypeName [{target.TypeName}]");
                    ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdDronesEngage);
                }
            }
        }

        private void ManageModules()
        {
            if (DirectEve.Session.IsInDockableLocation)
                return;

            List<int> groups = new List<int>() // priority, cap levels etc (custom type?)
            {
                (int)GroupID.ShieldHardeners,
            };

            var capPerc = DirectEve.ActiveShip.CapacitorPercentage;

            // multispectral shield hardener

            var anyTargets = DirectEve.Entities.Any(e =>
                e.IsNPCByBracketType && e.BracketType != BracketType.NPC_Drone);

            foreach (var groupId in groups)
            {
                foreach (var mod in DirectEve.Modules.Where(e => e.GroupId == (int)GroupID.ShieldHardeners))
                {
                    if (mod.IsActive)
                    {
                        if (DirectEve.Me.IsInAbyssalSpace() && !anyTargets)
                        {
                            if (mod.IsInLimboState)
                                continue;
                            Log($"Disabling mod TypeName [{mod.TypeName}] due no targets being on grid.");
                            if (DirectEve.Interval(800, 1500))
                            {
                                mod.Click();
                            }
                        }

                        if (capPerc < _moduleMinCapPerc)
                        {
                            if (mod.IsInLimboState)
                                continue;

                            Log($"Disabling mod TypeName [{mod.TypeName}] due cap too low. Perc [{capPerc}] Min required cap Perc [{_moduleMinCapPerc}]");
                            if (DirectEve.Interval(800, 1500))
                            {
                                mod.Click();
                            }
                        }
                        continue;
                    }

                    if (DirectEve.Me.IsInAbyssalSpace() && !anyTargets)
                        continue;

                    if (mod.IsInLimboState)
                        continue;
                    if (capPerc >= _moduleMinCapPerc && DirectEve.Interval(800, 1500))
                    {
                        Log($"Activating Typename [{mod.TypeName}]");
                        mod.Click();
                    }
                }
            }

            // shield booster
            foreach (var mod in DirectEve.Modules.Where(e => e.GroupId == (int)GroupID.ShieldBoosters))
            {
                if (mod.IsActive)
                {
                    if (DirectEve.Me.IsInAbyssalSpace() && !anyTargets)
                    {
                        if (mod.IsInLimboState)
                            continue;
                        Log($"Disabling mod TypeName [{mod.TypeName}] due no targets being on grid.");
                        if (DirectEve.Interval(800, 1500))
                        {
                            mod.Click();
                        }
                    }

                    if (capPerc < _shieldBoosterMinCapPerc)
                    {
                        if (mod.IsInLimboState)
                            continue;

                        Log($"Disabling mod TypeName [{mod.TypeName}] due cap too low. Perc [{capPerc}] Min required cap Perc [{_moduleMinCapPerc}]");
                        if (DirectEve.Interval(800, 1500))
                        {
                            mod.Click();
                        }
                    }
                    continue;
                }

                if (DirectEve.Me.IsInAbyssalSpace() && !anyTargets)
                    continue;

                var currentShieldPct = DirectEve.ActiveShip.ShieldPercentage;

                if (mod.IsInLimboState)
                    continue;
                if (capPerc >= _shieldBoosterMinCapPerc && currentShieldPct <= _shieldBoosterEnableCapPerc && DirectEve.Interval(800, 1500))
                {
                    Log($"Activating Typename [{mod.TypeName}]");
                    mod.Click();
                }
            }


            // prop mod only in abyss space
            if (DirectEve.Me.IsInAbyssalSpace())
            {
                foreach (var mod in DirectEve.Modules.Where(e => e.GroupId == (int)GroupID.Afterburner))
                {

                    if (mod.IsActive)
                    {
                        if (capPerc < _propModMinCapPerc)
                        {
                            if (mod.IsInLimboState)
                                continue;

                            Log($"Disabling propmod TypeName [{mod.TypeName}] due cap too low. Perc [{capPerc}] Min required cap Perc [{_propModMinCapPerc}]");
                            if (DirectEve.Interval(800, 1500))
                            {
                                mod.Click();
                            }
                        }
                        continue;
                    }

                    if (mod.IsInLimboState)
                        continue;


                    if (capPerc >= _propModMinCapPerc && DirectEve.Interval(800, 1500))
                    {
                        Log($"Activating Typename [{mod.TypeName}]");
                        mod.Click();
                    }
                }
            }

            // check drones and recover them
            var damagedDrones = DirectEve.ActiveDrones
                .Where(d => d.ShieldPct < _droneRecoverShieldPerc / 100);
            var damagedDroneIds = damagedDrones.Select(d => d.Id).ToList();
            if (damagedDroneIds.Any())
            {
                if (DirectEve.Interval(2500, 3500))
                {
                    DirectEve.ActiveShip.ReturnDronesToBay(damagedDroneIds);
                    Log($"Returning damaged drones to the bay. Perc {damagedDrones.FirstOrDefault().ShieldPct}");
                }
            }
        }

        public static string AbyssalDeadspaceBookmarkName { get; set; }
        public static string AbyssalDeadspaceFilamentName { get; set; }
        public static int? AbyssalDeadspaceFilamentsToStock { get; set; }
        public static int? AbyssalDeadspaceFilamentsToLoad
        {
            get
            {
                if (ESCache.Instance.ActiveShip == null)
                    return 1;

                if (ESCache.Instance.ActiveShip.IsFrigate)
                    return 3;

                if (ESCache.Instance.ActiveShip.IsDestroyer)
                    return 2;

                return 1;
            }
        }

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            AbyssalDeadspaceBookmarkName =
                (string)CharacterSettingsXml.Element("AbyssalDeadspaceBookmark") ?? (string)CharacterSettingsXml.Element("AbyssalDeadspaceBookmark") ??
                (string)CharacterSettingsXml.Element("AbyssalDeadspaceBookmarks") ?? (string)CharacterSettingsXml.Element("AbyssalDeadspaceBookmarks") ??
                (string)CommonSettingsXml.Element("AbyssalDeadspaceBookmark") ?? (string)CommonSettingsXml.Element("AbyssalDeadspaceBookmark") ??
                (string)CommonSettingsXml.Element("AbyssalDeadspaceBookmarks") ?? (string)CommonSettingsXml.Element("AbyssalDeadspaceBookmarks") ?? "abyssal";
            HomeBookmarkName =
                (string)CharacterSettingsXml.Element("HomeBookmark") ?? (string)CharacterSettingsXml.Element("HomeBookmark") ??
                (string)CommonSettingsXml.Element("HomeBookmark") ?? (string)CommonSettingsXml.Element("HomeBookmark") ?? "HomeBookmark";
            AbyssalDeadspaceFilamentName =
                (string)CharacterSettingsXml.Element("AbyssalDeadspaceFilamentName") ??
                (string)CommonSettingsXml.Element("AbyssalDeadspaceFilamentName") ?? "Calm Firestorm Filament";
            AbyssalDeadspaceFilamentsToStock =
                (int?)CharacterSettingsXml.Element("AbyssalDeadspaceFilamentsToStock") ??
                (int?)CommonSettingsXml.Element("AbyssalDeadspaceFilamentsToStock") ?? 1;
        }


        private static string HomeBookmarkName { get; set; }

        private static int _abyssalDeadspaceFilamentTypeId;

        public static int? AbyssalDeadspaceFilamentTypeId
        {
            get
            {
                try
                {
                    //ESCache.Instance.DirectEve.InvTypeNames.TryGetValue(AbyssalDeadspaceFilamentName, out _abyssalDeadspaceFilamentTypeId);
                    return _abyssalDeadspaceFilamentTypeId;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public override void DoWork()
        {
            if (DirectEve.Me.IsInAbyssalSpace())
                State = AbyssalState.AbyssalClear;

            ManageModules();

            switch (State)
            {
                case AbyssalState.Start:
                    _state = AbyssalState.UseFilament;
                    break;
                case AbyssalState.BuyItems:
                    break;
                case AbyssalState.Arm:
                    break;
                case AbyssalState.TravelToFilamentSpot:
                    break;
                case AbyssalState.TravelToJita:
                    break;
                case AbyssalState.TravelToHomeStation:
                    break;
                case AbyssalState.ReplaceShip:
                    break;
                case AbyssalState.ActivateShip:
                    break;
                case AbyssalState.UseFilament:

                    if (DirectEve.Me.IsInAbyssalSpace())
                    {
                        if (DirectEve.Interval(200, 5000))
                        {
                            Log($"Still in an abyss. Waiting.");
                            return;
                        }
                    }

                    if (DirectEve.Entities.Any(e => e.Distance < 1000000 && e.TypeId == 48964))
                    {
                        if (DirectEve.Interval(4000, 6000))
                        {
                            Log($"Waiting for the old abyssal trace to fade away.");
                        }
                        return;
                    }


                    if (DirectEve.Me.IsInvulnUndock)
                    {
                        if (DirectEve.Interval(2500, 3000))
                        {
                            //DirectEve.ActiveShip.MoveTo(new Vec3(0, 1, 0));
                            Log($"We are invulnerable, moving the ship to stop it.");
                            new ActionQueueAction(() =>
                            {
                                if (DirectEve.Session.IsInSpace)
                                {
                                    if (DirectEve.Interval(2500, 2500))
                                    {
                                        Log($"CmdStopShip.");
                                        DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);
                                    }
                                }
                            }).Initialize().QueueAction(2000);
                            Log($"Adding CmdStopShip action with a short delay.");
                        }
                        return;
                    }

                    var currentShipCargo = DirectEve.GetShipsCargo();
                    if (currentShipCargo == null)
                        return;

                    var activationWnd = DirectEve.Windows.OfType<DirectKeyActivationWindow>().FirstOrDefault();
                    if (activationWnd != null)
                    {
                        Log($"Key activation window found.");
                        State = AbyssalState.AbyssalEnter;
                        return;
                    }

                    if (currentShipCargo.Items.Any(e => e.TypeId == _filementTypeId))
                    {
                        var filament = currentShipCargo.Items.FirstOrDefault(e => e.TypeId == _filementTypeId);
                        if (DirectEve.Interval(3000, 4000))
                        {
                            Log($"Actvating abyssal key.");
                            filament.ActivateAbyssalKey();
                        }
                    }
                    else
                    {
                        Log($"Error: No filaments left.");
                        // goto base to search there and buy if there is none left
                    }

                    break;
                case AbyssalState.AbyssalEnter:

                    activationWnd = DirectEve.Windows.OfType<DirectKeyActivationWindow>().FirstOrDefault();
                    if (activationWnd != null)
                    {
                        if (activationWnd.IsReady)
                        {
                            Log($"Activation window is ready. Jumping into the abyss.");
                            if (DirectEve.Interval(2500, 3500) && activationWnd.ActivateKeyActivationWindow())
                            {
                                _lastFilamentActivation = DateTime.UtcNow;
                                State = AbyssalState.AbyssalClear;
                            }
                        }
                        return;
                    }

                    break;
                case AbyssalState.AbyssalClear:

                    if (!DirectEve.Me.IsInAbyssalSpace())
                    {
                        if (DirectEve.Interval(3000, 4000))
                            Log($"Not in abyss space yet. Waiting");
                        return;
                    }

                    if (!DirectEve.Session.IsInSpace)
                    {
                        if (DirectEve.Interval(15000, 20000))
                            Log($"Not in space? We probably are pretty much pod spinning in a station and borrowed our ship to someone.");
                        return;
                    }

                    var targets = DirectEve.Entities.Where(e => e.IsNPCByBracketType && e.BracketType != BracketType.NPC_Drone).OrderBy(e => e.AbyssalTargetPriority);

                    if (targets.Any())
                    {

                        if (ESCache.Instance.MaxLockedTargets == 0)
                        {
                            Log($"We are jammed, targeting the jamming entities.");
                            var jammers = Combat.TargetedBy.Where(t => t._directEntity.IsJammingMe || t._directEntity.IsTryingToJamMe || t._directEntity.IsJammingEntity).ToList();
                            foreach (var jammer in jammers)
                            {
                                if (!jammer.IsTargeting && !jammer.IsTarget && DirectEve.Interval(3500, 4500))
                                {
                                    Log($"Targeting jammer [{jammer.Id}] TypeName [{jammer.TypeName}].");
                                    jammer.LockTarget("");
                                }
                            }
                        }

                        if (DirectEve.Interval(15000, 20000))
                            Log($"We have targets to target.");


                        if (_currentLockedTargets.Count() < _maximumLockedTargets)
                        {
                            foreach (var target in targets.Where(e =>
                                !e.IsTarget && !e.IsTargeting && e.Distance < _maxTargetRange))
                            {
                                if (DirectEve.Interval(1))
                                {
                                    Log(
                                        $"Targeting Id [{target.Id}] TypeName [{target.TypeName}] TargetPriority [{target.AbyssalTargetPriority}]");
                                    target.LockTarget();
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }

                        // TODO: unlock targets if there is any not locked neut etc

                        if (_currentLockedTargets.Any())
                        {
                            if (_currentLockedTargets.Any(e => e.Distance >= _maxTargetRange))
                            {
                                var targetToUnlock =
                                    _currentLockedTargets.FirstOrDefault(e => e.Distance > _maxTargetRange);
                                if (DirectEve.Interval(1))
                                {
                                    Log(
                                        $"Unlocking target due being out of range [{targetToUnlock.Id}] TypeName [{targetToUnlock.TypeName}]");
                                    targetToUnlock.UnlockTarget();
                                }
                            }

                            _currentTarget = _currentLockedTargets.FirstOrDefault(e => e.Id == _currentTarget?.Id);


                            var newTarget = _currentLockedTargets.OrderBy(e => e.AbyssalTargetPriority).ThenBy(e => e.ShieldPct + e.ArmorPct + e.StructurePct)
                                .ThenBy(e => e.TypeName).ThenBy(e => e.Distance).FirstOrDefault();

                            if (_currentTarget == null)
                            {
                                _currentTarget = newTarget;
                                Log($"Set new current target TypeName [{_currentTarget.TypeName}]");
                            }

                            if (newTarget.AbyssalTargetPriority < _currentTarget.AbyssalTargetPriority)
                            {
                                _currentTarget = newTarget;
                                Log($"Set new current target TypeName [{_currentTarget.TypeName}] (due lower priority present)");
                            }

                            if (_currentTarget.Distance > _maxDroneRange && _currentTarget != newTarget)
                            {
                                _currentTarget = newTarget;
                                Log($"Set new current target TypeName [{_currentTarget.TypeName}] (due outside drone range)");
                            }

                            var t = targets.Where(e => e.Distance > _maxDroneRange - 4000);

                            if (!targets.Any(e => e.AbyssalTargetPriority < 2) && t.Any())
                            {
                                Log($"No other priority (ewar) on the grid, selecting the furthest away as current target.");
                                _currentTarget = t.FirstOrDefault(e => e.Distance > _maxDroneRange - 3000);
                            }

                            // move to target, pref loot container if current target is within drone range
                            var moveToTarget = _currentTarget == null || _currentTarget.Distance < _maxDroneRange - 6000
                                ? (_trigItemCache ?? _trigItemCacheWreck)
                                : _currentTarget;

                            if (moveToTarget == null)
                            {
                                if (DirectEve.Interval(1500, 2500))
                                {
                                    Log($"MoveToTarget == null");
                                }
                                return;
                            }

                            //moving into range
                            //if (moveToTarget.MoveToViaAStar())
                            {
                                if (!moveToTarget.IsOrbitedByActiveShip && DirectEve.Interval(2000, 3000))
                                {
                                    Log($"Orbiting [{moveToTarget.Id}] TypeName [{moveToTarget.TypeName}]");
                                    moveToTarget.Orbit(2000);
                                }
                            }

                            // manage drones
                            if (_currentDroneTarget == null || _currentDroneTarget != _currentTarget.Id || DirectEve.ActiveShip.GetRemainingDroneBandwidth() > 0)
                            {
                                if (_currentTarget.Distance < _maxDroneRange)
                                {
                                    DronesEngageTarget(_currentTarget);
                                }
                            }

                            // manage weapons
                            foreach (var mod in DirectEve.Weapons)
                            {
                                if (mod.CurrentCharges == 0 && mod.CanBeReloaded && !mod.IsInLimboState && !mod.IsActive)
                                {
                                    if (DirectEve.GetShipsCargo() != null)
                                    {
                                        var ammo = DirectEve.GetShipsCargo().Items
                                            .FirstOrDefault(e => e.TypeId == _ammoTypeId && e.Quantity >= 10);

                                        if (ammo != null)
                                        {
                                            if (DirectEve.Interval(1500, 2500))
                                            {
                                                Log($"Reloading weapons with type [{_ammoTypeId}]");
                                                mod.ChangeAmmo(ammo);
                                            }
                                            return;
                                        }
                                        else
                                        {
                                            if (DirectEve.Interval(10000, 15000))
                                            {
                                                Log($"No ammo charges of type [{_ammoTypeId}] left.");
                                            }
                                        }
                                    }
                                    return;
                                }

                                if (mod.IsActive)
                                {
                                    if (mod.TargetId != _currentTarget.Id || _currentTarget.Distance >= _maxWeaponRange)
                                    {
                                        if (DirectEve.Interval(800, 1200))
                                        {

                                            if (mod.IsInLimboState)
                                                continue;

                                            Log(
                                                $"Deactivating, wrong target id on module our out of range. TypeName [{mod.TypeName}]");
                                            mod.Click();
                                        }
                                    }

                                    continue;
                                }

                                if (mod.IsInLimboState)
                                    continue;

                                if (_currentTarget.Distance < _maxWeaponRange && DirectEve.Interval(800, 1200))
                                {
                                    Log(
                                        $"Activaing [{mod.TypeName}] on [{_currentTarget.TypeName}] Dist [{_currentTarget.Distance}]");
                                    mod.Activate(_currentTarget.Id);
                                }

                            }
                        }
                        else
                        {
                            var target = targets.OrderBy(e => e.Distance).FirstOrDefault();
                            // targets are there but not in range
                            if (DirectEve.Interval(5000, 10000))
                                Log($"Nothing in range yet, moving to TypeName [{target.TypeName}] Distance [{target.Distance}]");
                            //if (target.MoveToViaAStar())
                            {
                                target.MoveTo();
                            }
                        }
                    }
                    else // no targets
                    {
                        if (DirectEve.Interval(15000, 20000))
                            Log($"No targets left. Going to loot.");

                        if (_trigItemCache != null)
                        {
                            if (!_trigItemCache.IsTargeting && !_trigItemCache.IsTarget)
                            {
                                if (_trigItemCache.Distance <= _maxTargetRange)
                                {
                                    if (DirectEve.Interval(1))
                                    {
                                        Log($"Locking trig item cache.");
                                        _trigItemCache.LockTarget();
                                    }
                                }
                                else
                                {
                                    //_trigItemCache.MoveToViaAStar();
                                }

                                return;
                            }

                            if (_trigItemCache.IsTarget)
                            {
                                if (_trigItemCache.Distance < _maxDroneRange)
                                {
                                    if (DirectEve.Interval(1))
                                    {
                                        if (_currentDroneTarget == null || _currentDroneTarget != _trigItemCache.Id)
                                        {
                                            if (_trigItemCache.Distance < _maxDroneRange)
                                            {
                                                Log(
                                                    $"Drones Engaging target [{_trigItemCache.Id}] TypeName [{_trigItemCache.TypeName}]");
                                                DronesEngageTarget(_trigItemCache);
                                            }
                                        }
                                    }
                                }
                                else
                                {
                                    //_trigItemCache.MoveToViaAStar();
                                }
                            }

                            return;
                        }

                        if (_trigItemCacheWreck != null)
                        {
                            if (!_trigItemCacheWreck.IsEmpty)
                            {
                                if (_trigItemCacheWreck.Distance < 2500)
                                {
                                    var cont = ESCache.Instance.DirectEve.GetContainer(_trigItemCacheWreck.Id);
                                    if (cont == null)
                                    {
                                        Log($"Error: Cont == null!");
                                        return;
                                    }

                                    if (cont.Window == null)
                                    {
                                        if (DirectEve.Interval(1500, 2500) && _trigItemCacheWreck.OpenCargo())
                                        {
                                            Log($"Opening container cargo.");
                                        }

                                        return;
                                    }

                                    if (!cont.Window.IsReady)
                                    {
                                        Log($"Container window not ready yet.");
                                        return;
                                    }

                                    var totalVolume =
                                        cont.Items.Sum(i =>
                                            i.TotalVolume); // it is qty * volume or stacksize * volume?
                                    var freeCargo = DirectEve.GetShipsCargo().Capacity -
                                                    DirectEve.GetShipsCargo().UsedCapacity;

                                    if (freeCargo < totalVolume)
                                    {
                                        // TODO: handle me, drop loot or whatever, but keep going. (prob needs to ignore the can in the case we are not dropping worthless loot)
                                        Log($"There is not enough free cargo left.");
                                        return;
                                    }

                                    if (DirectEve.Interval(2500, 3500))
                                    {
                                        Log($"Moving loot to current ships cargo.");
                                        foreach (var item in cont.Items)
                                        {
                                            Log(
                                                $"Item Typename[{item.TypeName}] Amount [{item.Quantity}] Value [{item.AveragePrice() * item.Quantity}]");
                                        }

                                        DirectEve.GetShipsCargo().Add(cont.Items);
                                    }

                                }
                                else
                                {
                                    //if (_trigItemCacheWreck.MoveToViaAStar())
                                    {
                                        _trigItemCacheWreck.KeepAtRange(500);
                                    }
                                }

                                return;
                            }
                            else
                            {
                                // we looted all, move to the gate and jump
                                if (DirectEve.Interval(15000, 20000))
                                    Log($"Done. Move to the gate and jump.");

                                if (DirectEve.ActiveDrones.Any())
                                {
                                    if (!_areDronesReturning && DirectEve.Interval(2500, 3500))
                                    {
                                        Log($"Calling drones to return to the bay.");
                                        DirectEve.ActiveShip.ReturnDronesToBay(DirectEve.ActiveDrones
                                            .Select(e => e.Id).ToList());
                                    }
                                    return;
                                }

                                var gate = EndGate ?? MidGate;
                                if (gate.Distance > 2300)
                                {
                                    //if (gate.MoveToViaAStar())
                                    {
                                        gate.MoveTo();
                                    }

                                    return;
                                }

                                // at this point we are close to the gate and can jump based on mid/end gate
                                if (_isInLastRoom)
                                {
                                    if (DirectEve.Interval(2500, 3000))
                                    {
                                        Log($"ActivateAbyssalEndGate");
                                        gate.ActivateAbyssalEndGate();
                                        State = AbyssalState.UseFilament;
                                    }
                                }
                                else
                                {
                                    if (DirectEve.Interval(2500, 3000))
                                    {
                                        Log($"ActivateAbyssalAccelerationGate");
                                        gate.ActivateAbyssalAccelerationGate();
                                    }
                                }
                            }
                        }
                    }

                    break;
                case AbyssalState.UnloadLoot:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }
    }
}
