extern alias SC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Lookup;
using SC::SharedComponents.EVE.ClientSettings;
using SharpDX.Direct2D1;

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


    public class AbyssalController : BaseController
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

        public ClientSetting ClientSetting => ESCache.Instance.EveAccount.ClientSetting;

        private int _homeStationId => ClientSetting.AMS.HomeStationId;

        private string _combatShipName => ClientSetting.AMS.CombatShipName;

        private string _transportShipName => ClientSetting.AMS.TransportShipName;

        private int _maximumLockedTargets => Math.Min(DirectEve.Me.MaxLockedTargets, DirectEve.ActiveShip.MaxLockedTargets);


        private IEnumerable<DirectEntity> _currentLockingTargets => DirectEve.Entities.Where(e => e.IsTargeting);

        private IEnumerable<DirectEntity> _currentLockedTargets => DirectEve.Entities.Where(e => e.IsTarget);

        private IEnumerable<DirectEntity> _currentLockedAndLockingTargets => _currentLockedTargets.Concat(_currentLockingTargets);

        private IEnumerable<DirectEntity> _trigItemCaches => DirectEve.Entities.Where(e => e.GroupId == 2009);

        private IEnumerable<DirectEntity> _wrecks =>
            DirectEve.Entities.Where(e => e.BracketType == BracketType.Wreck_NPC || e.BracketType == BracketType.Wreck);

        private double _maxTargetRange => DirectEve.ActiveShip.MaxTargetRange;

        private double _maxWeaponRange = 29000;
        private int _ammoTypeId = 27371;
        private double _propModMinCapPerc = 35;
        private double _moduleMinCapPerc = 5;
        private double _shieldBoosterMinCapPerc = 10;
        private double _shieldBoosterEnableCapPerc = 101;
        private double _droneRecoverShieldPerc = 35;

        private double? _maxDroneRange;

        //private int _filementTypeId = 47764; // gamma t1
        //private int _filementTypeId = 47900; // gamma t2
        private int _filementTypeId = 47901; // gamma t3
        //private int _filementTypeId = 47902; // gamma t4

        //private int _filementTypeId = 47899; // fire t5

        private DirectEntity _currentTarget;

        private bool _areDronesReturning => DirectEve.ActiveDrones.All(i => i.DroneState == 4);

        private long? _currentDroneTarget => DirectEve.ActiveDrones?.FirstOrDefault()?.FollowId;

        private DirectStation HomeStation =>
            DirectEve.Stations.ContainsKey(_homeStationId) ? DirectEve.Stations[_homeStationId] : null;

        private DirectEntity _midGate =>
            DirectEve.Entities.FirstOrDefault(e => e.TypeId == 47685 && e.BracketType == BracketType.Warp_Gate);

        private DirectEntity _endGate =>
            DirectEve.Entities.FirstOrDefault(e => e.TypeId == 47686 && e.BracketType == BracketType.Warp_Gate);

        private DirectEntity _nextGate => _midGate ?? _endGate;

        private int _remainingDronesInBay => DirectEve?.GetShipsDroneBay()?.Items?.Sum(i => i.Quantity) ?? 0;

        private List<DirectItem> _getDronesInBay(MarketGroup marketGroup) => DirectEve?.GetShipsDroneBay()?.Items?.Where(d => d.MarketGroupId == (int)marketGroup)?.ToList() ?? new List<DirectItem>();

        private List<DirectEntity> _getDronesInSpace(MarketGroup marketGroup) => DirectEve?.ActiveDrones?.Where(d => d.MarketGroupId == (int)marketGroup)?.ToList() ?? new List<DirectEntity>();

        private bool _isInLastRoom => _endGate != null;

        private DateTime _lastFilamentActivation;

        private DirectItem _getMTUInBay => ESCache.Instance.DirectEve.GetShipsCargo().Items.FirstOrDefault(i => i.GroupId == 1250);

        private DirectEntity _getMTUInSpace => ESCache.Instance.DirectEve.Entities.FirstOrDefault(i => i.GroupId == 1250);

        private DateTime _lastMTULaunch;

        private bool _MTUReady => _lastMTULaunch.AddSeconds(11) < DateTime.UtcNow;

        private DateTime _timeStarted;

        private double _valueLooted;


        public bool LaunchMTU()
        {
            var mtu = _getMTUInBay;

            if (mtu != null && DirectEve.Interval(2000, 3000))
            {
                _lastMTULaunch = DateTime.UtcNow;
                mtu.LaunchForSelf();
            }
            return false;
        }

        public bool ScoopMTU()
        {
            var mtu = _getMTUInSpace;
            if (mtu != null && DirectEve.Interval(2000, 3000))
            {
                mtu.Scoop();
            }
            return false;
        }

        public AbyssalController()
        {
            ESCache.Instance.InitInstances();
            Form = new AbyssalControllerForm(this);
            _timeStarted = DateTime.UtcNow;
            _valueLooted = 0;
        }

        private void UpdateIskLabel(double value)
        {
            var frm = this.Form as AbyssalControllerForm;
            frm.IskPerHLabel.Invoke(new Action(() =>
            {
                frm.IskPerHLabel.Text = Math.Round(value, 2).ToString(CultureInfo.InvariantCulture);
            }));
        }

        private void DronesEngageTarget(DirectEntity target)
        {
            var smallDronesInBay = _getDronesInBay(MarketGroup.LightScoutDrone);
            var mediumDronesInBay = _getDronesInBay(MarketGroup.MediumScoutDrone);
            var largeDronesInBay = _getDronesInBay(MarketGroup.HeavyAttackDrone);
            var smallDronesInSpace = _getDronesInSpace(MarketGroup.LightScoutDrone);
            var mediumDronesInSpace = _getDronesInSpace(MarketGroup.MediumScoutDrone);
            var largeDronesInSpace = _getDronesInSpace(MarketGroup.HeavyAttackDrone);

            // TODO: remove after dbg (_remainingDronesInBay)
            if (_remainingDronesInBay >= 5 && DirectEve.ActiveShip.GetRemainingDroneBandwidth() > 0)
            {
                if (DirectEve.Interval(1500, 2500))
                {
                    DirectEve.Log("Launching drones.");
                    //DirectEve.ActiveShip.LaunchDrones(target.IsNPCFrigate && mediumDronesInBay.Any() ? mediumDronesInBay.FirstOrDefault().TypeId : largeDronesInBay?.FirstOrDefault()?.TypeId);
                    DirectEve.ActiveShip.LaunchDrones(largeDronesInBay?.FirstOrDefault()?.TypeId ?? mediumDronesInBay?.FirstOrDefault()?.TypeId);
                }
            }

            if (target.GroupId == 2009 && DirectEve.Entities.Count(e => e.GroupId == 2009 && (e.Distance < _maxDroneRange || e.Distance < _maxWeaponRange)) > 1)
            {
                var entities = DirectEve.Entities;
                Log($"Current target is a cache, splitting drones.");
                var caches = entities.Where(e => e.GroupId == 2009 && (e.Distance < _maxDroneRange || e.Distance < _maxWeaponRange));
                var cacheIds = caches.Select(e => e.Id);
                if (DirectEve.Interval(1500, 2500))
                {
                    foreach (var cache in caches)
                    {
                        // is any drone already targeting this cache
                        if (DirectEve.ActiveDrones.Any(e => e.FollowId == cache.Id))
                            continue;

                        // get a drone which is NOT targeting a cache
                        var unusedDrone = DirectEve.ActiveDrones.FirstOrDefault(e => !cacheIds.Contains(e.FollowId));

                        // if there is none left pick one of a group which has the same follow id
                        if (unusedDrone == null)
                        {
                            var followIds = DirectEve.ActiveDrones.Where(e => e.FollowId > 0).Select(e => e.FollowId);
                            foreach (var id in followIds)
                            {
                                if (DirectEve.ActiveDrones.Count(e => e.FollowId == id) > 1)
                                {
                                    unusedDrone = DirectEve.ActiveDrones.FirstOrDefault(e => e.FollowId == id);
                                    break;
                                }
                            }
                        }
                        // pwn that thing
                        if (unusedDrone != null)
                        {
                            if (DirectEve.Interval(1))
                            {
                                Log($"Engaging target [{cache.Id}|{cache.TypeName}] with drone [{unusedDrone.Id}|{unusedDrone.TypeName}]");
                                cache.EngageTargetWithDrones(new List<long>() { unusedDrone.Id });
                            }
                        }
                    }
                }
                return;
            }

            if (DirectEve.Interval(1500, 2500))
            {
                if (target.MakeActiveTarget(false))
                {
                    Log($"Marked active Target. Engaging drones on target Id [{target.Id}] TypeName [{target.TypeName}]");
                    // only engage with non returning drones
                    target.EngageTargetWithDrones(DirectEve.ActiveDrones.Where(d => d.DroneState != 4).Select(d => d.Id).ToList());
                }
            }
        }

        private void ManageModules()
        {
            if (DirectEve.Session.IsInDockableLocation)
                return;

            var capPerc = DirectEve.ActiveShip.CapacitorPercentage;
            var currentShieldPct = DirectEve.ActiveShip.ShieldPercentage;
            var currentArmorPct = DirectEve.ActiveShip.ArmorPercentage;

            if (ESCache.Instance.Modules.Any(e => e.GroupId == 60 && e.EffectId == 7012 && e.IsActivatable)) // assault damage control effect
            {
                var lowDefenseCondition = capPerc < 15 && currentShieldPct < 30 && currentArmorPct < 50 || currentArmorPct < 20;
                if (ESCache.Instance.Entities.Any(e => e.IsPlayer && e.IsAttacking) || lowDefenseCondition)
                {
                    var module =
                        ESCache.Instance.Modules.FirstOrDefault(e => e.GroupId == 60 && e.EffectId == 7012 && e.IsActivatable);

                    if (module.IsInLimboState)
                        return;

                    if (module.IsDeactivating)
                        return;

                    if (module.IsActive)
                        return;

                    Log($"We are being attacked by a player or are really low at our defenses. Activating assault damage control.");

                    if (DirectEve.Interval(5000, 6000))
                        module.Click();
                }
            }

            List<int> groups = new List<int>() // priority, cap levels etc (custom type?)
            {
                (int)Group.ShieldHardeners,
            };



            // multispectral shield hardener

            var anyTargets = DirectEve.Entities.Any(e =>
                e.IsNPCByBracketType && e.BracketType != BracketType.NPC_Drone);

            foreach (var groupId in groups)
            {
                foreach (var mod in DirectEve.Modules.Where(e => e.GroupId == (int)Group.ShieldHardeners))
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
            foreach (var mod in DirectEve.Modules.Where(e => e.GroupId == (int)Group.ShieldBoosters))
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



                if (mod.IsInLimboState)
                    continue;
                if (capPerc >= _shieldBoosterMinCapPerc && currentShieldPct <= _shieldBoosterEnableCapPerc && DirectEve.Interval(800, 1500))
                {
                    Log($"Activating Typename [{mod.TypeName}]");
                    mod.Click();
                }
            }


            // prop mod usage only in abyss space
            if (DirectEve.Me.IsInAbyssalSpace())
            {
                foreach (var mod in DirectEve.Modules.Where(e => e.GroupId == (int)Group.Afterburner))
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

                    var scrambled = DirectEve.Entities.Any(e => e.IsWarpScramblingMe);
                    if (capPerc >= _propModMinCapPerc && !scrambled && DirectEve.Interval(800, 1500))
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

        public override void DoWork()
        {
            if (DirectEve.Me.IsInAbyssalSpace())
                State = AbyssalState.AbyssalClear;
            else
            {
                if (State == AbyssalState.AbyssalClear)
                    State = AbyssalState.Start;
            }

            if (DirectEve.ActiveShip?.GroupId == (int)Group.Capsule)
            {
                if (DirectEve.Interval(25000, 30000))
                {
                    //TODO: if we are not in abyss space nor in a station we might want to save our pod
                    Log($"Yaaay. Congratulations! We are in a capsule.");
                }
                return;
            }

            if (_maxDroneRange == null)
            {
                var fittingWnd = DirectEve.GetFittingWindow();

                if (fittingWnd == null || !fittingWnd.Ready)
                {
                    Log($"Waiting for the fitting window to be ready.");
                    return;
                }

                _maxDroneRange = fittingWnd.GetCurrentDroneControlRange;
                Log($"Read drone range from the game. Value [{_maxDroneRange}]");
                return;
            }

            if (DirectEve.Interval(8000, 10000))
            {
                if (DirectEve.Windows.OfType<DirectFittingWindow>().Any())
                {
                    var wnd = DirectEve.Windows.OfType<DirectFittingWindow>().FirstOrDefault();
                    Log($"Closing fitting window.");
                    LocalPulse = DateTime.UtcNow.AddSeconds(2);
                    wnd.Close();
                }
            }

            ManageModules();

            switch (State)
            {
                case AbyssalState.Start:
                    State = AbyssalState.UseFilament;
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
                            DirectEve.ActiveShip.MoveTo(0, 1, 0);
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
                            if (DirectEve.Interval(2500, 3500) && activationWnd.Activate())
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

                    var targets = DirectEve.Entities.Where(e => e.IsNPCByBracketType && e.BracketType != BracketType.NPC_Drone || e.GroupId == 2009).OrderBy(e => e.AbyssalTargetPriority);

                    // any targets or if there are any non empty wrecks while we don't have the mtu out yet 
                    if (targets.Any() || (_wrecks != null && _wrecks.Any(w => !w.IsEmpty) && _getMTUInSpace == null))
                    {

                        if (ESCache.Instance.MaxLockedTargets == 0)
                        {
                            Log($"We are jammed, targeting the jamming entities.");
                            var jammers = ESCache.Instance.Combat.TargetedBy.Where(t => t.IsJammingMe).ToList();
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


                        if (_currentLockedAndLockingTargets.Count() < _maximumLockedTargets)
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

                        if (_currentLockedTargets.Any() || _getMTUInSpace == null)
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

                            if (newTarget != null)
                            {
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
                            }

                            var t = targets.Where(e => e.Distance > _maxDroneRange - 4000);

                            if (!targets.Any(e => e.AbyssalTargetPriority < 3) && t.Any())
                            {
                                Log($"No other priority (ewar) on the grid, selecting the furthest away as current target.");
                                _currentTarget = t.FirstOrDefault(e => e.Distance > _maxDroneRange - 3000);
                            }

                            // move to target, pref loot container if current target is within drone range
                            var moveToTarget = _currentTarget == null || _currentTarget.Distance < _maxDroneRange - 6000
                                ? targets.Count() < 3 && _getMTUInSpace != null ? _getMTUInSpace : _nextGate
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
                            if (moveToTarget.MoveToViaAStar())
                            {
                                if (!moveToTarget.IsOrbitedByActiveShip && DirectEve.Interval(2000, 3000))
                                {
                                    if (_getMTUInSpace == null && moveToTarget == _nextGate)
                                    {
                                        Log($"Moving closer to the gate to drop the mtu.");
                                        moveToTarget.KeepAtRange(500);
                                    }
                                    else
                                    {
                                        if (moveToTarget != null && _getMTUInSpace == moveToTarget && targets.Count() < 3)
                                        {
                                            Log($"Keep at range MTU.");
                                            moveToTarget.KeepAtRange(1000);
                                        }
                                        else
                                        {
                                            Log($"Orbiting [{moveToTarget.Id}] TypeName [{moveToTarget.TypeName}]");
                                            moveToTarget.Orbit(2500);
                                        }
                                    }
                                }
                            }

                            if (_nextGate != null && _nextGate.Distance <= 3000 && _getMTUInSpace == null)
                            {
                                //TODO: any additional condition checks required to drop the mtu?
                                if (DirectEve.Interval(1500, 2500))
                                {
                                    Log($"No MTU in space, lauching the MTU.");
                                    LaunchMTU();
                                }
                            }

                            // manage drones
                            if (_currentDroneTarget == null || _currentDroneTarget != _currentTarget?.Id || DirectEve.ActiveShip.GetRemainingDroneBandwidth() > 0 ||
                                (DirectEve?.ActiveDrones?.Any(d => d.DroneState != 4 && d.FollowId != _currentTarget?.Id) ?? false))
                            {
                                if (_currentTarget != null && _currentTarget.Distance < _maxDroneRange)
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

                                if (_currentTarget == null)
                                    continue;

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

                                if (mod.CurrentCharges == 0)
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


                            if (target == null)
                            {
                                target = _currentTarget == null || _currentTarget.Distance < _maxDroneRange - 6000
                                    ? targets.Count() < 3 && _getMTUInSpace != null ? _getMTUInSpace : _nextGate
                                    : _currentTarget;
                            }

                            if (DirectEve.Interval(5000, 10000))
                                Log($"Nothing in range yet, moving to TypeName [{target.TypeName}] Distance [{target.Distance}]");
                            if (target.MoveToViaAStar())
                            {
                                target.MoveTo();
                            }
                        }
                    }
                    else // no targets
                    {
                        if (DirectEve.Interval(15000, 20000))
                            Log($"No targets left.");

                        RecallDrones();

                        if (_wrecks != null && _wrecks.Any() && _getMTUInSpace != null)
                        {

                            if (_wrecks.Any(w => !w.IsEmpty))
                            {
                                if (DirectEve.Interval(2500, 3000))
                                    Log($"An non empty wreck is still there, waiting for the MTU to scoop the content. Moving to the MTU.");

                                if (DirectEve.ActiveShip.FollowingEntity != _getMTUInSpace || _getMTUInSpace.MoveToViaAStar())
                                {
                                    if (DirectEve.Interval(2500, 3000))
                                    {
                                        _getMTUInSpace.KeepAtRange(500);
                                    }
                                }
                                return;
                            }

                            if (DirectEve.Interval(15000, 20000))
                                Log($"MTU not empty, looting.");

                            if (_getMTUInSpace.Distance < 2500)
                            {
                                var cont = ESCache.Instance.DirectEve.GetContainer(_getMTUInSpace.Id);
                                if (cont == null)
                                {
                                    Log($"Error: Cont == null!");
                                    return;
                                }

                                if (cont.Window == null)
                                {
                                    if (DirectEve.Interval(1500, 2500) && _getMTUInSpace.OpenCargo())
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

                                if (cont.Items.Any())
                                {

                                    var totalVolume =
                                        cont.Items.Sum(i =>
                                            i.TotalVolume);
                                    var freeCargo = DirectEve.GetShipsCargo().Capacity -
                                                    DirectEve.GetShipsCargo().UsedCapacity;

                                    if (freeCargo < totalVolume)
                                    {
                                        // TODO: Keep in mind that the MTU also needs space
                                        // TODO: handle me, drop loot or whatever, but keep going. (prob needs to ignore the can in the case we are not dropping worthless loot)
                                        Log($"There is not enough free cargo left.");
                                        return;
                                    }

                                    if (DirectEve.Interval(2500, 3500))
                                    {
                                        Log($"Moving loot to current ships cargo.");
                                        foreach (var item in cont.Items)
                                        {
                                            var value = item.AveragePrice() * item.Quantity;
                                            Log(
                                                $"Item Typename[{item.TypeName}] Amount [{item.Quantity}] Value [{value}]");
                                            _valueLooted += value;
                                        }

                                        var totalMinutes = (DateTime.UtcNow - _timeStarted).TotalMinutes;
                                        if (totalMinutes != 0 && _valueLooted != 0)
                                        {
                                            var millionIskPerHour = (((_valueLooted / totalMinutes) * 60) / 1000000);
                                            UpdateIskLabel(millionIskPerHour);
                                        }
                                        DirectEve.GetShipsCargo().Add(cont.Items);
                                    }

                                    return;
                                }

                            }
                            else
                            {
                                if (DirectEve.ActiveShip.FollowingEntity != _getMTUInSpace || _getMTUInSpace.MoveToViaAStar())
                                {
                                    if (DirectEve.Interval(2500, 3000))
                                    {
                                        _getMTUInSpace.KeepAtRange(500);
                                    }
                                }

                                return;
                            }
                        }

                        if (_getMTUInSpace != null)
                        {
                            if (_getMTUInSpace.Distance < 2500)
                            {
                                if (DirectEve.Interval(2500, 3000))
                                {
                                    Log($"Scooping the MTU.");
                                    ScoopMTU();
                                }
                            }
                            else
                            {
                                Log($"Moving to the MTU.");
                                if (DirectEve.ActiveShip.FollowingEntity != _getMTUInSpace || _getMTUInSpace.MoveToViaAStar())
                                {
                                    if (DirectEve.Interval(2500, 3000))
                                    {
                                        _getMTUInSpace.KeepAtRange(500);
                                    }
                                }
                            }
                            return;
                        }


                        if (DirectEve.ActiveDrones.Any())
                        {
                            return;
                        }

                        if (_getMTUInSpace == null && _getMTUInBay == null)
                        {
                            Log($"MTU is not in space and also not in the bay. Waiting.");
                            return;
                        }

                        // we looted all, move to the gate and jump
                        if (DirectEve.Interval(15000, 20000))
                            Log($"Done. Move to the gate and jump.");


                        var gate = _endGate ?? _midGate;
                        if (gate.Distance > 2300)
                        {
                            if (gate.MoveToViaAStar())
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

        private void RecallDrones()
        {
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
        }
    }
}
