extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Combat;
using SC::SharedComponents.EVE;

namespace EVESharpCore.Framework
{
    public partial class DirectEntity
    {
        #region Properties

        public bool IsAccelerationGate
        {
            get
            {
                if (GroupId == (int)Group.AccelerationGate)
                {
                    if (DirectEve.Session.IsAbyssalDeadspace)
                    {
                        if (TypeId == (int)TypeID.AbyssPvPGate)
                            return false;
                    }

                    return true;
                }

                if (BracketType == BracketType.Warp_Gate)
                    return true;

                return false;
            }
        }

        public bool IsBadIdea
        {
            get
            {
                if (GroupId == (int)Group.InvadingPrecursorEntities)
                    return false;

                bool result = false;
                result |= BracketType == BracketType.Asteroid_Billboard;
                result |= BracketType == BracketType.Sentry_Gun;
                result |= BracketType == BracketType.Stargate;
                result |= BracketType == BracketType.Station;
                result |= BracketType == BracketType.Wormhole;
                result |= BracketType == BracketType.Warp_Gate;
                result |= BracketType == BracketType.Planetary_Customs_Office;
                result |= BracketType == BracketType.Planetary_Customs_Office_NPC;
                result |= BracketType == BracketType.Capsule;
                result |= GroupId == (int)Group.ConcordDrone;
                result |= GroupId == (int)Group.PoliceDrone;
                result |= GroupId == (int)Group.CustomsOfficial;
                result |= GroupId == (int)Group.Billboard;
                result |= GroupId == (int)Group.Stargate;
                result |= CategoryId == (int)CategoryID.Station;
                result |= CategoryId == (int)CategoryID.Citadel;
                result |= GroupId == (int)Group.SentryGun;
                result |= GroupId == (int)Group.Capsule;
                result |= GroupId == (int)Group.MissionContainer;
                result |= GroupId == (int)Group.CustomsOffice;
                result |= GroupId == (int)Group.GasCloud;
                result |= GroupId == (int)Group.ConcordBillboard;
                result |= IsFrigate;
                result |= IsCruiser;
                result |= IsBattlecruiser;
                result |= IsBattleship;
                result |= !IsAttacking && IsPlayer;
                result |= BracketType == BracketType.Navy_Concord_Customs;
                return result;
            }
        }

        public bool IsAbyssalMarshal => TypeId == 56176 || TypeId == 56177 || TypeId == 56178;
        public bool IsAbyssalLeshak => IsNPCBattleship && Name.Contains("Leshak");
        public bool IsAbyssalKaren => IsNPCBattleship && Name.Contains("Karybdis ");
        public bool IsAbyssalDroneBattleship => IsNPCBattleship && Name.Contains("Abyssal Overmind");
        public bool IsAbyssalLucidDeepwatcherBattleship => IsNPCBattleship && Name.Contains("Lucid Deepwatcher");

        public bool IsWithinThisDistanceOfOurDrones(int _distance)
        {
            try
            {
                if (!Drones.UseDrones)
                    return false;

                if (!Drones.AllDronesInSpace.Any())
                    return false;

                if (_distance > (int)DistanceTo(Drones.AllDronesInSpace.Select(i => i._directEntity)))
                {
                    return true;
                }

                return false;

            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
                return false;
            }
        }

        public bool IsTargetingDrones
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer || ESCache.Instance.ActiveShip.IsAssaultShip)
                    return false;

                //if (IsExplicitRemoteRepairEntity)
                //    return false;

                if (IsYellowBoxing)
                    return true;

                return false;
            }
        }

        //public bool IsTargetingDrones => IsYellowBoxing && !IsExplicitRemoteRepairEntity;

        public bool IsExplicitRemoteRepairEntity => (IsRemoteArmorRepairingEntity || IsRemoteShieldRepairingEntity) && !GetAttributesInvType().ContainsKey("damageMultiplier");


        public static void InvalidateCache()
        {
        }

        internal static void ClearPerPocketCache()
        {
            DirectEntity.AStarErrors = 0;
            _followIdCacheDrones = new Dictionary<long, (long, DateTime)>();
        }


        private void AdjustAbyssalTargetPriority_Common()
        {
            if (_abyssalTargetPriority == null)
                _abyssalTargetPriority = 50;

            AdjustAbyssalTargetPriority_Neuts();

            AdjustAbyssalTargetPriority_Nodes();

            if (!ESCache.Instance.ActiveShip.IsFrigate && !ESCache.Instance.ActiveShip.IsAssaultShip && !ESCache.Instance.ActiveShip.IsDestroyer)
            {
                if (ShieldPct < 0.75 && Combat.PotentialCombatTargets.Any(i => i.Id == Id))
                {
                    if (Drones.AllDronesInSpace.Count(i => i._directEntity.FollowEntity != null && i._directEntity.FollowEntity.Id == Id) >= 2)
                        _abyssalTargetPriority = 0.00777;
                }
            }

            //speed clouds == bad!
            if (IsLocatedWithinSpeedCloud)
            {
                if (Velocity > 2000)
                {
                    _abyssalTargetPriority = _abyssalTargetPriority + .66;
                }
                else if (Velocity > 1500)
                {
                    _abyssalTargetPriority = _abyssalTargetPriority + .066;
                }
            }

            //adjust priority to sort this one till after others
            if (!ESCache.Instance.EveAccount.UseFleetMgr && !ESCache.Instance.ActiveShip.IsShipWithDroneBonuses && !IsInNPCsOptimalRange)
            {
                _abyssalTargetPriority = _abyssalTargetPriority + .0005;
            }

            AdjustAbyssalTargetPriority_Health();

            //adjust priority to sort this one till after others
            if (IsTooCloseToSmallDeviantAutomataSuppressor)
                _abyssalTargetPriority = _abyssalTargetPriority + .002;

            //adjust priority to sort this one till after others
            if (IsTooCloseToMediumDeviantAutomataSuppressor)
                _abyssalTargetPriority = _abyssalTargetPriority + .001;

            AdjustAbyssalTargetPriority_StormBringer();

            if (IsWithinThisDistanceOfOurDrones(5000))
            {
                _abyssalTargetPriority = _abyssalTargetPriority - .000003; //lower is better!
            }
            else if (IsWithinThisDistanceOfOurDrones(10000))
            {
                _abyssalTargetPriority = _abyssalTargetPriority - .000002; //lower is better!
            }
            else if (IsWithinThisDistanceOfOurDrones(15000))
            {
                _abyssalTargetPriority = _abyssalTargetPriority - .000001; //lower is better!
            }
        }

        private void AdjustAbyssalTargetPriority_Neuts()
        {
            if (!ESCache.Instance.EveAccount.UseFleetMgr && Combat.PotentialCombatTargets.Where(i => i.IsReadyToTarget && i.IsNeutralizingMe && i.IsTargetedBy).Sum(e => e._directEntity.GigaJouleNeutedPerSecond) >= _maxGigaJoulePerSecondTank)
            {
                if (!ESCache.Instance.EveAccount.UseFleetMgr && IsNeutralizingMe)
                {
                    if (IsNPCBattleship)
                        _abyssalTargetPriority = 6;
                    if (IsNPCCruiser)
                        _abyssalTargetPriority = 5;
                    if (IsNPCDestroyer)
                        _abyssalTargetPriority = 4;
                    if (IsNPCFrigate)
                        _abyssalTargetPriority = 3;
                    if (TypeName.ToLower().Contains("Starving Vedmak".ToLower()))
                        _abyssalTargetPriority = 2;
                }

                foreach (var neutingNPC in Combat.PotentialCombatTargets.Where(i => i.IsNeutralizingMe && i.IsTargetedBy))
                {
                    if (DebugConfig.DebugPotentialCombatTargets) Log.WriteLine("neutingNPC [" + neutingNPC.TypeName + "] Priority [" + _abyssalTargetPriority + "] GigaJouleNeutedPerSecond [" + neutingNPC._directEntity.GigaJouleNeutedPerSecond + "]");
                }
            }

            if (TypeName.ToLower().Contains("Starving Damavik"))
                _abyssalTargetPriority = 1.8;
        }

        private void AdjustAbyssalTargetPriority_Nodes()
        {
            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
            {
                if (IsAbyssalBioAdaptiveCache && !Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                {
                    if (IsInRangeOfWeapons || (Drones.DronesKillHighValueTargets && Drones.DroneControlRange > Distance))
                    {
                        if (ESCache.Instance.EveAccount.UseFleetMgr)
                        {
                            _abyssalTargetPriority = 10.2;

                            if (ESCache.Instance.EveAccount.IsLeader) //if in a fleet only one of us needs to kill the cache, not all 3 ffs!
                                _abyssalTargetPriority = 1.3;
                        }
                        else _abyssalTargetPriority = 1.3;
                    }
                }

                if (IsAbyssalDeadspaceTriglavianExtractionNode && !Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                {
                    if (IsInRangeOfWeapons || (Drones.DronesKillHighValueTargets && Drones.DroneControlRange > Distance))
                    {
                        if (ESCache.Instance.EveAccount.UseFleetMgr)
                        {
                            _abyssalTargetPriority = 10.1;

                            if (ESCache.Instance.EveAccount.IsLeader) //if in a fleet only one of us needs to kill the cache, not all 3 ffs!
                                _abyssalTargetPriority = 1.2;
                        }
                        else _abyssalTargetPriority = 1.2;
                    }
                }
            }

            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
            {
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                {

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
                    {
                        _abyssalTargetPriority = 50;
                        return;
                    }
                }
            }

            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
            {
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
                    {
                        _abyssalTargetPriority = 50;
                        return;
                    }
                }
            }

            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
            {
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser && i.Name.Contains("Lucifer Cynabal")))
                    {
                        _abyssalTargetPriority = 50;
                        return;
                    }
                }
            }

            if (IsAbyssalBioAdaptiveCache)
            {
                if (IsInRangeOfWeapons || (Drones.DronesKillHighValueTargets && Drones.DroneControlRange > Distance))
                {
                    if (ESCache.Instance.EveAccount.UseFleetMgr)
                    {
                        _abyssalTargetPriority = 10.2;

                        if (ESCache.Instance.EveAccount.IsLeader) //if in a fleet only one of us needs to kill the cache, not all 3 ffs!
                            _abyssalTargetPriority = .2;
                    }
                    else _abyssalTargetPriority = .2;
                }
            }

            if (IsAbyssalDeadspaceTriglavianExtractionNode)
            {
                if (IsInRangeOfWeapons || (Drones.DronesKillHighValueTargets && Drones.DroneControlRange > Distance))
                {
                    if (ESCache.Instance.EveAccount.UseFleetMgr)
                    {
                        _abyssalTargetPriority = 10.1;

                        if (ESCache.Instance.EveAccount.IsLeader) //if in a fleet only one of us needs to kill the cache, not all 3 ffs!
                            _abyssalTargetPriority = .1;
                    }
                    else _abyssalTargetPriority = .1;
                }
            }
        }

        private void AdjustAbyssalTargetPriority_Health()
        {
            if (.25 > ArmorPct)
            {
                if (ESCache.Instance.ActiveShip.ShieldPercentage < 60)
                {
                    _abyssalTargetPriority = _abyssalTargetPriority - .06;
                    return;
                }

                _abyssalTargetPriority = _abyssalTargetPriority - .00006;
                return;
            }

            if (.50 > ArmorPct)
            {
                if (ESCache.Instance.ActiveShip.ShieldPercentage < 60)
                {
                    _abyssalTargetPriority = _abyssalTargetPriority - .05;
                    return;
                }

                _abyssalTargetPriority = _abyssalTargetPriority - .00005;
                return;
            }

            if (.75 > ArmorPct)
            {
                if (ESCache.Instance.ActiveShip.ShieldPercentage < 60)
                {
                    _abyssalTargetPriority = _abyssalTargetPriority - .04;
                    return;
                }

                _abyssalTargetPriority = _abyssalTargetPriority - .00004;
                return;
            }


            if (.25 > ShieldPct)
            {
                if (ESCache.Instance.ActiveShip.ShieldPercentage < 60)
                {
                    _abyssalTargetPriority = _abyssalTargetPriority - .03;
                    return;
                }

                _abyssalTargetPriority = _abyssalTargetPriority - .00003;
                return;
            }

            if (.50 > ShieldPct)
            {
                if (ESCache.Instance.ActiveShip.ShieldPercentage < 60)
                {
                    _abyssalTargetPriority = _abyssalTargetPriority - .02;
                    return;
                }

                _abyssalTargetPriority = _abyssalTargetPriority - .00002;
                return;
            }

            if (.75 > ShieldPct)
            {
                if (ESCache.Instance.ActiveShip.ShieldPercentage < 60)
                {
                    _abyssalTargetPriority = _abyssalTargetPriority - .01;
                    return;
                }

                _abyssalTargetPriority = _abyssalTargetPriority - .00001;
                return;
            }
        }

        private void AdjustAbyssalTargetPriority_StormBringer()
        {
            if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
            {
                if (NumOfPCTInVontonArcRange == 2)
                    _abyssalTargetPriority = _abyssalTargetPriority - .002;
                if (NumOfPCTInVontonArcRange == 3)
                    _abyssalTargetPriority = _abyssalTargetPriority - .003;
                if (NumOfPCTInVontonArcRange == 4)
                    _abyssalTargetPriority = _abyssalTargetPriority - .004;
                if (NumOfPCTInVontonArcRange == 5)
                    _abyssalTargetPriority = _abyssalTargetPriority - .005;
                if (NumOfPCTInVontonArcRange == 6)
                    _abyssalTargetPriority = _abyssalTargetPriority - .006;
                if (NumOfPCTInVontonArcRange == 7)
                    _abyssalTargetPriority = _abyssalTargetPriority - .007;
                if (NumOfPCTInVontonArcRange == 8)
                    _abyssalTargetPriority = _abyssalTargetPriority - .008;
                if (NumOfPCTInVontonArcRange == 9)
                    _abyssalTargetPriority = _abyssalTargetPriority - .009;
                if (NumOfPCTInVontonArcRange == 10)
                    _abyssalTargetPriority = _abyssalTargetPriority - .010;
                if (NumOfPCTInVontonArcRange == 11)
                    _abyssalTargetPriority = _abyssalTargetPriority - .011;
                if (NumOfPCTInVontonArcRange == 12)
                    _abyssalTargetPriority = _abyssalTargetPriority - .012;
                if (NumOfPCTInVontonArcRange == 13)
                    _abyssalTargetPriority = _abyssalTargetPriority - .013;
                if (NumOfPCTInVontonArcRange == 14)
                    _abyssalTargetPriority = _abyssalTargetPriority - .014;
                if (NumOfPCTInVontonArcRange == 15)
                    _abyssalTargetPriority = _abyssalTargetPriority - .015;
                if (NumOfPCTInVontonArcRange == 16)
                    _abyssalTargetPriority = _abyssalTargetPriority - .016;
                if (NumOfPCTInVontonArcRange == 17)
                    _abyssalTargetPriority = _abyssalTargetPriority - .017;
                if (NumOfPCTInVontonArcRange == 18)
                    _abyssalTargetPriority = _abyssalTargetPriority - .018;
                if (NumOfPCTInVontonArcRange == 19)
                    _abyssalTargetPriority = _abyssalTargetPriority - .019;
                if (NumOfPCTInVontonArcRange == 20)
                    _abyssalTargetPriority = _abyssalTargetPriority - .020;

                if (IsAbyssalDroneBattleship)
                    _abyssalTargetPriority = .0001; //Sprint to the battleship, no distractions! We need to kill it asap and it takes a while to kill it! Everything else should die to the lightning damage
            }
        }


        private double AbyssalTargetPriority_DevotedCruiserSpawn
        {
            get
            {
                //https://caldarijoans.streamlit.app/Room_Overviews_and_Tips
                //This room can easily be kited to help reduce incoming damage.
                //The Devoted Smith neutralizes cap for 75% of what a Devoted Knight can do. They are low HP enemies that can be killed early to help with capacitor.
                //
                _abyssalTargetPriority = 50;

                if (IsNPCFrigate)
                    _abyssalTargetPriority = 50;
                if (IsNPCCruiser)
                    _abyssalTargetPriority = 40;
                if (TypeName.ToLower().Contains("Devoted Torchbearer".ToLower())) //target painter: https://everef.net/type/56168
                    _abyssalTargetPriority = 39.8;
                if (TypeName.ToLower().Contains("Devoted Herald".ToLower())) //tracking disruptor: https://everef.net/type/56167
                    _abyssalTargetPriority = 39.7;
                if (TypeName.ToLower().Contains("Devoted Lookout".ToLower()))
                    _abyssalTargetPriority = 39.6;
                if (TypeName.ToLower().Contains("Devoted Trapper".ToLower())) //Scram (anti-MWD): https://everef.net/type/56164
                    _abyssalTargetPriority = 39.5;
                if (TypeName.ToLower().Contains("Devoted Hunter".ToLower())) //dps: https://everef.net/type/56138
                    _abyssalTargetPriority = 39.4;
                if (TypeName.ToLower().Contains("Devoted Priest".ToLower()))//webs: https://everef.net/type/56163
                    _abyssalTargetPriority = 39.3;
                if (TypeName.ToLower().Contains("Devoted Smith".ToLower())) //Neuts: https://everef.net/type/56165
                    _abyssalTargetPriority = 39.2;

                if (IsRemoteRepairEntity)
                    _abyssalTargetPriority = 29.31;

                if (IsWebbingEntity)
                {
                    _abyssalTargetPriority = 9.666;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWebbingMe) //webs
                        _abyssalTargetPriority = 9.665;

                    if (TypeName.ToLower().Contains("Devoted Fisher".ToLower())) //webs: https://everef.net/type/56162
                        _abyssalTargetPriority = 9.662;
                }

                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                {
                    if (IsWarpScramblingEntity)
                    {
                        _abyssalTargetPriority = 8.7;
                        if (.5 > ShieldPct)
                            _abyssalTargetPriority = 8.69;
                        if (.25 > ShieldPct)
                            _abyssalTargetPriority = 8.68;
                        if (.01 > ShieldPct)
                            _abyssalTargetPriority = 8.67;
                        if (.5 > ArmorPct)
                            _abyssalTargetPriority = 8.66;
                        if (.25 > ArmorPct)
                            _abyssalTargetPriority = 8.65;
                        if (.01 > ArmorPct)
                            _abyssalTargetPriority = 8.64;
                    }

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe)
                    {
                        _abyssalTargetPriority = 8.6;
                        if (.5 > ShieldPct)
                            _abyssalTargetPriority = 8.59;
                        if (.25 > ShieldPct)
                            _abyssalTargetPriority = 8.58;
                        if (.01 > ShieldPct)
                            _abyssalTargetPriority = 8.57;
                        if (.5 > ArmorPct)
                            _abyssalTargetPriority = 8.56;
                        if (.25 > ArmorPct)
                            _abyssalTargetPriority = 8.55;
                        if (.01 > ArmorPct)
                            _abyssalTargetPriority = 8.54;
                    }

                    if (IsWarpScramblingEntity && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (IsWarpScramblingEntity && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.4;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.3;

                    if (IsWarpScramblingEntity && IsNPCFrigate)
                        _abyssalTargetPriority = 8.2;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCFrigate)
                        _abyssalTargetPriority = 8.1;
                }

                if (IsTargetingDrones) // focus targets which are focusing the drones
                {
                    _abyssalTargetPriority = 7.99;

                    if (TypeName.ToLower().Contains("Devoted Fisher".ToLower())) //https://everef.net/type/56162
                        _abyssalTargetPriority = 7.95;

                    if (TypeName.ToLower().Contains("Devoted Priest".ToLower())) //https://everef.net/type/56163
                        _abyssalTargetPriority = 7.94;
                    if (TypeName.ToLower().Contains("Devoted Torchbearer".ToLower())) //target painter: https://everef.net/type/56168
                        _abyssalTargetPriority = 7.9;
                    if (TypeName.ToLower().Contains("Devoted Herald".ToLower())) //tracking disruptor: https://everef.net/type/56167
                        _abyssalTargetPriority = 7.85;
                    if (TypeName.ToLower().Contains("Devoted Lookout".ToLower()))
                        _abyssalTargetPriority = 7.8;
                    if (TypeName.ToLower().Contains("Devoted Hunter".ToLower())) //dps: https://everef.net/type/56138
                        _abyssalTargetPriority = 7.7;

                    if (IsWebbingEntity)
                    {
                        _abyssalTargetPriority = 7.666;

                        if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWebbingMe) //webs
                            _abyssalTargetPriority = 7.665;

                        if (TypeName.ToLower().Contains("Devoted Fisher".ToLower())) //webs: https://everef.net/type/56162
                            _abyssalTargetPriority = 7.662;
                    }

                    //if (TypeName.ToLower().Contains("Devoted Knight".ToLower())) //https://everef.net/type/56148
                    //    _abyssalTargetPriority = 7.1;
                }


                if (TypeName.ToLower().Contains("Devoted Knight".ToLower()))
                    _abyssalTargetPriority = 0.02;
                if (TypeName.ToLower().Contains("Devoted Knight".ToLower()) && IsTargetingDrones)
                    _abyssalTargetPriority = 0.01;


                AdjustAbyssalTargetPriority_Common();

                return _abyssalTargetPriority.Value;
            }

        }

        private double AbyssalTargetPriority_VedmakCruiserSpawn
        {
            get
            {
                _abyssalTargetPriority = 50;

                if (IsNPCFrigate)
                    _abyssalTargetPriority = 40;
                if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                    _abyssalTargetPriority = 39.9;

                if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                    _abyssalTargetPriority = 39.7;
                if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                    _abyssalTargetPriority = 39.6;

                //Needle Lower DPS
                if (IsNPCFrigate && TypeName.ToLower().Contains("Emberneedle Tessella".ToLower())) //20 Thermal * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.5;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Strikeneedle Tessella".ToLower())) //20 Kinetic * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.4;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Blastneedle Tessella".ToLower())) //20 Explosive * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.3;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparkneedle Tessella".ToLower())) //20 EM * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.2;

                //Lance High DPS
                if (IsNPCFrigate && TypeName.ToLower().Contains("Blastlance Tessella".ToLower())) //40 Explosive * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.5;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Emberlance Tessella".ToLower())) //40 Thermal * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.4;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 EM * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.3;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Strikelance Tessella".ToLower())) //40 Kinetic * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.2;

                if (IsNPCCruiser)
                    _abyssalTargetPriority = 30;

                if (TypeName.ToLower().Contains("nullcharge") || TypeName.ToLower().Contains("ephialtes"))
                    _abyssalTargetPriority = 12.01;

                if (TypeName.ToLower().Contains("nullcharge") || TypeName.ToLower().Contains("ephialtes") && IsTargetingDrones)
                    _abyssalTargetPriority = 12;

                if (TypeName.ToLower().Contains("kikimora".ToLower()))
                {
                    _abyssalTargetPriority = 11;
                    if (TypeName.ToLower().Contains("ghosting kikimora".ToLower()))
                        _abyssalTargetPriority = 10.5;
                    if (TypeName.ToLower().Contains("striking kikimora".ToLower()))
                        _abyssalTargetPriority = 10;
                    long RemoteRepRange = 15000;
                    if (Combat.PotentialCombatTargets.Any(i => i.Id != Id && TypeName.ToLower().Contains("kikimora".ToLower()) && RemoteRepRange > i._directEntity.DistanceTo(this)))
                    {
                        _abyssalTargetPriority = 11.0666;
                    }
                }

                if (IsAbyssalBioAdaptiveCache)
                    _abyssalTargetPriority = 10.2; //used when flying a gila
                if (IsAbyssalDeadspaceTriglavianExtractionNode)
                    _abyssalTargetPriority = 10.1; //used when flying a gila

                if (IsNPCFrigate && TypeName.ToLower().Contains("Ghosting Damavik".ToLower()))
                    _abyssalTargetPriority = 9.4;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Anchoring Damavik".ToLower()))
                    _abyssalTargetPriority = 9.3;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Striking Damavik".ToLower()))
                    _abyssalTargetPriority = 9.2;

                if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower()) && Combat.PotentialCombatTargets.Count(i => i.TypeName.ToLower().Contains("lance Tessella".ToLower())) > 6) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                    _abyssalTargetPriority = 9.13;

                if (IsNeutingEntity)
                    _abyssalTargetPriority = 9.12;

                if (IsNPCFrigate && TypeName.ToLower().Contains("Starving Damavik".ToLower()))
                    _abyssalTargetPriority = 9.06;

                if (TypeName.ToLower().Contains("Harrowing Vedmak".ToLower()))
                    _abyssalTargetPriority = 9.05;

                if (TypeName.ToLower().Contains("Starving Vedmak".ToLower()))
                    _abyssalTargetPriority = 9.04;

                if (TypeName.ToLower().Contains("tangling kikimora".ToLower()))
                    _abyssalTargetPriority = 9.03;

                if (TypeName.ToLower().Contains("Tangling Damavik".ToLower())) //webs
                    _abyssalTargetPriority = 9.02;

                if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                {
                    if (TypeName.ToLower().Contains("Harrowing Vedmak".ToLower()))
                        _abyssalTargetPriority = 9.01;

                    if (TypeName.ToLower().Contains("Starving Vedmak".ToLower()))
                        _abyssalTargetPriority = 9;
                }

                if (TypeName.ToLower().Contains("Harrowing Vedmak".ToLower()) && Combat.PotentialCombatTargets.Count(i => i.TypeName.ToLower().Contains("Vedmak".ToLower())) >= 3)
                    _abyssalTargetPriority = 9.01;

                if (TypeName.ToLower().Contains("Starving Vedmak".ToLower()) && Combat.PotentialCombatTargets.Count(i => i.TypeName.ToLower().Contains("Vedmak".ToLower())) >= 3)
                    _abyssalTargetPriority = 9;

                if (TypeName.ToLower().Contains("Vedmak".ToLower()) && _abyssalTargetPriority > 10)
                    _abyssalTargetPriority = 9.05;


                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                {
                    if (IsWarpScramblingEntity)
                    {
                        _abyssalTargetPriority = 8.7;
                        if (.5 > ShieldPct)
                            _abyssalTargetPriority = 8.69;
                        if (.25 > ShieldPct)
                            _abyssalTargetPriority = 8.68;
                        if (.01 > ShieldPct)
                            _abyssalTargetPriority = 8.67;
                        if (.5 > ArmorPct)
                            _abyssalTargetPriority = 8.66;
                        if (.25 > ArmorPct)
                            _abyssalTargetPriority = 8.65;
                        if (.01 > ArmorPct)
                            _abyssalTargetPriority = 8.54;
                    }

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe)
                    {
                        _abyssalTargetPriority = 8.6;
                        if (.5 > ShieldPct)
                            _abyssalTargetPriority = 8.59;
                        if (.25 > ShieldPct)
                            _abyssalTargetPriority = 8.58;
                        if (.01 > ShieldPct)
                            _abyssalTargetPriority = 8.57;
                        if (.5 > ArmorPct)
                            _abyssalTargetPriority = 8.56;
                        if (.25 > ArmorPct)
                            _abyssalTargetPriority = 8.55;
                        if (.01 > ArmorPct)
                            _abyssalTargetPriority = 8.54;
                    }

                    if (IsWarpScramblingEntity && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (IsWarpScramblingEntity && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.4;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.3;

                    if (IsWarpScramblingEntity && IsNPCFrigate)
                        _abyssalTargetPriority = 8.2;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCFrigate)
                        _abyssalTargetPriority = 8.1;
                }

                if (IsTargetingDrones) // focus targets which are focusing the drones
                {
                    _abyssalTargetPriority = 7.99;
                    if (TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                        _abyssalTargetPriority = 7.9;
                    if (TypeName.ToLower().Contains("caster Tessella".ToLower())) //40 OMNI damage ROF: 4 seconds - in 10 sec 160 damage
                        _abyssalTargetPriority = 7.8;
                    if (TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                        _abyssalTargetPriority = 7.7;
                    if (TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                        _abyssalTargetPriority = 7.6;
                    if (TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                        _abyssalTargetPriority = 7.5;
                    if (TypeName.ToLower().Contains("ghosting kikimora".ToLower()))
                        _abyssalTargetPriority = 7.49;
                    if (TypeName.ToLower().Contains("striking kikimora".ToLower()))
                        _abyssalTargetPriority = 7.45;

                    if (TypeName.ToLower().Contains("Ghosting Damavik".ToLower()))
                        _abyssalTargetPriority = 7.4;
                    if (TypeName.ToLower().Contains("Anchoring Damavik".ToLower()))
                        _abyssalTargetPriority = 7.35;
                    if (TypeName.ToLower().Contains("Striking Damavik".ToLower()))
                        _abyssalTargetPriority = 7.3;

                    if (IsNeutingEntity)
                        _abyssalTargetPriority = 7.25;

                    if (TypeName.ToLower().Contains("Starving Damavik".ToLower()))
                        _abyssalTargetPriority = 7.24;

                    if (TypeName.ToLower().Contains("Harrowing Vedmak".ToLower()))
                        _abyssalTargetPriority = 7.23;

                    if (TypeName.ToLower().Contains("Starving Vedmak".ToLower()))
                        _abyssalTargetPriority = 7.22;

                    if (TypeName.ToLower().Contains("tangling kikimora".ToLower()))
                        _abyssalTargetPriority = 7.21;

                    if (TypeName.ToLower().Contains("Tangling Damavik".ToLower())) //webs
                        _abyssalTargetPriority = 7.20;

                    if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                    {
                        if (TypeName.ToLower().Contains("Harrowing Vedmak".ToLower()))
                            _abyssalTargetPriority = 7.19;

                        if (TypeName.ToLower().Contains("Starving Vedmak".ToLower()))
                            _abyssalTargetPriority = 7.18;
                    }

                    if (TypeName.ToLower().Contains("Harrowing Vedmak".ToLower()) && Combat.PotentialCombatTargets.Count(i => i.TypeName.ToLower().Contains("Vedmak".ToLower())) >= 3)
                        _abyssalTargetPriority = 7.19;

                    if (TypeName.ToLower().Contains("Starving Vedmak".ToLower()) && Combat.PotentialCombatTargets.Count(i => i.TypeName.ToLower().Contains("Vedmak".ToLower())) >= 3)
                        _abyssalTargetPriority = 7.18;
                }

                if (TypeName.ToLower().Contains("Starving Vedmak".ToLower()) && Combat.PotentialCombatTargets.Count(i => i.TypeName.ToLower().Contains("Vedmak".ToLower())) >= 3)
                    _abyssalTargetPriority = 6.5;


                // override for frigates in speed clouds
                //if (IsNPCFrigate && IsInSpeedCloud)
                //    _abyssalTargetPriority = 666;
                //
                //if (IsNPCFrigate && IsInSpeedCloud && IsTargetingDrones)
                //    _abyssalTargetPriority = 665;

                AdjustAbyssalTargetPriority_Common();

                return _abyssalTargetPriority.Value;
            }
        }

        private double AbyssalTargetPriority_ConcordSpawn
        {
            get
            {
                //https://caldarijoans.streamlit.app/Room_Overviews_and_Tips
                //Seriously, keep up your speed and avoid Blue Clouds. Outranging Marshalls and Thunderchilds is a common tip to reduce incoming damage. Both reach out to approximately 65km.
                //Killing support enemies is an efficient way to reduce incoming damage.
                //If you can tank the EDENCOM chain lighting then you can also use it to damage other enemies in the room.
                //Battleships here start with damaged tanks. Kill them as soon as realistically possible to reduce the amount of HP you need to clear.

                _abyssalTargetPriority = 50;

                if (IsNPCFrigate)
                    _abyssalTargetPriority = 40;

                if (TypeName.ToLower().Contains("Stormbringer".ToLower())) //cruiser
                    _abyssalTargetPriority = 29;

                if (TypeName.ToLower().Contains("Skybreaker".ToLower())) //frigate
                    _abyssalTargetPriority = 28;

                if (IsAbyssalBioAdaptiveCache)
                    _abyssalTargetPriority = 20.2;
                if (IsAbyssalDeadspaceTriglavianExtractionNode)
                    _abyssalTargetPriority = 20.1;

                if (IsRemoteRepairEntity)
                    _abyssalTargetPriority = 9.5;

                if (TypeName.ToLower().Contains("Marker Pacifier".ToLower()))
                    _abyssalTargetPriority = 9.10;

                if (TypeName.ToLower().Contains("Attacker Pacifier".ToLower()))
                    _abyssalTargetPriority = 9.2;

                if (TypeName.ToLower().Contains("Drainer Pacifier".ToLower()))
                    _abyssalTargetPriority = 9.15;

                if (TypeName.ToLower().Contains("Marker Enforcer".ToLower()))
                    _abyssalTargetPriority = 9.11;

                if (TypeName.ToLower().Contains("Assault Enforcer".ToLower()))
                    _abyssalTargetPriority = 9.10;

                if (TypeName.ToLower().Contains("Drainer Enforcer".ToLower()))
                    _abyssalTargetPriority = 9.05;

                if (TypeName.ToLower().Contains("Marshal".ToLower()))
                    _abyssalTargetPriority = 8.97;

                if (TypeName.ToLower().Contains("Marshal".ToLower()) && IsTargetingDrones)
                    _abyssalTargetPriority = 8.96;

                if (Drones.AllDronesInSpace.Any() || ESCache.Instance.ActiveShip.HasDroneBay)
                {
                    if (TypeName.ToLower().Contains("Stormbringer".ToLower())) //cruiser
                        _abyssalTargetPriority = 8.96;

                    if (TypeName.ToLower().Contains("Skybreaker".ToLower())) //frigate
                        _abyssalTargetPriority = 8.91;
                }

                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                {
                    if (IsWarpScramblingEntity)
                    {
                        _abyssalTargetPriority = 8.7;
                        if (.5 > ShieldPct)
                            _abyssalTargetPriority = 8.69;
                        if (.25 > ShieldPct)
                            _abyssalTargetPriority = 8.68;
                        if (.01 > ShieldPct)
                            _abyssalTargetPriority = 8.67;
                        if (.5 > ArmorPct)
                            _abyssalTargetPriority = 8.66;
                        if (.25 > ArmorPct)
                            _abyssalTargetPriority = 8.65;
                        if (.01 > ArmorPct)
                            _abyssalTargetPriority = 8.64;
                    }

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe)
                    {
                        _abyssalTargetPriority = 8.6;
                        if (.5 > ShieldPct)
                            _abyssalTargetPriority = 8.59;
                        if (.25 > ShieldPct)
                            _abyssalTargetPriority = 8.58;
                        if (.01 > ShieldPct)
                            _abyssalTargetPriority = 8.57;
                        if (.5 > ArmorPct)
                            _abyssalTargetPriority = 8.56;
                        if (.25 > ArmorPct)
                            _abyssalTargetPriority = 8.55;
                        if (.01 > ArmorPct)
                            _abyssalTargetPriority = 8.54;
                    }

                    if (IsWarpScramblingEntity && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (IsWarpScramblingEntity && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.4;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.3;

                    if (IsWarpScramblingEntity && IsNPCFrigate)
                        _abyssalTargetPriority = 8.2;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCFrigate)
                        _abyssalTargetPriority = 8.1;
                }

                if (IsTargetingDrones) // focus targets which are focusing the drones
                {
                    _abyssalTargetPriority = 7.99;
                    if (TypeName.ToLower().Contains("Attacker Pacifier ".ToLower()))
                        _abyssalTargetPriority = 7.981;

                    if (TypeName.ToLower().Contains("Assault Enforcer".ToLower()))
                        _abyssalTargetPriority = 7.98;

                    if (TypeName.ToLower().Contains("Drainer Enforcer".ToLower()))
                        _abyssalTargetPriority = 7.97;

                    if (TypeName.ToLower().Contains("Stormbringer".ToLower()))
                        _abyssalTargetPriority = 7.96;

                    if (TypeName.ToLower().Contains("Skybreaker".ToLower()))
                        _abyssalTargetPriority = 7.95;
                }

                if (TypeName.ToLower().Contains("Thunderchild".ToLower()))
                    _abyssalTargetPriority = 2.01;

                if (TypeName.ToLower().Contains("Thunderchild".ToLower()) && IsTargetingDrones)
                    _abyssalTargetPriority = 2;

                if (TypeName.ToLower().Contains("Marshal".ToLower()) && Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Marshal")) >= 2)
                    _abyssalTargetPriority = 1.1;

                if (TypeName.ToLower().Contains("Skybreaker".ToLower()))
                    _abyssalTargetPriority = 1.05;

                if (TypeName.ToLower().Contains("Arrester Pacifier".ToLower()))
                    _abyssalTargetPriority = 1;


                AdjustAbyssalTargetPriority_Common();

                return _abyssalTargetPriority.Value;
            }
        }

        private double AbyssalTargetPriority_DrekavacBattleCruiserSpawn
        {
            get
            {
                //https://caldarijoans.streamlit.app/Room_Overviews_and_Tips
                //As you begin to clear Drekavacs from the spawn the room will begin to go faster due to significantly less RR.
                //Identify high threat EWAR Damaviks at the start of the room in order to clear one thin target off the list before starting the proper engagement with the Drekavacs. If Drekavacs begin landing repairs on the Damavik then it's worth considering swapping off.

                _abyssalTargetPriority = 50;

                if (IsNPCFrigate)
                    _abyssalTargetPriority = 40;
                if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                    _abyssalTargetPriority = 39.6;
                if (IsNPCFrigate && TypeName.ToLower().Contains("caster Tessella".ToLower())) //40 OMNI damage ROF: 4 seconds - in 10 sec 160 damage
                    _abyssalTargetPriority = 39.5;
                if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                    _abyssalTargetPriority = 39.4;
                if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                    _abyssalTargetPriority = 39.3;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                    _abyssalTargetPriority = 39.2;
                if (IsNPCCruiser)
                    _abyssalTargetPriority = 30;
                if (IsNPCBattlecruiser)
                    _abyssalTargetPriority = 31;
                if (IsTargetPaintingEntity)
                    _abyssalTargetPriority = 25;
                if (IsWebbingEntity)
                    _abyssalTargetPriority = 24;

                if (IsAbyssalBioAdaptiveCache)
                    _abyssalTargetPriority = 20.2;
                if (IsAbyssalDeadspaceTriglavianExtractionNode)
                    _abyssalTargetPriority = 20.1;

                if (IsNPCBattlecruiser && TypeName.ToLower().Contains("Striking Drekavac"))
                    _abyssalTargetPriority = 19;
                if (IsNPCBattlecruiser && TypeName.ToLower().Contains("Shining Drekavac"))
                    _abyssalTargetPriority = 17;
                if (IsSensorDampeningEntity)
                    _abyssalTargetPriority = 15;
                if (IsWebbingEntity && (IsNPCBattlecruiser || IsNPCCruiser || IsNPCFrigate) && Distance < 19500)
                    _abyssalTargetPriority = 13;
                if (TypeName.ToLower().Contains("nullcharge") || TypeName.ToLower().Contains("ephialtes"))
                    _abyssalTargetPriority = 12;

                if (TypeName.ToLower().Contains("Striking Damavik".ToLower()))
                    _abyssalTargetPriority = 11.5;
                if (TypeName.ToLower().Contains("Ghosting Damavik".ToLower()))
                    _abyssalTargetPriority = 11.4;
                if (TypeName.ToLower().Contains("Anchoring Damavik".ToLower()))
                    _abyssalTargetPriority = 11.3;
                //if (TypeName.ToLower().Contains("Anchoring Damavik".ToLower()))
                //    _abyssalTargetPriority = 11.2;


                if (TypeName.ToLower().Contains("kikimora".ToLower()))
                    _abyssalTargetPriority = 11;
                if (TypeName.ToLower().Contains("ghosting kikimora".ToLower()))
                    _abyssalTargetPriority = 10.5;
                if (TypeName.ToLower().Contains("striking kikimora".ToLower()))
                    _abyssalTargetPriority = 10;
                //if (IsRemoteRepairEntity && Combat.PotentialCombatTargets.All(i => !i.IsNPCBattlecruiser))
                //    _abyssalTargetPriority = 3.5;
                if (TypeName.ToLower().Contains("renewing".ToLower()) || TypeName.ToLower().Contains("priest".ToLower()) || TypeName.ToLower().Contains("plateforger".ToLower()) || TypeName.ToLower().Contains("preserver".ToLower()) || TypeName.ToLower().Contains("fieldweaver".ToLower()) || TypeName.ToLower().Contains("burst".ToLower()))
                    _abyssalTargetPriority = 8.3;
                if (TypeName.ToLower().Contains("rodiva".ToLower()))
                    _abyssalTargetPriority = 8.2;

                if (TypeName.ToLower().Contains("Starving Damavik".ToLower()))
                    _abyssalTargetPriority = 9.6;
                if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWebbingMe) //webs
                    _abyssalTargetPriority = 8.89;
                if (TypeName.ToLower().Contains("tangling kikimora".ToLower()))
                    _abyssalTargetPriority = 8.85;
                //if (TypeName.ToLower().Contains("tangling kikimora".ToLower()))
                //    _abyssalTargetPriority = 8.10;

                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                {
                    if (IsWarpScramblingEntity)
                        _abyssalTargetPriority = 8.7;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe)
                        _abyssalTargetPriority = 8.6;

                    if (IsWarpScramblingEntity && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (IsWarpScramblingEntity && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.4;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.3;

                    if (IsWarpScramblingEntity && IsNPCFrigate)
                        _abyssalTargetPriority = 8.2;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCFrigate)
                        _abyssalTargetPriority = 8.1;
                }

                if (IsTargetingDrones) // focus targets which are focusing the drones
                {
                    _abyssalTargetPriority = 7.99;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                        _abyssalTargetPriority = 7.98;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("caster Tessella".ToLower())) //40 OMNI damage ROF: 4 seconds - in 10 sec 160 damage
                        _abyssalTargetPriority = 7.97;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                        _abyssalTargetPriority = 7.96;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                        _abyssalTargetPriority = 7.95;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                        _abyssalTargetPriority = 7.945;
                    //if (IsNPCBattlecruiser && TypeName.ToLower().Contains("Striking Drekavac"))
                    //    _abyssalTargetPriority = 7.9;
                    //if (IsNPCBattlecruiser && TypeName.ToLower().Contains("Shining Drekavac"))
                    //    _abyssalTargetPriority = 7.8;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Tangling Damavik".ToLower()))
                        _abyssalTargetPriority = 7.6;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Starving Damavik".ToLower()))
                        _abyssalTargetPriority = 7.5;

                    if (TypeName.ToLower().Contains("Ghosting Damavik".ToLower()))
                        _abyssalTargetPriority = 7.4;
                    if (TypeName.ToLower().Contains("Anchoring Damavik".ToLower()))
                        _abyssalTargetPriority = 7.3;
                    //if (TypeName.ToLower().Contains("Anchoring Damavik".ToLower()))
                    //    _abyssalTargetPriority = 11.2;

                    if (TypeName.ToLower().Contains("kikimora".ToLower()))
                        _abyssalTargetPriority = 7.2;
                    if (TypeName.ToLower().Contains("ghosting kikimora".ToLower()))
                        _abyssalTargetPriority = 7.15;
                    if (TypeName.ToLower().Contains("striking kikimora".ToLower()))
                        _abyssalTargetPriority = 7.14;
                    //if (IsRemoteRepairEntity && Combat.PotentialCombatTargets.All(i => !i.IsNPCBattlecruiser))
                    //    _abyssalTargetPriority = 3.5;
                    if (TypeName.ToLower().Contains("renewing".ToLower()) || TypeName.ToLower().Contains("priest".ToLower()) || TypeName.ToLower().Contains("plateforger".ToLower()) || TypeName.ToLower().Contains("preserver".ToLower()) || TypeName.ToLower().Contains("fieldweaver".ToLower()) || TypeName.ToLower().Contains("burst".ToLower()))
                        _abyssalTargetPriority = 7.13;
                    if (TypeName.ToLower().Contains("rodiva".ToLower()))
                        _abyssalTargetPriority = 7.12;

                    if (IsWebbingEntity)
                        _abyssalTargetPriority = 7.1;

                    if (TypeName.ToLower().Contains("tangling kikimora".ToLower()))
                        _abyssalTargetPriority = 7.05;
                }

                AdjustAbyssalTargetPriority_Common();

                return _abyssalTargetPriority.Value;
            }
        }
        private double AbyssalTargetPriority_LeshakBSSpawn
        {
            get
            {
                //https://caldarijoans.streamlit.app/Room_Overviews_and_Tips
                //As you begin to clear Drekavacs from the spawn the room will begin to go faster due to significantly less RR.
                //Identify high threat EWAR Damaviks at the start of the room in order to clear one thin target off the list before starting the proper engagement with the Drekavacs. If Drekavacs begin landing repairs on the Damavik then it's worth considering swapping off.

                _abyssalTargetPriority = 50;

                if (IsNPCFrigate)
                    _abyssalTargetPriority = 40;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Ghosting Damavik".ToLower()))
                    _abyssalTargetPriority = 39.9;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Anchoring Damavik".ToLower()))
                    _abyssalTargetPriority = 39.8;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Striking Damavik".ToLower()))
                    _abyssalTargetPriority = 39.7;
                if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                    _abyssalTargetPriority = 39.6;
                if (IsNPCFrigate && TypeName.ToLower().Contains("caster Tessella".ToLower())) //40 OMNI damage ROF: 4 seconds - in 10 sec 160 damage
                    _abyssalTargetPriority = 39.5;
                if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                    _abyssalTargetPriority = 39.4;
                if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                    _abyssalTargetPriority = 39.3;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                    _abyssalTargetPriority = 39.2;
                if (IsNPCCruiser)
                    _abyssalTargetPriority = 30;
                if (IsNPCBattlecruiser)
                    _abyssalTargetPriority = 31;
                if (IsTargetPaintingEntity)
                    _abyssalTargetPriority = 25;

                if (TypeName.ToLower().Contains("warding leshak".ToLower()))
                    _abyssalTargetPriority = 24.6;
                if (TypeName.ToLower().Contains("striking leshak".ToLower()))
                    _abyssalTargetPriority = 24.4;
                if (TypeName.ToLower().Contains("renewing leshak".ToLower()))
                    _abyssalTargetPriority = 24.3;
                if (TypeName.ToLower().Contains("starving leshak".ToLower()))
                    _abyssalTargetPriority = 24.2;

                if (IsWebbingEntity)
                    _abyssalTargetPriority = 24;

                if (TypeName.ToLower().Contains("tangling leshak".ToLower()))
                    _abyssalTargetPriority = 23.9;

                if (IsAbyssalBioAdaptiveCache)
                    _abyssalTargetPriority = 20.2;
                if (IsAbyssalDeadspaceTriglavianExtractionNode)
                    _abyssalTargetPriority = 20.1;

                if (IsSensorDampeningEntity)
                    _abyssalTargetPriority = 15;

                if (TypeName.ToLower().Contains("blinding leshak".ToLower()))
                    _abyssalTargetPriority = 13.9;

                if (IsWebbingEntity && (IsNPCBattlecruiser || IsNPCCruiser || IsNPCFrigate) && Distance < 19500)
                    _abyssalTargetPriority = 13;


                if (TypeName.ToLower().Contains("Ghosting Damavik".ToLower()))
                    _abyssalTargetPriority = 11.4;
                if (TypeName.ToLower().Contains("Anchoring Damavik".ToLower()))
                    _abyssalTargetPriority = 11.3;
                //if (TypeName.ToLower().Contains("Anchoring Damavik".ToLower()))
                //    _abyssalTargetPriority = 11.2;

                if (TypeName.ToLower().Contains("Starving Damavik".ToLower()))
                    _abyssalTargetPriority = 9.6;
                if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWebbingMe) //webs
                    _abyssalTargetPriority = 8.89;
                if (TypeName.ToLower().Contains("tangling kikimora".ToLower()))
                    _abyssalTargetPriority = 8.85;
                //if (TypeName.ToLower().Contains("tangling kikimora".ToLower()))
                //    _abyssalTargetPriority = 8.10;



                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                {
                    if (IsWarpScramblingEntity)
                        _abyssalTargetPriority = 8.7;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe)
                        _abyssalTargetPriority = 8.6;

                    if (IsWarpScramblingEntity && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (IsWarpScramblingEntity && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.4;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.3;

                    if (IsWarpScramblingEntity && IsNPCFrigate)
                        _abyssalTargetPriority = 8.2;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCFrigate)
                        _abyssalTargetPriority = 8.1;
                }

                if (IsTargetingDrones) // focus targets which are focusing the drones
                {
                    _abyssalTargetPriority = 7.99;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                        _abyssalTargetPriority = 7.98;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("caster Tessella".ToLower())) //40 OMNI damage ROF: 4 seconds - in 10 sec 160 damage
                        _abyssalTargetPriority = 7.97;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                        _abyssalTargetPriority = 7.96;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                        _abyssalTargetPriority = 7.95;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                        _abyssalTargetPriority = 7.945;
                    if (IsNPCBattlecruiser && TypeName.ToLower().Contains("Striking Drekavac"))
                        _abyssalTargetPriority = 7.9;
                    if (IsNPCBattlecruiser && TypeName.ToLower().Contains("Shining Drekavac"))
                        _abyssalTargetPriority = 7.8;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Tangling Damavik".ToLower()))
                        _abyssalTargetPriority = 7.6;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Starving Damavik".ToLower()))
                        _abyssalTargetPriority = 7.5;

                    if (TypeName.ToLower().Contains("Ghosting Damavik".ToLower()))
                        _abyssalTargetPriority = 7.4;
                    if (TypeName.ToLower().Contains("Anchoring Damavik".ToLower()))
                        _abyssalTargetPriority = 7.3;
                    //if (TypeName.ToLower().Contains("Anchoring Damavik".ToLower()))
                    //    _abyssalTargetPriority = 11.2;

                    if (TypeName.ToLower().Contains("kikimora".ToLower()))
                        _abyssalTargetPriority = 7.2;
                    if (TypeName.ToLower().Contains("ghosting kikimora".ToLower()))
                        _abyssalTargetPriority = 7.15;
                    if (TypeName.ToLower().Contains("striking kikimora".ToLower()))
                        _abyssalTargetPriority = 7.14;
                    //if (IsRemoteRepairEntity && Combat.PotentialCombatTargets.All(i => !i.IsNPCBattlecruiser))
                    //    _abyssalTargetPriority = 3.5;
                    if (TypeName.ToLower().Contains("renewing".ToLower()) || TypeName.ToLower().Contains("priest".ToLower()) || TypeName.ToLower().Contains("plateforger".ToLower()) || TypeName.ToLower().Contains("preserver".ToLower()) || TypeName.ToLower().Contains("fieldweaver".ToLower()) || TypeName.ToLower().Contains("burst".ToLower()))
                        _abyssalTargetPriority = 7.13;
                    if (TypeName.ToLower().Contains("rodiva".ToLower()))
                        _abyssalTargetPriority = 7.12;

                    if (IsWebbingEntity)
                        _abyssalTargetPriority = 7.1;

                    if (TypeName.ToLower().Contains("tangling kikimora".ToLower()))
                        _abyssalTargetPriority = 7.05;
                }

                AdjustAbyssalTargetPriority_Common();

                return _abyssalTargetPriority.Value;
            }
        }
        private double AbyssalTargetPriority_LucidCruiserSpawn
        {
            get
            {
                _abyssalTargetPriority = 50;

                if (IsNPCFrigate)
                    _abyssalTargetPriority = 40;
                if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                    _abyssalTargetPriority = 39.9;

                if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                    _abyssalTargetPriority = 39.7;
                if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                    _abyssalTargetPriority = 39.6;

                //Needle Lower DPS
                if (IsNPCFrigate && TypeName.ToLower().Contains("Emberneedle Tessella".ToLower())) //20 Thermal * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.5;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Strikeneedle Tessella".ToLower())) //20 Kinetic * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.4;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Blastneedle Tessella".ToLower())) //20 Explosive * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.3;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparkneedle Tessella".ToLower())) //20 EM * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.2;

                if (TypeName.ToLower().Contains("Lucid Escort".ToLower()))
                    _abyssalTargetPriority = 38.9;
                if (TypeName.ToLower().Contains("Lucid Preserver".ToLower()))
                    _abyssalTargetPriority = 38.8;
                if (TypeName.ToLower().Contains("Lucid Warden".ToLower()))
                    _abyssalTargetPriority = 38.7;

                //Lance High DPS
                if (IsNPCFrigate && TypeName.ToLower().Contains("Blastlance Tessella".ToLower())) //40 Explosive * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.5;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Emberlance Tessella".ToLower())) //40 Thermal * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.4;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 EM * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.3;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Strikelance Tessella".ToLower())) //40 Kinetic * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.2;

                if (IsNPCCruiser)
                    _abyssalTargetPriority = 30;
                if (IsNPCBattlecruiser)
                    _abyssalTargetPriority = 31;
                if (IsNPCBattleship)
                    _abyssalTargetPriority = 32;
                if (IsTargetPaintingEntity)
                    _abyssalTargetPriority = 25;
                if (IsWebbingEntity)
                    _abyssalTargetPriority = 24;
                if (GroupId == 1997 || NPCHasVortonProjectorGuns) // https://everef.net/groups/1997 abyss drones OR vorton projectors
                    _abyssalTargetPriority = 22;

                if (IsSensorDampeningEntity)
                    _abyssalTargetPriority = 15;

                if (TypeName.ToLower().Contains("Lucid Watchmen".ToLower())) //damage
                    _abyssalTargetPriority = 12;
                if (TypeName.ToLower().Contains("Lucid Upholder".ToLower())) //webs
                    _abyssalTargetPriority = 11;
                if (IsNeutingEntity) //neuts
                    _abyssalTargetPriority = 10.22;
                if (TypeName.ToLower().Contains("Drifter Nullcharge Cruiser".ToLower())) //neuts
                    _abyssalTargetPriority = 10.21;
                if (TypeName.ToLower().Contains("Lucid Sentinel".ToLower())) //neuts
                    _abyssalTargetPriority = 10;

                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                {
                    if (IsWarpScramblingEntity)
                    {
                        _abyssalTargetPriority = 8.7;
                        if (.5 > ShieldPct)
                            _abyssalTargetPriority = 8.69;
                        if (.25 > ShieldPct)
                            _abyssalTargetPriority = 8.68;
                        if (.01 > ShieldPct)
                            _abyssalTargetPriority = 8.67;
                        if (.5 > ArmorPct)
                            _abyssalTargetPriority = 8.66;
                        if (.25 > ArmorPct)
                            _abyssalTargetPriority = 8.65;
                        if (.01 > ArmorPct)
                            _abyssalTargetPriority = 8.64;
                    }

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe)
                    {
                        _abyssalTargetPriority = 8.6;
                        if (.5 > ShieldPct)
                            _abyssalTargetPriority = 8.59;
                        if (.25 > ShieldPct)
                            _abyssalTargetPriority = 8.58;
                        if (.01 > ShieldPct)
                            _abyssalTargetPriority = 8.57;
                        if (.5 > ArmorPct)
                            _abyssalTargetPriority = 8.56;
                        if (.25 > ArmorPct)
                            _abyssalTargetPriority = 8.55;
                        if (.01 > ArmorPct)
                            _abyssalTargetPriority = 8.54;
                    }

                    if (IsWarpScramblingEntity && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (IsWarpScramblingEntity && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.4;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.3;

                    if (IsWarpScramblingEntity && IsNPCFrigate)
                        _abyssalTargetPriority = 8.2;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCFrigate)
                        _abyssalTargetPriority = 8.1;
                }

                if (IsTargetingDrones) // focus targets which are focusing the drones
                {
                    _abyssalTargetPriority = 7.99;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                        _abyssalTargetPriority = 7.9;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("caster Tessella".ToLower())) //40 OMNI damage ROF: 4 seconds - in 10 sec 160 damage
                        _abyssalTargetPriority = 7.8;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                        _abyssalTargetPriority = 7.7;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                        _abyssalTargetPriority = 7.6;

                    if (TypeName.ToLower().Contains("Lucid Escort".ToLower())) //damage
                        _abyssalTargetPriority = 7.499;
                    if (TypeName.ToLower().Contains("Lucid Aegis".ToLower())) //damage
                        _abyssalTargetPriority = 7.498;
                    if (TypeName.ToLower().Contains("Lucid Preserver".ToLower())) //damage
                        _abyssalTargetPriority = 7.495;
                    if (TypeName.ToLower().Contains("Lucid Warden".ToLower())) //damage
                        _abyssalTargetPriority = 7.48;

                    if (TypeName.ToLower().Contains("Lucid Watchmen".ToLower())) //damage
                        _abyssalTargetPriority = 7.4;
                    if (TypeName.ToLower().Contains("Lucid Upholder".ToLower())) //webs
                        _abyssalTargetPriority = 7.3;

                    if (IsNeutingEntity) //neuts
                        _abyssalTargetPriority = 7.22;
                    if (TypeName.ToLower().Contains("Drifter Nullcharge Cruiser".ToLower())) //neuts
                        _abyssalTargetPriority = 7.21;
                    if (TypeName.ToLower().Contains("Lucid Sentinel".ToLower())) //neuts
                        _abyssalTargetPriority = 7.2;
                    if (TypeName.ToLower().Contains("Lucid Firewatcher".ToLower())) //damage
                        _abyssalTargetPriority = 7.1;
                }

                AdjustAbyssalTargetPriority_Common();

                return _abyssalTargetPriority.Value;
            }
        }
        private double AbyssalTargetPriority_LucidBSSpawn
        {
            get
            {
                _abyssalTargetPriority = 50;

                if (IsNPCFrigate)
                    _abyssalTargetPriority = 40;
                if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                    _abyssalTargetPriority = 39.9;
                if (IsNPCFrigate && TypeName.ToLower().Contains("caster Tessella".ToLower())) //40 OMNI damage ROF: 4 seconds - in 10 sec 160 damage
                    _abyssalTargetPriority = 39.8;
                if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                    _abyssalTargetPriority = 39.7;
                if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                    _abyssalTargetPriority = 39.6;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                    _abyssalTargetPriority = 39.5;
                if (TypeName.ToLower().Contains("Lucid Escort".ToLower()))
                    _abyssalTargetPriority = 38.499;
                if (TypeName.ToLower().Contains("Lucid Preserver".ToLower()))
                    _abyssalTargetPriority = 38.495;
                if (TypeName.ToLower().Contains("Lucid Warden".ToLower()))
                    _abyssalTargetPriority = 38.48;
                if (IsNPCCruiser)
                    _abyssalTargetPriority = 30;
                if (IsNPCBattlecruiser)
                    _abyssalTargetPriority = 31;
                if (IsNPCBattleship)
                    _abyssalTargetPriority = 32;
                if (TypeName.ToLower().Contains("Lucid Deepwatcher".ToLower()))
                    _abyssalTargetPriority = 38.48;
                if (IsTargetPaintingEntity)
                    _abyssalTargetPriority = 25;
                if (IsWebbingEntity)
                    _abyssalTargetPriority = 24;
                if (GroupId == 1997 || NPCHasVortonProjectorGuns) // https://everef.net/groups/1997 abyss drones OR vorton projectors
                    _abyssalTargetPriority = 22;

                if (IsSensorDampeningEntity)
                    _abyssalTargetPriority = 15;

                if (TypeName.ToLower().Contains("Lucid Watchmen".ToLower())) //damage
                    _abyssalTargetPriority = 12;
                if (TypeName.ToLower().Contains("Lucid Upholder".ToLower())) //webs
                    _abyssalTargetPriority = 11;
                if (IsNeutingEntity) //neuts
                    _abyssalTargetPriority = 10.22;
                if (TypeName.ToLower().Contains("Drifter Nullcharge Cruiser".ToLower())) //neuts
                    _abyssalTargetPriority = 10.21;
                if (TypeName.ToLower().Contains("Lucid Sentinel".ToLower())) //neuts
                    _abyssalTargetPriority = 10;

                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                {
                    if (IsWarpScramblingEntity)
                    {
                        _abyssalTargetPriority = 8.7;
                        if (.5 > ShieldPct)
                            _abyssalTargetPriority = 8.69;
                        if (.25 > ShieldPct)
                            _abyssalTargetPriority = 8.68;
                        if (.01 > ShieldPct)
                            _abyssalTargetPriority = 8.67;
                        if (.5 > ArmorPct)
                            _abyssalTargetPriority = 8.66;
                        if (.25 > ArmorPct)
                            _abyssalTargetPriority = 8.65;
                        if (.01 > ArmorPct)
                            _abyssalTargetPriority = 8.64;
                    }

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe)
                    {
                        _abyssalTargetPriority = 8.6;
                        if (.5 > ShieldPct)
                            _abyssalTargetPriority = 8.59;
                        if (.25 > ShieldPct)
                            _abyssalTargetPriority = 8.58;
                        if (.01 > ShieldPct)
                            _abyssalTargetPriority = 8.57;
                        if (.5 > ArmorPct)
                            _abyssalTargetPriority = 8.56;
                        if (.25 > ArmorPct)
                            _abyssalTargetPriority = 8.55;
                        if (.01 > ArmorPct)
                            _abyssalTargetPriority = 8.54;
                    }

                    if (IsWarpScramblingEntity && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (IsWarpScramblingEntity && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.4;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.3;

                    if (IsWarpScramblingEntity && IsNPCFrigate)
                        _abyssalTargetPriority = 8.2;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCFrigate)
                        _abyssalTargetPriority = 8.1;
                }

                if (IsTargetingDrones) // focus targets which are focusing the drones
                {
                    _abyssalTargetPriority = 7.99;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                        _abyssalTargetPriority = 7.9;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("caster Tessella".ToLower())) //40 OMNI damage ROF: 4 seconds - in 10 sec 160 damage
                        _abyssalTargetPriority = 7.8;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                        _abyssalTargetPriority = 7.7;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                        _abyssalTargetPriority = 7.6;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                        _abyssalTargetPriority = 7.5;

                    if (TypeName.ToLower().Contains("Lucid Escort".ToLower())) //damage
                        _abyssalTargetPriority = 7.499;
                    if (TypeName.ToLower().Contains("Lucid Aegis".ToLower())) //damage
                        _abyssalTargetPriority = 7.498;
                    if (TypeName.ToLower().Contains("Lucid Preserver".ToLower())) //damage
                        _abyssalTargetPriority = 7.495;
                    if (TypeName.ToLower().Contains("Lucid Warden".ToLower())) //damage
                        _abyssalTargetPriority = 7.48;

                    if (TypeName.ToLower().Contains("Lucid Watchmen".ToLower())) //damage
                        _abyssalTargetPriority = 7.4;
                    if (TypeName.ToLower().Contains("Lucid Upholder".ToLower())) //webs
                        _abyssalTargetPriority = 7.3;

                    if (IsNeutingEntity) //neuts
                        _abyssalTargetPriority = 7.22;
                    if (TypeName.ToLower().Contains("Drifter Nullcharge Cruiser".ToLower())) //neuts
                        _abyssalTargetPriority = 7.21;
                    if (TypeName.ToLower().Contains("Lucid Sentinel".ToLower())) //neuts
                        _abyssalTargetPriority = 7.2;
                    if (TypeName.ToLower().Contains("Lucid Firewatcher".ToLower())) //damage
                        _abyssalTargetPriority = 7.1;
                }


                AdjustAbyssalTargetPriority_Common();

                return _abyssalTargetPriority.Value;
            }
        }
        private double AbyssalTargetPriority_EphialtesCruiserSpawn
        {
            get
            {
                _abyssalTargetPriority = 50;

                if (IsNPCFrigate)
                    _abyssalTargetPriority = 40;
                if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                    _abyssalTargetPriority = 39.9;
                if (IsNPCFrigate && TypeName.ToLower().Contains("caster Tessella".ToLower())) //40 OMNI damage ROF: 4 seconds - in 10 sec 160 damage
                    _abyssalTargetPriority = 39.8;
                if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                    _abyssalTargetPriority = 39.7;
                if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                    _abyssalTargetPriority = 39.6;

                if (TypeName.ToLower().Contains("Lucid Escort"))
                    _abyssalTargetPriority = 38.499;
                if (TypeName.ToLower().Contains("Lucid Preserver"))
                    _abyssalTargetPriority = 38.495;
                if (IsNPCCruiser)
                    _abyssalTargetPriority = 30;
                if (IsNPCBattleship)
                    _abyssalTargetPriority = 32;
                if (TypeName.ToLower().Contains("ephialtes confuser")) //tracking disruptor
                    _abyssalTargetPriority = 12.9;
                if (TypeName.ToLower().Contains("ephialtes illuminator")) //target painter
                    _abyssalTargetPriority = 12.9;
                if (TypeName.ToLower().Contains("ephialtes spearfisher")) //warp scramble
                    _abyssalTargetPriority = 12.9;
                if (TypeName.ToLower().Contains("ephialtes lancer")) //DPS
                    _abyssalTargetPriority = 12.9;
                if (TypeName.ToLower().Contains("ephialtes obfuscator")) //damps
                    _abyssalTargetPriority = 12.9;

                if (TypeName.ToLower().Contains("nullcharge"))
                    _abyssalTargetPriority = 12;


                if (TypeName.ToLower().Contains("Lucid Watchmen".ToLower())) //damage
                    _abyssalTargetPriority = 11;

                if (IsRemoteRepairEntity && Combat.PotentialCombatTargets.All(i => !i.IsNPCBattlecruiser))
                    _abyssalTargetPriority = 9.9;
                if (TypeName.ToLower().Contains("renewing".ToLower()) || TypeName.ToLower().Contains("priest".ToLower()) || TypeName.ToLower().Contains("plateforger".ToLower()) || TypeName.ToLower().Contains("preserver".ToLower()) || TypeName.ToLower().Contains("fieldweaver".ToLower()) || TypeName.ToLower().Contains("burst".ToLower()))
                    _abyssalTargetPriority = 9.8;

                if (IsWebbingEntity)
                    _abyssalTargetPriority = 9.65;

                if (IsWebbingEntity && GroupId == 1997 && (IsNPCBattlecruiser || IsNPCCruiser || IsNPCFrigate) && Distance < 19500)
                    _abyssalTargetPriority = 9.55;
                if (TypeName.ToLower().Contains("Lucid Upholder".ToLower())) //webs
                    _abyssalTargetPriority = 9.5;
                if (TypeName.ToLower().Contains("ephialtes entangler")) //webifier
                    _abyssalTargetPriority = 9.4;
                if (TypeName.ToLower().Contains("Lucid Warden".ToLower()))
                    _abyssalTargetPriority = 9.3;

                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                {
                    if (IsWarpScramblingEntity)
                        _abyssalTargetPriority = 8.7;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe)
                        _abyssalTargetPriority = 8.6;

                    if (IsWarpScramblingEntity && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (IsWarpScramblingEntity && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.4;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.3;

                    if (IsWarpScramblingEntity && IsNPCFrigate)
                        _abyssalTargetPriority = 8.2;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCFrigate)
                        _abyssalTargetPriority = 8.1;
                }

                if (IsTargetingDrones) // focus targets which are focusing the drones
                {
                    _abyssalTargetPriority = 7.99;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                        _abyssalTargetPriority = 7.9;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("caster Tessella".ToLower())) //40 OMNI damage ROF: 4 seconds - in 10 sec 160 damage
                        _abyssalTargetPriority = 7.8;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                        _abyssalTargetPriority = 7.7;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                        _abyssalTargetPriority = 7.6;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                        _abyssalTargetPriority = 7.5;

                    if (TypeName.ToLower().Contains("ephialtes confuser")) //tracking disruptor
                        _abyssalTargetPriority = 7.48;
                    if (TypeName.ToLower().Contains("ephialtes illuminator")) //target painter
                        _abyssalTargetPriority = 7.47;
                    if (TypeName.ToLower().Contains("ephialtes spearfisher")) //warp scramble
                        _abyssalTargetPriority = 7.46;
                    if (TypeName.ToLower().Contains("ephialtes lancer")) //DPS
                        _abyssalTargetPriority = 7.45;
                    if (TypeName.ToLower().Contains("ephialtes obfuscator")) //damps
                        _abyssalTargetPriority = 7.44;
                    if (TypeName.ToLower().Contains("nullcharge"))
                        _abyssalTargetPriority = 7.43;
                    if (TypeName.ToLower().Contains("Lucid Watchmen".ToLower())) //damage
                        _abyssalTargetPriority = 7.42;
                    if (IsRemoteRepairEntity && Combat.PotentialCombatTargets.All(i => !i.IsNPCBattlecruiser))
                        _abyssalTargetPriority = 7.41;
                    if (TypeName.ToLower().Contains("renewing".ToLower()) || TypeName.ToLower().Contains("priest".ToLower()) || TypeName.ToLower().Contains("plateforger".ToLower()) || TypeName.ToLower().Contains("preserver".ToLower()) || TypeName.ToLower().Contains("fieldweaver".ToLower()) || TypeName.ToLower().Contains("burst".ToLower()))
                        _abyssalTargetPriority = 7.40;
                    if (IsNeutingEntity)
                        _abyssalTargetPriority = 7.39;
                    if (TypeName.ToLower().Contains("Lucid Sentinel".ToLower())) //neuts
                        _abyssalTargetPriority = 7.38;
                    if (TypeName.ToLower().Contains("ephialtes dissipator")) //neutralizer
                        _abyssalTargetPriority = 7.37;
                    if (IsWebbingEntity)
                        _abyssalTargetPriority = 7.36;
                    if (IsWebbingEntity && GroupId == 1997 && (IsNPCBattlecruiser || IsNPCCruiser || IsNPCFrigate) && Distance < 19500)
                        _abyssalTargetPriority = 7.34;
                    if (TypeName.ToLower().Contains("Lucid Upholder".ToLower())) //webs
                        _abyssalTargetPriority = 7.33;
                    if (TypeName.ToLower().Contains("ephialtes entangler")) //webifier
                        _abyssalTargetPriority = 7.32;

                }

                AdjustAbyssalTargetPriority_Common();

                return _abyssalTargetPriority.Value;
            }
        }
        private double AbyssalTargetPriority_KikimoraDestroyerSpawn
        {
            get
            {
                _abyssalTargetPriority = 50;

                if (IsNPCFrigate)
                    _abyssalTargetPriority = 40;

                if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                    _abyssalTargetPriority = 39.9;

                if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                    _abyssalTargetPriority = 39.7;
                if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                    _abyssalTargetPriority = 39.6;

                //Needle Lower DPS
                if (IsNPCFrigate && TypeName.ToLower().Contains("Emberneedle Tessella".ToLower())) //20 Thermal * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.5;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Strikeneedle Tessella".ToLower())) //20 Kinetic * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.4;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Blastneedle Tessella".ToLower())) //20 Explosive * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.3;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparkneedle Tessella".ToLower())) //20 EM * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.2;

                //Lance High DPS
                if (IsNPCFrigate && TypeName.ToLower().Contains("Blastlance Tessella".ToLower())) //40 Explosive * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.5;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Emberlance Tessella".ToLower())) //40 Thermal * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.4;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 EM * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.3;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Strikelance Tessella".ToLower())) //40 Kinetic * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.2;
                if (IsNPCCruiser)
                    _abyssalTargetPriority = 30;
                if (IsNPCBattlecruiser)
                    _abyssalTargetPriority = 31;
                if (IsWebbingEntity)
                    _abyssalTargetPriority = 24;
                if (IsNPCBattlecruiser && TypeName.ToLower().Contains("Striking Drekavac"))
                    _abyssalTargetPriority = 19;
                if (IsNPCBattlecruiser && TypeName.ToLower().Contains("Shining Drekavac"))
                    _abyssalTargetPriority = 17;
                if (IsSensorDampeningEntity)
                    _abyssalTargetPriority = 15;
                if (IsWebbingEntity && (IsNPCBattlecruiser || IsNPCCruiser || IsNPCFrigate) && Distance < 19500)
                    _abyssalTargetPriority = 13;
                if (TypeName.ToLower().Contains("nullcharge") || TypeName.ToLower().Contains("ephialtes"))
                    _abyssalTargetPriority = 12;

                if (TypeName.ToLower().Contains("Striking Damavik".ToLower()))
                    _abyssalTargetPriority = 11.5;
                if (TypeName.ToLower().Contains("Ghosting Damavik".ToLower()))
                    _abyssalTargetPriority = 11.4;
                if (TypeName.ToLower().Contains("Anchoring Damavik".ToLower()))
                    _abyssalTargetPriority = 11.3;
                //if (TypeName.ToLower().Contains("Anchoring Damavik".ToLower()))
                //    _abyssalTargetPriority = 11.2;


                if (TypeName.ToLower().Contains("kikimora".ToLower()))
                {
                    _abyssalTargetPriority = 10;
                    if (TypeName.ToLower().Contains("ghosting kikimora".ToLower()))
                        _abyssalTargetPriority = 9.5;
                    if (TypeName.ToLower().Contains("striking kikimora".ToLower()))
                        _abyssalTargetPriority = 9;
                    long RemoteRepRange = 15000;
                    if (Combat.PotentialCombatTargets.Any(i => i.Id != Id && TypeName.ToLower().Contains("kikimora".ToLower()) && RemoteRepRange > i._directEntity.DistanceTo(this)))
                    {
                        _abyssalTargetPriority = 10.0222;
                    }
                }

                //if (IsRemoteRepairEntity && Combat.PotentialCombatTargets.All(i => !i.IsNPCBattlecruiser))
                //    _abyssalTargetPriority = 3.5;
                if (TypeName.ToLower().Contains("renewing".ToLower()) || TypeName.ToLower().Contains("priest".ToLower()) || TypeName.ToLower().Contains("plateforger".ToLower()) || TypeName.ToLower().Contains("preserver".ToLower()) || TypeName.ToLower().Contains("fieldweaver".ToLower()) || TypeName.ToLower().Contains("burst".ToLower()))
                    _abyssalTargetPriority = 8.3;
                if (TypeName.ToLower().Contains("rodiva".ToLower()))
                    _abyssalTargetPriority = 8.2;
                if (TypeName.ToLower().Contains("Starving Damavik".ToLower()))
                    _abyssalTargetPriority = 9.6;
                if (TypeName.ToLower().Contains("tangling kikimora".ToLower()))
                    _abyssalTargetPriority = 8.85;

                if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower()) && Combat.PotentialCombatTargets.Count(i => i.TypeName.ToLower().Contains("lance Tessella".ToLower())) > 6) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                    _abyssalTargetPriority = 8.8;

                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                {
                    if (IsWarpScramblingEntity)
                    {
                        _abyssalTargetPriority = 8.7;
                        if (.5 > ShieldPct)
                            _abyssalTargetPriority = 8.69;
                        if (.25 > ShieldPct)
                            _abyssalTargetPriority = 8.68;
                        if (.01 > ShieldPct)
                            _abyssalTargetPriority = 8.67;
                        if (.5 > ArmorPct)
                            _abyssalTargetPriority = 8.66;
                        if (.25 > ArmorPct)
                            _abyssalTargetPriority = 8.65;
                        if (.01 > ArmorPct)
                            _abyssalTargetPriority = 8.64;
                    }

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe)
                    {
                        _abyssalTargetPriority = 8.6;
                        if (.5 > ShieldPct)
                            _abyssalTargetPriority = 8.59;
                        if (.25 > ShieldPct)
                            _abyssalTargetPriority = 8.58;
                        if (.01 > ShieldPct)
                            _abyssalTargetPriority = 8.57;
                        if (.5 > ArmorPct)
                            _abyssalTargetPriority = 8.56;
                        if (.25 > ArmorPct)
                            _abyssalTargetPriority = 8.55;
                        if (.01 > ArmorPct)
                            _abyssalTargetPriority = 8.54;
                    }

                    if (IsWarpScramblingEntity && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (IsWarpScramblingEntity && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.4;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.3;

                    if (IsWarpScramblingEntity && IsNPCFrigate)
                        _abyssalTargetPriority = 8.2;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCFrigate)
                        _abyssalTargetPriority = 8.1;
                }

                if (IsTargetingDrones) // focus targets which are focusing the drones
                {
                    _abyssalTargetPriority = 7.99;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                        _abyssalTargetPriority = 7.98;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("caster Tessella".ToLower())) //40 OMNI damage ROF: 4 seconds - in 10 sec 160 damage
                        _abyssalTargetPriority = 7.97;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                        _abyssalTargetPriority = 7.96;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                        _abyssalTargetPriority = 7.95;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                        _abyssalTargetPriority = 7.945;
                    if (IsNPCBattlecruiser && TypeName.ToLower().Contains("Striking Drekavac"))
                        _abyssalTargetPriority = 7.9;
                    if (IsNPCBattlecruiser && TypeName.ToLower().Contains("Shining Drekavac"))
                        _abyssalTargetPriority = 7.8;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Tangling Damavik".ToLower()))
                        _abyssalTargetPriority = 7.6;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Starving Damavik".ToLower()))
                        _abyssalTargetPriority = 7.5;

                    if (TypeName.ToLower().Contains("Ghosting Damavik".ToLower()))
                        _abyssalTargetPriority = 7.4;
                    if (TypeName.ToLower().Contains("Anchoring Damavik".ToLower()))
                        _abyssalTargetPriority = 7.3;
                    //if (TypeName.ToLower().Contains("Anchoring Damavik".ToLower()))
                    //    _abyssalTargetPriority = 11.2;


                    if (TypeName.ToLower().Contains("kikimora".ToLower()))
                        _abyssalTargetPriority = 7.2;
                    if (TypeName.ToLower().Contains("ghosting kikimora".ToLower()))
                        _abyssalTargetPriority = 7.15;
                    if (TypeName.ToLower().Contains("striking kikimora".ToLower()))
                        _abyssalTargetPriority = 7.14;
                    //if (IsRemoteRepairEntity && Combat.PotentialCombatTargets.All(i => !i.IsNPCBattlecruiser))
                    //    _abyssalTargetPriority = 3.5;
                    if (TypeName.ToLower().Contains("renewing".ToLower()) || TypeName.ToLower().Contains("priest".ToLower()) || TypeName.ToLower().Contains("plateforger".ToLower()) || TypeName.ToLower().Contains("preserver".ToLower()) || TypeName.ToLower().Contains("fieldweaver".ToLower()) || TypeName.ToLower().Contains("burst".ToLower()))
                        _abyssalTargetPriority = 7.13;
                    if (TypeName.ToLower().Contains("rodiva".ToLower()))
                        _abyssalTargetPriority = 7.12;

                    if (IsWebbingEntity)
                        _abyssalTargetPriority = 7.1;

                    if (TypeName.ToLower().Contains("tangling kikimora".ToLower()))
                        _abyssalTargetPriority = 7.05;
                }

                AdjustAbyssalTargetPriority_Common();

                return _abyssalTargetPriority.Value;
            }
        }
        private double AbyssalTargetPriority_KarybdisTyrannosSpawn
        {
            get
            {
                _abyssalTargetPriority = 50;

                if (IsNPCFrigate)
                    _abyssalTargetPriority = 60;
                if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                    _abyssalTargetPriority = 59.9;
                if (IsNPCFrigate && TypeName.ToLower().Contains("caster Tessella".ToLower())) //40 OMNI damage ROF: 4 seconds - in 10 sec 160 damage
                    _abyssalTargetPriority = 59.8;
                if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                    _abyssalTargetPriority = 59.7;
                if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                    _abyssalTargetPriority = 59.6;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                    _abyssalTargetPriority = 59.5;
                if (TypeName.ToLower().Contains("ephialtes lancer")) //DPS
                    _abyssalTargetPriority = 45;
                if (TypeName.ToLower().Contains("Lucid Escort"))
                    _abyssalTargetPriority = 38.499;
                if (TypeName.ToLower().Contains("Lucid Preserver"))
                    _abyssalTargetPriority = 38.495;
                if (TypeName.ToLower().Contains("Lucid Warden"))
                    _abyssalTargetPriority = 38.48;
                if (IsNPCCruiser)
                    _abyssalTargetPriority = 30;
                if (TypeName.ToLower().Contains("ephialtes confuser")) //tracking disruptor
                    _abyssalTargetPriority = 12.9;
                if (TypeName.ToLower().Contains("ephialtes illuminator")) //target painter
                    _abyssalTargetPriority = 12.9;
                if (TypeName.ToLower().Contains("ephialtes spearfisher")) //warp scramble
                    _abyssalTargetPriority = 12.9;
                if (TypeName.ToLower().Contains("ephialtes obfuscator")) //damps
                    _abyssalTargetPriority = 12.9;

                if (TypeName.ToLower().Contains("nullcharge"))
                    _abyssalTargetPriority = 12;

                if (TypeName.ToLower().Contains("Lucid Watchmen".ToLower())) //damage
                    _abyssalTargetPriority = 11;

                if (IsRemoteRepairEntity && Combat.PotentialCombatTargets.All(i => !i.IsNPCBattlecruiser))
                    _abyssalTargetPriority = 9.9;
                if (TypeName.ToLower().Contains("renewing".ToLower()) || TypeName.ToLower().Contains("priest".ToLower()) || TypeName.ToLower().Contains("plateforger".ToLower()) || TypeName.ToLower().Contains("preserver".ToLower()) || TypeName.ToLower().Contains("fieldweaver".ToLower()) || TypeName.ToLower().Contains("burst".ToLower()))
                    _abyssalTargetPriority = 9.8;

                if (IsWebbingEntity)
                    _abyssalTargetPriority = 9.7;

                if (TypeName.ToLower().Contains("Lucid Upholder".ToLower())) //webs
                    _abyssalTargetPriority = 9.5;
                if (TypeName.ToLower().Contains("ephialtes entangler")) //webifier
                    _abyssalTargetPriority = 9.4;


                if (IsNeutingEntity)
                    _abyssalTargetPriority = 9;
                if (TypeName.ToLower().Contains("Lucid Sentinel".ToLower())) //neuts
                    _abyssalTargetPriority = 8.95;
                if (TypeName.ToLower().Contains("ephialtes dissipator")) //neutralizer
                    _abyssalTargetPriority = 8.9;

                if (IsDrifterBSSpawn && !Combat.PotentialCombatTargets.Any(e => e.IsNPCCruiser)) // prio the drifter bs if there are 5 ships left on grid, so the turrets can also do some work while the drones kill the drifter bs
                    _abyssalTargetPriority = 8.8;

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                {
                    if (IsAbyssalKaren && 8 > Combat.PotentialCombatTargets.Count(e => e.IsNPCCruiser)) // prio the drifter bs if there are 5 ships left on grid, so the turrets can also do some work while the drones kill the drifter bs
                        _abyssalTargetPriority = 1;
                }


                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                {
                    if (IsWarpScramblingEntity)
                        _abyssalTargetPriority = 8.7;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe)
                        _abyssalTargetPriority = 8.6;

                    if (IsWarpScramblingEntity && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (IsWarpScramblingEntity && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.4;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.3;

                    if (IsWarpScramblingEntity && IsNPCFrigate)
                        _abyssalTargetPriority = 8.2;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCFrigate)
                        _abyssalTargetPriority = 8.1;
                }

                if (IsTargetingDrones && !IsNPCBattleship) // focus targets which are focusing the drones
                {
                    _abyssalTargetPriority = 7.99;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                        _abyssalTargetPriority = 7.9;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("caster Tessella".ToLower())) //40 OMNI damage ROF: 4 seconds - in 10 sec 160 damage
                        _abyssalTargetPriority = 7.8;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                        _abyssalTargetPriority = 7.7;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                        _abyssalTargetPriority = 7.6;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                        _abyssalTargetPriority = 7.5;

                    if (TypeName.ToLower().Contains("ephialtes confuser")) //tracking disruptor
                        _abyssalTargetPriority = 7.48;
                    if (TypeName.ToLower().Contains("ephialtes illuminator")) //target painter
                        _abyssalTargetPriority = 7.47;
                    if (TypeName.ToLower().Contains("ephialtes spearfisher")) //warp scramble
                        _abyssalTargetPriority = 7.46;
                    if (TypeName.ToLower().Contains("ephialtes lancer")) //DPS
                        _abyssalTargetPriority = 7.45;
                    if (TypeName.ToLower().Contains("ephialtes obfuscator")) //damps
                        _abyssalTargetPriority = 7.44;
                    if (TypeName.ToLower().Contains("nullcharge"))
                        _abyssalTargetPriority = 7.43;
                    if (TypeName.ToLower().Contains("Lucid Watchmen".ToLower())) //damage
                        _abyssalTargetPriority = 7.42;
                    if (IsRemoteRepairEntity && Combat.PotentialCombatTargets.All(i => !i.IsNPCBattlecruiser))
                        _abyssalTargetPriority = 7.41;
                    if (TypeName.ToLower().Contains("renewing".ToLower()) || TypeName.ToLower().Contains("priest".ToLower()) || TypeName.ToLower().Contains("plateforger".ToLower()) || TypeName.ToLower().Contains("preserver".ToLower()) || TypeName.ToLower().Contains("fieldweaver".ToLower()) || TypeName.ToLower().Contains("burst".ToLower()))
                        _abyssalTargetPriority = 7.40;
                    if (IsWebbingEntity)
                        _abyssalTargetPriority = 7.39;
                    if (IsWebbingEntity && GroupId == 1997 && (IsNPCBattlecruiser || IsNPCCruiser || IsNPCFrigate) && Distance < 19500)
                        _abyssalTargetPriority = 7.37;
                    if (TypeName.ToLower().Contains("Lucid Upholder".ToLower())) //webs
                        _abyssalTargetPriority = 7.36;
                    if (TypeName.ToLower().Contains("ephialtes entangler")) //webifier
                        _abyssalTargetPriority = 7.35;
                    if (IsNeutingEntity)
                        _abyssalTargetPriority = 7.34;
                    if (TypeName.ToLower().Contains("Lucid Sentinel".ToLower())) //neuts
                        _abyssalTargetPriority = 7.33;
                    if (TypeName.ToLower().Contains("ephialtes dissipator")) //neutralizer
                        _abyssalTargetPriority = 7.32;
                }

                //9-2024
                //adjust the priority list in the KarybdisTyrannosSpawn to kill first cruisers that are webbing us over yellow boxing cruisers.. sometimes we find ourselves in a situation where we have like 6 cruiser left and 4-5 are yellow boxing and 1 is webbing us and bc we are webbed the Karybdis BS is able to get some good shots off on us.
                if (IsWebbingEntity)
                    _abyssalTargetPriority = 6;

                AdjustAbyssalTargetPriority_Common();

                return _abyssalTargetPriority ?? 50;
            }
        }
        private double AbyssalTargetPriority_LuciferSpawn
        {
            get
            {
                //https://caldarijoans.streamlit.app/Room_Overviews_and_Tips
                //Primary Lucifer Cynabals before Elite Lucifer Cynabals in almost every scenario. The former has significantly less tank but deals excellent damage.
                //Lucifer Echos are relatively thin frigates that are fast and have Scram + Web. Quickly killing one Echo can be a good choice at the start of an Angel room. - How?

                _abyssalTargetPriority = 9999;

                if (ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate)
                {
                    if (TypeName != "Lucifer Cynabal")
                    {
                        if (Combat.PotentialCombatTargets.Where(x => x.Id != Id).Any(i => i.TypeName == "Lucifer Cynabal"))
                        {
                            //If we have any Lucifer Cynabal on the field assign 9999 to the priority to everything else so that we dont even target anything else!
                            //including any Elite Lucifer Cynabal: we want to shoot the non-elite ones first!
                            return _abyssalTargetPriority ?? 0;
                        }
                    }

                    if (!Combat.PotentialCombatTargets.Any(i => i.TypeName == "Lucifer Cynabal"))
                    {
                        if (TypeName != "Elite Lucifer Cynabal")
                        {
                            if (Combat.PotentialCombatTargets.Where(x => x.Id != Id).Any(i => i.TypeName == "Elite Lucifer Cynabal"))
                            {
                                //after the regular Lucifer Cynabal(s) are gone work on the Elite Lucifer Cynabal(s)
                                return _abyssalTargetPriority ?? 0;
                            }
                        }
                    }
                }

                if (TypeName.ToLower().Contains("Elite Lucifer Cynabal".ToLower()))
                    _abyssalTargetPriority = 1.02; //Elite

                if (TypeName.ToLower() == "Lucifer Cynabal".ToLower())
                    _abyssalTargetPriority = 1.01; //Not elite

                if (IsNPCFrigate)
                    _abyssalTargetPriority = 40;
                if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                    _abyssalTargetPriority = 39.9;
                if (IsNPCFrigate && TypeName.ToLower().Contains("caster Tessella".ToLower())) //40 OMNI damage ROF: 4 seconds - in 10 sec 160 damage
                    _abyssalTargetPriority = 39.8;
                if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                    _abyssalTargetPriority = 39.7;
                if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                    _abyssalTargetPriority = 39.6;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                    _abyssalTargetPriority = 39.5;
                if (IsNPCCruiser)
                    _abyssalTargetPriority = 30;

                if (IsTargetPaintingEntity)
                    _abyssalTargetPriority = 25;
                if (IsSensorDampeningEntity)
                    _abyssalTargetPriority = 23;
                if (IsRemoteRepairEntity && Combat.PotentialCombatTargets.All(i => !i.IsNPCBattlecruiser))
                    _abyssalTargetPriority = 19;
                if (TypeName.ToLower().Contains("renewing".ToLower()) || TypeName.ToLower().Contains("priest".ToLower()) || TypeName.ToLower().Contains("plateforger".ToLower()) || TypeName.ToLower().Contains("preserver".ToLower()) || TypeName.ToLower().Contains("fieldweaver".ToLower()) || TypeName.ToLower().Contains("burst".ToLower()))
                    _abyssalTargetPriority = 18;
                if (IsNeutingEntity)
                    _abyssalTargetPriority = 17;

                if (TypeName.ToLower().Contains("Lucifer Ixion".ToLower()))
                    _abyssalTargetPriority = 16;
                if (TypeName.ToLower().Contains("Lucifer Burst".ToLower()))
                    _abyssalTargetPriority = 15;
                if (TypeName.ToLower().Contains("Elite Lucifer Cynabal".ToLower()))
                    _abyssalTargetPriority = 14;
                if (TypeName.ToLower().Contains("Lucifer Cynabal".ToLower()))
                    _abyssalTargetPriority = 13;

                if (TypeName.ToLower().Contains("Snarecaster Tessella".ToLower())) //webbing frigate
                    _abyssalTargetPriority = 12.10;

                if (TypeName.ToLower().Contains("Lucifer Swordspine".ToLower()))
                    _abyssalTargetPriority = 12.09;
                if (TypeName.ToLower().Contains("Lucifer Echo".ToLower()))
                    _abyssalTargetPriority = 12.08;
                if (TypeName.ToLower().Contains("Elite Lucifer Dramiel".ToLower()))
                    _abyssalTargetPriority = 12.07;
                if (TypeName.ToLower() == "Lucifer Dramiel".ToLower())
                    _abyssalTargetPriority = 12.06;

                if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                {
                    //small things first if in a worm!
                    if (TypeName.ToLower().Contains("Elite Lucifer Dramiel".ToLower()))
                        _abyssalTargetPriority = .07;
                    if (TypeName.ToLower() == "Lucifer Dramiel".ToLower())
                        _abyssalTargetPriority = .06;
                }

                if (IsNPCFrigate && IsWebbingEntity)
                {
                    _abyssalTargetPriority = 12.0666;
                    if (TypeName.ToLower().Contains("Snarecaster Tessella".ToLower())) //webbing frigate
                        _abyssalTargetPriority = 12.0665;

                    if (TypeName.ToLower().Contains("Lucifer Swordspine".ToLower()))
                        _abyssalTargetPriority = 12.0664;
                    if (TypeName.ToLower().Contains("Lucifer Echo".ToLower()))
                        _abyssalTargetPriority = 12.0663;
                    if (TypeName.ToLower().Contains("Elite Lucifer Dramiel".ToLower()))
                        _abyssalTargetPriority = 12.0662;
                    if (TypeName.ToLower() == "Lucifer Dramiel".ToLower())
                        _abyssalTargetPriority = 12.0661;
                }

                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                {
                    if (IsWarpScramblingEntity)
                    {
                        _abyssalTargetPriority = 8.7;
                        if (.5 > ShieldPct)
                            _abyssalTargetPriority = 8.69;
                        if (.25 > ShieldPct)
                            _abyssalTargetPriority = 8.68;
                        if (.01 > ShieldPct)
                            _abyssalTargetPriority = 8.67;
                        if (.5 > ArmorPct)
                            _abyssalTargetPriority = 8.66;
                        if (.25 > ArmorPct)
                            _abyssalTargetPriority = 8.65;
                        if (.01 > ArmorPct)
                            _abyssalTargetPriority = 8.64;
                    }

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe)
                    {
                        _abyssalTargetPriority = 8.6;
                        if (.5 > ShieldPct)
                            _abyssalTargetPriority = 8.59;
                        if (.25 > ShieldPct)
                            _abyssalTargetPriority = 8.58;
                        if (.01 > ShieldPct)
                            _abyssalTargetPriority = 8.57;
                        if (.5 > ArmorPct)
                            _abyssalTargetPriority = 8.56;
                        if (.25 > ArmorPct)
                            _abyssalTargetPriority = 8.55;
                        if (.01 > ArmorPct)
                            _abyssalTargetPriority = 8.54;
                    }

                    if (IsWarpScramblingEntity && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (IsWarpScramblingEntity && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.4;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.3;

                    if (IsWarpScramblingEntity && IsNPCFrigate)
                        _abyssalTargetPriority = 8.2;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCFrigate)
                        _abyssalTargetPriority = 8.1;
                }

                if (IsTargetingDrones) // focus targets which are focusing the drones
                {
                    _abyssalTargetPriority = 7.99;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                        _abyssalTargetPriority = 7.9;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("caster Tessella".ToLower())) //40 OMNI damage ROF: 4 seconds - in 10 sec 160 damage
                        _abyssalTargetPriority = 7.8;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                        _abyssalTargetPriority = 7.7;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                        _abyssalTargetPriority = 7.6;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                        _abyssalTargetPriority = 7.5;

                    if (IsNPCFrigate && TypeName.ToLower().Contains("Lucifer Ixion".ToLower()))
                        _abyssalTargetPriority = 7.27;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Lucifer Burst".ToLower()))
                        _abyssalTargetPriority = 7.26;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Lucifer Swordspine".ToLower()))
                        _abyssalTargetPriority = 7.22;
                    if (IsWebbingEntity)
                        _abyssalTargetPriority = 7.21;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Lucifer Echo".ToLower()))
                        _abyssalTargetPriority = 7.20;
                    if (TypeName.ToLower().Contains("Elite Lucifer Dramiel".ToLower()))
                        _abyssalTargetPriority = 7.19;
                    if (TypeName.ToLower() == "Lucifer Dramiel".ToLower())
                        _abyssalTargetPriority = 7.18;
                    if (TypeName.ToLower().Contains("Elite Lucifer Cynabal".ToLower()))
                        _abyssalTargetPriority = 7.11;
                    if (TypeName.ToLower() == "Lucifer Cynabal".ToLower())
                        _abyssalTargetPriority = 7.10;

                    if (IsNPCFrigate && IsWebbingEntity)
                    {
                        _abyssalTargetPriority = 7.0666;
                        if (TypeName.ToLower().Contains("Snarecaster Tessella".ToLower())) //webbing frigate
                            _abyssalTargetPriority = 7.0665;

                        if (TypeName.ToLower().Contains("Lucifer Swordspine".ToLower()))
                            _abyssalTargetPriority = 7.0664;
                        if (TypeName.ToLower().Contains("Lucifer Echo".ToLower()))
                            _abyssalTargetPriority = 7.0663;
                        if (TypeName.ToLower().Contains("Elite Lucifer Dramiel".ToLower()))
                            _abyssalTargetPriority = 7.0662;
                        if (TypeName.ToLower() == "Lucifer Dramiel".ToLower())
                            _abyssalTargetPriority = 7.0661;
                    }
                }

                if (TypeName.ToLower().Contains("Elite Lucifer Cynabal".ToLower()))
                    _abyssalTargetPriority = 1.02; //Elite

                if (TypeName.ToLower() == "Lucifer Cynabal".ToLower())
                    _abyssalTargetPriority = 1.01; //Not elite



                AdjustAbyssalTargetPriority_Common();

                return _abyssalTargetPriority.Value;
            }
        }

        private double AbyssalTargetPriority_AbyssalOvermindDroneBSSpawn
        {
            get
            {
                _abyssalTargetPriority = 50;

                if (IsNPCBattleship)
                    _abyssalTargetPriority = 50;

                if (IsNPCFrigate)
                    _abyssalTargetPriority = 40;
                if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                    _abyssalTargetPriority = 39.9;

                if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                    _abyssalTargetPriority = 39.7;
                if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                    _abyssalTargetPriority = 39.6;

                //Needle Lower DPS
                if (IsNPCFrigate && TypeName.ToLower().Contains("Emberneedle Tessella".ToLower())) //20 Thermal * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.5;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Strikeneedle Tessella".ToLower())) //20 Kinetic * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.4;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Blastneedle Tessella".ToLower())) //20 Explosive * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.3;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparkneedle Tessella".ToLower())) //20 EM * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.2;

                //Lance High DPS
                if (IsNPCFrigate && TypeName.ToLower().Contains("Blastlance Tessella".ToLower())) //40 Explosive * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.5;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Emberlance Tessella".ToLower())) //40 Thermal * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.4;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 EM * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.3;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Strikelance Tessella".ToLower())) //40 Kinetic * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.2;

                if (IsWebbingEntity)
                    _abyssalTargetPriority = 24;


                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                {
                    if (IsWarpScramblingEntity)
                        _abyssalTargetPriority = 8.7;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe)
                        _abyssalTargetPriority = 8.6;

                    if (IsWarpScramblingEntity && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (IsWarpScramblingEntity && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.4;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.3;

                    if (IsWarpScramblingEntity && IsNPCFrigate)
                        _abyssalTargetPriority = 8.2;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCFrigate)
                        _abyssalTargetPriority = 8.1;
                }

                if (IsTargetingDrones) // focus targets which are focusing the drones
                {
                    _abyssalTargetPriority = 7.99;

                    if (GroupId == 1997 && IsNPCBattlecruiser) // prio rogue bcs
                        _abyssalTargetPriority = 7.98;
                    if (GroupId == 1997 && IsNPCBattlecruiser && TypeName.ToLower().Contains("sparkgrip")) // high em damage rogue drone bcs
                        _abyssalTargetPriority = 7.97;
                    if (GroupId == 1997 && IsNPCBattlecruiser && TypeName.ToLower().Contains("blastgrip")) // high explo damage rogue drone bcs
                        _abyssalTargetPriority = 7.96;

                    if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                        _abyssalTargetPriority = 7.8;

                    if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                        _abyssalTargetPriority = 7.6;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                        _abyssalTargetPriority = 7.5;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                        _abyssalTargetPriority = 7.4;

                    if (IsWarpScramblingEntity)
                        _abyssalTargetPriority = 7.3;

                    if (IsWebbingEntity)
                        _abyssalTargetPriority = 7.2;

                    if (IsNPCFrigate && IsWebbingEntity)
                        _abyssalTargetPriority = 7.15;

                    if (IsNPCFrigate && TypeName.ToLower().Contains("Snarecaster Tessella".ToLower())) //webbing frigate
                        _abyssalTargetPriority = 7.1;
                }

                if (IsRemoteRepairEntity && Combat.PotentialCombatTargets.All(i => !i.IsNPCBattlecruiser))
                    _abyssalTargetPriority = 3.5;

                if (TypeName.ToLower().Contains("renewing".ToLower()) || TypeName.ToLower().Contains("priest".ToLower()) || TypeName.ToLower().Contains("plateforger".ToLower()) || TypeName.ToLower().Contains("preserver".ToLower()) || TypeName.ToLower().Contains("fieldweaver".ToLower()) || TypeName.ToLower().Contains("burst".ToLower()))
                    _abyssalTargetPriority = 3;

                if (IsNPCFrigate && IsWebbingEntity)
                    _abyssalTargetPriority = 2.15;

                if (IsNPCFrigate && TypeName.ToLower().Contains("Snarecaster Tessella".ToLower())) //webbing frigate
                    _abyssalTargetPriority = 2.1;

                AdjustAbyssalTargetPriority_Common();

                return _abyssalTargetPriority.Value;
            }
        }
        private double AbyssalTargetPriority_HighAngleDroneBattlecruiserSpawn
        {
            get
            {
                _abyssalTargetPriority = 50;

                if (IsNPCFrigate)
                    _abyssalTargetPriority = 40;
                if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                    _abyssalTargetPriority = 39.9;

                if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessella".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                    _abyssalTargetPriority = 39.7;
                if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                    _abyssalTargetPriority = 39.6;

                //Needle Lower DPS
                if (IsNPCFrigate && TypeName.ToLower().Contains("Emberneedle Tessella".ToLower())) //20 Thermal * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.5;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Strikeneedle Tessella".ToLower())) //20 Kinetic * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.4;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Blastneedle Tessella".ToLower())) //20 Explosive * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.3;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparkneedle Tessella".ToLower())) //20 EM * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.2;

                //Lance High DPS
                if (IsNPCFrigate && TypeName.ToLower().Contains("Blastlance Tessella".ToLower())) //40 Explosive * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.5;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Emberlance Tessella".ToLower())) //40 Thermal * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.4;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 EM * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.3;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Strikelance Tessella".ToLower())) //40 Kinetic * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.2;

                //Blastgrip Tessera   Explosive   900
                //Embergrip Tessera   Thermal 525
                //Sparkgrip Tessera   EM  775
                //Strikegrip Tessera  Kinetic 650

                if (GroupId == 1997 && (IsNPCBattlecruiser || IsNPCCruiser || IsNPCFrigate) && Distance < 19500) // prio nearby rogue drones
                    _abyssalTargetPriority = 21;

                if (GroupId == 1997 && IsNPCBattlecruiser) // prio rogue bcs
                    _abyssalTargetPriority = 20;

                if (TypeName.ToLower().Contains("Blastgrip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage - Explosive!
                    _abyssalTargetPriority = 19.77;
                if (TypeName.ToLower().Contains("Embergrip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage - Thermal!
                    _abyssalTargetPriority = 19.76;
                if (TypeName.ToLower().Contains("Strikegrip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage - Kinetic!
                    _abyssalTargetPriority = 19.75;

                if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                    _abyssalTargetPriority = 18.6;
                if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                    _abyssalTargetPriority = 18.5;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                    _abyssalTargetPriority = 18.4;

                if (TypeName.ToLower().Contains("Sparkgrip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage - EM!
                    _abyssalTargetPriority = 17.74;

                if (IsWebbingEntity && GroupId == 1997 && (IsNPCBattlecruiser || IsNPCCruiser || IsNPCFrigate) && Distance < 19500)
                    _abyssalTargetPriority = 13;

                if (IsWebbingEntity)
                    _abyssalTargetPriority = 9.5;

                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                {
                    if (IsWarpScramblingEntity)
                        _abyssalTargetPriority = 8.7;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe)
                        _abyssalTargetPriority = 8.6;

                    if (IsWarpScramblingEntity && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (IsWarpScramblingEntity && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.4;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.3;

                    if (IsWarpScramblingEntity && IsNPCFrigate)
                        _abyssalTargetPriority = 8.2;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCFrigate)
                        _abyssalTargetPriority = 8.1;
                }

                if (IsTargetingDrones) // focus targets which are focusing the drones
                {
                    _abyssalTargetPriority = 9.99;

                    if (GroupId == 1997 && IsNPCBattlecruiser) // prio rogue bcs
                        _abyssalTargetPriority = 9.98;

                    if (TypeName.ToLower().Contains("Blastgrip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage - Explosive!
                        _abyssalTargetPriority = 9.77;
                    if (TypeName.ToLower().Contains("Embergrip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage - Thermal!
                        _abyssalTargetPriority = 9.76;
                    if (TypeName.ToLower().Contains("Strikegrip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage - Kinetic!
                        _abyssalTargetPriority = 9.75;

                    if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                        _abyssalTargetPriority = 8.6;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                        _abyssalTargetPriority = 8.5;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                        _abyssalTargetPriority = 8.4;

                    if (TypeName.ToLower().Contains("Sparkgrip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage - EM!
                        _abyssalTargetPriority = 7.74;

                    if (IsWarpScramblingEntity)
                        _abyssalTargetPriority = 7.3;

                    if (IsWebbingEntity)
                        _abyssalTargetPriority = 7.2;

                    if (IsNPCFrigate && IsWebbingEntity)
                        _abyssalTargetPriority = 7.15;

                    if (IsNPCFrigate && TypeName.ToLower().Contains("Snarecaster Tessella".ToLower())) //webbing frigate
                        _abyssalTargetPriority = 7.1;
                }


                if (IsRemoteRepairEntity && Combat.PotentialCombatTargets.All(i => !i.IsNPCBattlecruiser))
                    _abyssalTargetPriority = 3.5;

                if (TypeName.ToLower().Contains("renewing".ToLower()) || TypeName.ToLower().Contains("priest".ToLower()) || TypeName.ToLower().Contains("plateforger".ToLower()) || TypeName.ToLower().Contains("preserver".ToLower()) || TypeName.ToLower().Contains("fieldweaver".ToLower()) || TypeName.ToLower().Contains("burst".ToLower()))
                    _abyssalTargetPriority = 3;

                if (TypeName.ToLower().Contains("Sparkgrip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage - EM!
                    _abyssalTargetPriority = 2.99;

                if (IsNPCFrigate && IsWebbingEntity)
                    _abyssalTargetPriority = 2.15;

                if (IsNPCFrigate && TypeName.ToLower().Contains("Snarecaster Tessella".ToLower())) //webbing frigate
                    _abyssalTargetPriority = 2.1;

                AdjustAbyssalTargetPriority_Common();

                return _abyssalTargetPriority.Value;
            }
        }
        private double AbyssalTargetPriority_DamavikFrigateSpawn
        {
            get
            {
                //https://caldarijoans.streamlit.app/Room_Overviews_and_Tips
                //Prioritize Damaviks by EWAR threats to your fit.
                //Take advantage of Blue Clouds and Tracking Towers in Damavik heavy rooms. This spawn is unlikely to have problems applying damage to your fit so you might as well improve your own application.
                _abyssalTargetPriority = 50;

                if (IsNPCFrigate)
                    _abyssalTargetPriority = 40;

                if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                    _abyssalTargetPriority = 39.9;

                if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                    _abyssalTargetPriority = 39.7;
                if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                    _abyssalTargetPriority = 39.6;

                //Needle Lower DPS
                if (IsNPCFrigate && TypeName.ToLower().Contains("Emberneedle Tessella".ToLower())) //20 Thermal * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.5;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Strikeneedle Tessella".ToLower())) //20 Kinetic * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.4;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Blastneedle Tessella".ToLower())) //20 Explosive * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.3;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparkneedle Tessella".ToLower())) //20 EM * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.2;

                //Lance High DPS
                if (IsNPCFrigate && TypeName.ToLower().Contains("Blastlance Tessella".ToLower())) //40 Explosive * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.5;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Emberlance Tessella".ToLower())) //40 Thermal * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.4;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 EM * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.3;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Strikelance Tessella".ToLower())) //40 Kinetic * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.2;

                if (IsNPCCruiser)
                    _abyssalTargetPriority = 30;

                if (TypeName.ToLower().Contains("Anchoring Damavik".ToLower()))
                    _abyssalTargetPriority = 14;

                if (TypeName.ToLower().Contains("Striking Damavik".ToLower()))
                    _abyssalTargetPriority = 13;

                if (TypeName.ToLower().Contains("Ghosting Damavik".ToLower()))
                    _abyssalTargetPriority = 12;

                if (TypeName.ToLower().Contains("Starving Damavik".ToLower()))
                    _abyssalTargetPriority = 11;

                if (TypeName.ToLower().Contains("Rodiva".ToLower()))
                    _abyssalTargetPriority = 10.5;

                if (TypeName.ToLower().Contains("Tangling Damavik".ToLower()))
                    _abyssalTargetPriority = 10;

                if (IsWebbingEntity)
                    _abyssalTargetPriority = 9.6;

                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                {
                    if (IsWarpScramblingEntity)
                        _abyssalTargetPriority = 8.7;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe)
                        _abyssalTargetPriority = 8.6;

                    if (IsWarpScramblingEntity && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (IsWarpScramblingEntity && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.4;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.3;

                    if (TypeName.ToLower().Contains("Anchoring Damavik".ToLower()))
                        _abyssalTargetPriority = 8.21;

                    if (IsWarpScramblingEntity && IsNPCFrigate)
                        _abyssalTargetPriority = 8.2;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCFrigate)
                        _abyssalTargetPriority = 8.1;
                }

                if (IsTargetingDrones) // focus targets which are focusing the drones
                {
                    _abyssalTargetPriority = 7.99;

                    if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                        _abyssalTargetPriority = 7.8;

                    if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                        _abyssalTargetPriority = 7.6;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                        _abyssalTargetPriority = 7.5;

                    if (IsWarpScramblingEntity)
                        _abyssalTargetPriority = 7.3;

                    if (IsWebbingEntity)
                        _abyssalTargetPriority = 7.2;

                    if (IsNPCFrigate && IsWebbingEntity)
                        _abyssalTargetPriority = 7.15;

                    if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                    {
                        if (IsWarpScramblingEntity)
                        {
                            _abyssalTargetPriority = 7.7;
                            if (.5 > ShieldPct)
                                _abyssalTargetPriority = 7.69;
                            if (.25 > ShieldPct)
                                _abyssalTargetPriority = 7.68;
                            if (.01 > ShieldPct)
                                _abyssalTargetPriority = 7.67;
                            if (.5 > ArmorPct)
                                _abyssalTargetPriority = 7.66;
                            if (.25 > ArmorPct)
                                _abyssalTargetPriority = 7.65;
                            if (.01 > ArmorPct)
                                _abyssalTargetPriority = 7.64;
                        }

                        if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe)
                        {
                            _abyssalTargetPriority = 7.6;
                            if (.5 > ShieldPct)
                                _abyssalTargetPriority = 7.59;
                            if (.25 > ShieldPct)
                                _abyssalTargetPriority = 7.58;
                            if (.01 > ShieldPct)
                                _abyssalTargetPriority = 7.57;
                            if (.5 > ArmorPct)
                                _abyssalTargetPriority = 7.56;
                            if (.25 > ArmorPct)
                                _abyssalTargetPriority = 7.55;
                            if (.01 > ArmorPct)
                                _abyssalTargetPriority = 7.54;
                        }

                        if (IsWarpScramblingEntity && IsNPCCruiser)
                            _abyssalTargetPriority = 7.5;

                        if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCCruiser)
                            _abyssalTargetPriority = 7.5;

                        if (IsWarpScramblingEntity && IsNPCDestroyer)
                            _abyssalTargetPriority = 7.4;

                        if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCDestroyer)
                            _abyssalTargetPriority = 7.3;

                        if (IsWarpScramblingEntity && IsNPCFrigate)
                            _abyssalTargetPriority = 7.2;

                        if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCFrigate)
                            _abyssalTargetPriority = 7.1;
                    }
                }

                if (IsNPCFrigate && IsWebbingEntity)
                    _abyssalTargetPriority = 2.15;

                AdjustAbyssalTargetPriority_Common();

                return _abyssalTargetPriority.Value;
            }
        }
        private double AbyssalTargetPriority_DroneFrigateSpawn
        {
            get
            {
                //https://caldarijoans.streamlit.app/Room_Overviews_and_Tips
                //Deviant Automata Suppressor towers can be used to kill Rogue Drone Frigates. Short Range versions do high DPS out to 25km. Medium Range version do lower DPS out to 35km.
                //The frigate swarm frequently stops moving upon killing one of their allies. Use this to your advantage to pull range or take easy shots.

                _abyssalTargetPriority = 50;

                if (IsNPCFrigate)
                    _abyssalTargetPriority = 40;
                if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                    _abyssalTargetPriority = 39.9;

                if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                    _abyssalTargetPriority = 39.7;
                if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                    _abyssalTargetPriority = 39.6;

                //Needle Lower DPS
                if (IsNPCFrigate && TypeName.ToLower().Contains("Emberneedle Tessella".ToLower())) //20 Thermal * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.5;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Strikeneedle Tessella".ToLower())) //20 Kinetic * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.4;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Blastneedle Tessella".ToLower())) //20 Explosive * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.3;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparkneedle Tessella".ToLower())) //20 EM * 2.5 = [ 50 damage ] every 2 seconds = [ 25 ] DPS @ 2.5k Optimal 5k falloff
                    _abyssalTargetPriority = 39.2;

                //Lance High DPS
                if (IsNPCFrigate && TypeName.ToLower().Contains("Emberlance Tessella".ToLower())) //40 Thermal * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.4;

                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 EM * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.3;

                if (IsNPCFrigate && TypeName.ToLower().Contains("Strikelance Tessella".ToLower())) //40 Kinetic * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.2;

                if (IsNPCFrigate && TypeName.ToLower().Contains("Blastlance Tessella".ToLower())) //40 Explosive * 2.5 = [ 100 damage ] every 2 seconds = [ 50 ] DPS @ 5k Optimal 10k falloff
                    _abyssalTargetPriority = 38.1;

                if (IsNPCCruiser)
                    _abyssalTargetPriority = 30;

                if (TypeName.ToLower().Contains("nullcharge") || TypeName.ToLower().Contains("ephialtes"))
                    _abyssalTargetPriority = 12;
                if (TypeName.ToLower().Contains("kikimora".ToLower()))
                {
                    _abyssalTargetPriority = 11;
                    if (TypeName.ToLower().Contains("ghosting kikimora".ToLower()))
                        _abyssalTargetPriority = 10.5;
                    if (TypeName.ToLower().Contains("striking kikimora".ToLower()))
                        _abyssalTargetPriority = 10;
                    long RemoteRepRange = 15000;
                    if (Combat.PotentialCombatTargets.Any(i => i.Id != Id && TypeName.ToLower().Contains("kikimora".ToLower()) && RemoteRepRange > i._directEntity.DistanceTo(this)))
                    {
                        _abyssalTargetPriority = 11.0666;
                    }
                }
                if (IsWebbingEntity)
                    _abyssalTargetPriority = 9.5;

                if (IsNPCFrigate && TypeName.ToLower().Contains("lance".ToLower()) && Combat.PotentialCombatTargets.Count(i => i.TypeName.ToLower().Contains("lance".ToLower())) > 6) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                    _abyssalTargetPriority = 8.8;

                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                {
                    if (IsWarpScramblingEntity)
                        _abyssalTargetPriority = 8.7;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe)
                        _abyssalTargetPriority = 8.6;

                    if (IsWarpScramblingEntity && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCCruiser)
                        _abyssalTargetPriority = 8.5;

                    if (IsWarpScramblingEntity && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.4;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCDestroyer)
                        _abyssalTargetPriority = 8.3;

                    if (IsWarpScramblingEntity && IsNPCFrigate)
                        _abyssalTargetPriority = 8.2;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe && IsNPCFrigate)
                        _abyssalTargetPriority = 8.1;
                }

                if (IsTargetingDrones && !IsNPCBattleship) // focus targets which are focusing the drones
                {
                    _abyssalTargetPriority = 7.99;

                    if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                        _abyssalTargetPriority = 7.8;

                    if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                        _abyssalTargetPriority = 7.6;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                        _abyssalTargetPriority = 7.5;


                    if (IsWarpScramblingEntity)
                        _abyssalTargetPriority = 7.3;

                    if (IsWebbingEntity)
                        _abyssalTargetPriority = 7.2;

                    if (IsNPCFrigate && IsWebbingEntity)
                        _abyssalTargetPriority = 7.15;

                    if (IsNPCFrigate && TypeName.ToLower().Contains("Snarecaster Tessella".ToLower())) //webbing frigate
                        _abyssalTargetPriority = 7.1;

                    //if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower()) && Combat.PotentialCombatTargets.Count(i => i.TypeName.ToLower().Contains("lance Tessella".ToLower())) > 6) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                    //    _abyssalTargetPriority = 7;
                }

                if (IsNPCFrigate && IsWebbingEntity)
                    _abyssalTargetPriority = 2.15;

                if (IsNPCFrigate && TypeName.ToLower().Contains("Snarecaster Tessella".ToLower())) //webbing frigate
                    _abyssalTargetPriority = 2.1;

                //if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower()) && Combat.PotentialCombatTargets.Count(i => i.TypeName.ToLower().Contains("lance Tessella".ToLower())) > 6) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                //    _abyssalTargetPriority = 1;

                AdjustAbyssalTargetPriority_Common();

                return _abyssalTargetPriority.Value;
            }
        }

        private double? _maxGigaJoulePerSecondTank
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsActiveTanked)
                {
                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                    {
                        //In a filament cloud
                        if (ESCache.Instance.ActiveShip.IsLocatedWithinFilamentCloud && ESCache.Instance.ActiveShip.IsShieldTanked)
                        {
                            if (DirectEve.Interval(5000)) Log.WriteLine("IsLocatedWithinFilamentCloud [ true ]");
                            if (ESCache.Instance.Modules.Any(i => (i.IsShieldRepairModule || i.IsArmorRepairModule) && i.IsOverloaded))
                            {
                                return 21;
                            }

                            return 32;
                        }

                        if (ESCache.Instance.Modules.Any(i => i.IsOverloaded))
                        {
                            return 42;
                        }

                        return 52;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.VexorNavyIssue)
                    {
                        //In a filament cloud
                        if (ESCache.Instance.ActiveShip.IsLocatedWithinFilamentCloud && ESCache.Instance.ActiveShip.IsShieldTanked)
                        {
                            if (DirectEve.Interval(5000)) Log.WriteLine("IsLocatedWithinFilamentCloud [ true ]");
                            if (ESCache.Instance.Modules.Any(i => (i.IsShieldRepairModule || i.IsArmorRepairModule) && i.IsOverloaded))
                            {
                                return 15;
                            }

                            return 20;
                        }

                        if (ESCache.Instance.Modules.Any(i => i.IsOverloaded))
                        {
                            return 30;
                        }

                        return 40;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                    {
                        //In a filament cloud
                        if (ESCache.Instance.ActiveShip.IsLocatedWithinFilamentCloud && ESCache.Instance.ActiveShip.IsShieldTanked)
                        {
                            Log.WriteLine("IsLocatedWithinFilamentCloud [ true ]");
                            if (ESCache.Instance.Modules.Any(i => (i.IsShieldRepairModule || i.IsArmorRepairModule) && i.IsOverloaded))
                            {
                                return 21;
                            }

                            return 32;
                        }

                        if (ESCache.Instance.Modules.Any(i => i.IsOverloaded))
                        {
                            return 42;
                        }

                        return 52;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Vagabond)
                    {
                        //In a filament cloud
                        if (ESCache.Instance.ActiveShip.IsLocatedWithinFilamentCloud && ESCache.Instance.ActiveShip.IsShieldTanked)
                        {
                            Log.WriteLine("IsLocatedWithinFilamentCloud [ true ]");
                            if (ESCache.Instance.Modules.Any(i => (i.IsShieldRepairModule || i.IsArmorRepairModule) && i.IsOverloaded))
                            {
                                return 21;
                            }

                            return 32;
                        }

                        if (ESCache.Instance.Modules.Any(i => i.IsOverloaded))
                        {
                            return 42;
                        }

                        return 52;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                    {
                        //In a filament cloud
                        if (ESCache.Instance.ActiveShip.IsLocatedWithinFilamentCloud && ESCache.Instance.ActiveShip.IsShieldTanked)
                        {
                            Log.WriteLine("IsLocatedWithinFilamentCloud [ true ]");
                            if (ESCache.Instance.Modules.Any(i => i.IsOverloaded))
                            {
                                return 21;
                            }

                            return 32;
                        }

                        if (ESCache.Instance.Modules.Any(i => i.IsOverloaded))
                        {
                            return 44;
                        }

                        return 52;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Worm)
                    {
                        if (ESCache.Instance.Modules.Any(i => i.IsOverloaded))
                        {
                            return 6;
                        }

                        return 9;
                    }

                    if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Frigate || ESCache.Instance.ActiveShip.GroupId == (int)Group.Destroyer)
                    {
                        //fixme
                        return 1;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                    {
                        return 4.5;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Hawk)
                    {
                        return 4.5;
                    }

                    if (ESCache.Instance.ActiveShip.GroupId == (int)Group.AssaultShip)
                    {
                        return 4.5;
                    }

                    return 52;
                }

                return 999;
            }
        }

        private double? _abyssalTargetPriority { get; set; } = null;
        public double AbyssalTargetPriority
        {
            get
            {
                if (!IsValid)
                    return 9999;

                if (IsPlayer)
                {
                    if (GroupId == (int)Group.AssaultShip)
                        return 9999;

                    if (GroupId == (int)Group.Frigate)
                        return 9999;

                    if (GroupId == (int)Group.Destroyer)
                        return 9999;

                    if (GroupId == (int)Group.Interceptor)
                        return 9999;

                    if (GroupId == (int)Group.ElectronicAttackShip)
                        return 9999;
                }

                //If IsTargetingDrones is true we dont want to use the cached value of _abyssalTargetPriority
                if (IsTargetingDrones)
                {
                    _abyssalTargetPriority = null;
                }
                else if (_abyssalTargetPriority.HasValue)
                {
                    return _abyssalTargetPriority.Value;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn)
                {
                    return AbyssalTargetPriority_DevotedCruiserSpawn;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                {
                    return AbyssalTargetPriority_VedmakCruiserSpawn;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakVilaCruiserSwarmerSpawn)
                {
                    return AbyssalTargetPriority_VedmakCruiserSpawn; //not a mistake: Vedmak and Vila spawns are *very* close and should be using the same priority list
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                {
                    return AbyssalTargetPriority_ConcordSpawn;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
                {
                    return AbyssalTargetPriority_DrekavacBattleCruiserSpawn;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                {
                    return AbyssalTargetPriority_AbyssalOvermindDroneBSSpawn;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                {
                    return AbyssalTargetPriority_HighAngleDroneBattlecruiserSpawn;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn)
                {
                    return AbyssalTargetPriority_LucidBSSpawn;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn)
                {
                    return AbyssalTargetPriority_LucidCruiserSpawn;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                {
                    return AbyssalTargetPriority_LuciferSpawn;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn)
                {
                    return AbyssalTargetPriority_EphialtesCruiserSpawn;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                {
                    return AbyssalTargetPriority_KikimoraDestroyerSpawn;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                {
                    return AbyssalTargetPriority_KarybdisTyrannosSpawn;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.RodivaSpawn)
                {
                    //not a mistake: Rodiva, Damavik and Vedmak spawns are *very* close and should be using the same priority list
                    return AbyssalTargetPriority_DamavikFrigateSpawn;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn)
                {
                    return AbyssalTargetPriority_DamavikFrigateSpawn;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                {
                    return AbyssalTargetPriority_DroneFrigateSpawn;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn)
                {
                    return AbyssalTargetPriority_LeshakBSSpawn;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)
                {
                    return AbyssalTargetPriority_LeshakBSSpawn;
                }

                _abyssalTargetPriority = 50;

                if (IsNPCFrigate)
                    _abyssalTargetPriority = 40;
                if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                    _abyssalTargetPriority = 39.9;
                if (IsNPCFrigate && TypeName.ToLower().Contains("caster Tessella".ToLower())) //40 OMNI damage ROF: 4 seconds - in 10 sec 160 damage
                    _abyssalTargetPriority = 39.8;
                if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                    _abyssalTargetPriority = 39.7;
                if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                    _abyssalTargetPriority = 39.6;
                if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                    _abyssalTargetPriority = 39.5;
                if (IsNPCCruiser)
                    _abyssalTargetPriority = 30;
                if (IsNPCBattlecruiser)
                    _abyssalTargetPriority = 31;
                if (IsNPCBattleship)
                    _abyssalTargetPriority = 32;
                if (IsHeavyDeepsIntegrating && (BracketType == BracketType.NPC_Battleship || BracketType == BracketType.NPC_Battlecruiser))
                    _abyssalTargetPriority = 26;
                if (IsTargetPaintingEntity)
                    _abyssalTargetPriority = 25;
                if (GroupId == 1997 || NPCHasVortonProjectorGuns) // https://everef.net/groups/1997 abyss drones OR vorton projectors
                    _abyssalTargetPriority = 22;
                if (GroupId == 1997 && (IsNPCBattlecruiser || IsNPCCruiser || IsNPCFrigate) && Distance < 19500) // prio nearby rogue drones
                    _abyssalTargetPriority = 21;
                if (GroupId == 1997 && IsNPCBattlecruiser) // prio rogue bcs
                    _abyssalTargetPriority = 20;
                if (GroupId == 1997 && IsNPCBattlecruiser && TypeName.ToLower().Contains("sparkgrip")) // high em damage rogue drone bcs
                    _abyssalTargetPriority = 19;
                if (GroupId == 1997 && IsNPCBattlecruiser && TypeName.ToLower().Contains("blastgrip")) // high explo damage rogue drone bcs
                    _abyssalTargetPriority = 17;
                if (IsSensorDampeningEntity)
                    _abyssalTargetPriority = 15;
                if (IsWebbingEntity && GroupId == 1997 && (IsNPCBattlecruiser || IsNPCCruiser || IsNPCFrigate) && Distance < 19500)
                    _abyssalTargetPriority = 13;
                if (TypeName.ToLower().Contains("nullcharge") || TypeName.ToLower().Contains("ephialtes"))
                    _abyssalTargetPriority = 12;
                if (TypeName.ToLower().Contains("kikimora"))
                    _abyssalTargetPriority = 11;
                if (TypeName.ToLower().Contains("striking") && TypeName.ToLower().Contains("kikimora"))
                    _abyssalTargetPriority = 10;
                if (IsWebbingEntity)
                    _abyssalTargetPriority = 8.8;

                if (ESCache.Instance.DirectEve.Modules.Any(i => i.IsMicroWarpDrive))
                {
                    if (IsWarpScramblingEntity)
                        _abyssalTargetPriority = 8.2;

                    if (!ESCache.Instance.EveAccount.UseFleetMgr && IsWarpScramblingMe)
                        _abyssalTargetPriority = 8.1;
                }

                if (IsTargetingDrones && !IsNPCBattleship) // focus targets which are focusing the drones
                {
                    _abyssalTargetPriority = 7.99;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("needle Tessella".ToLower())) //20 damage ROF: 2 seconds - in 10 sec 100 damage
                        _abyssalTargetPriority = 7.9;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("caster Tessella".ToLower())) //40 OMNI damage ROF: 4 seconds - in 10 sec 160 damage
                        _abyssalTargetPriority = 7.8;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("grip Tessera".ToLower())) //50 damage ROF: 5 seconds - in 10 sec 250 damage
                        _abyssalTargetPriority = 7.7;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("lance Tessella".ToLower())) //40 Damage ROF: 2 seconds - 80 per volley: in 10 sec ~400 damage
                        _abyssalTargetPriority = 7.6;
                    if (IsNPCFrigate && TypeName.ToLower().Contains("Sparklance Tessella".ToLower())) //40 * 2.5 = 90 EM Damage ROF: 2 seconds - 180 per volley: in 10 sec ~1000 damage (Ouch!)
                        _abyssalTargetPriority = 7.5;

                    if (NPCHasVortonProjectorGuns)
                        _abyssalTargetPriority = 7.4;

                    if (IsWarpScramblingEntity)
                        _abyssalTargetPriority = 7.3;

                    if (IsWebbingEntity)
                        _abyssalTargetPriority = 7.2;

                    if (IsTrackingDisruptingEntity)
                        _abyssalTargetPriority = 7.1;
                }

                if (NPCHasVortonProjectorGuns && Distance > 12000)
                    _abyssalTargetPriority = 5.5;
                if (IsDrifterBSSpawn && DirectEve.Entities.Count(e => e.IsNPCByBracketType && e.GroupId != 2009) <= 5) // prio the drifter bs if there are 5 ships left on grid, so the turrets can also do some work while the drones kill the drifter bs
                    _abyssalTargetPriority = 3.6;
                if (IsRemoteRepairEntity && Combat.PotentialCombatTargets.All(i => !i.IsNPCBattlecruiser))
                    _abyssalTargetPriority = 3.5;
                if (TypeName.ToLower().Contains("renewing".ToLower()) || TypeName.ToLower().Contains("priest".ToLower()) || TypeName.ToLower().Contains("plateforger".ToLower()) || TypeName.ToLower().Contains("preserver".ToLower()) || TypeName.ToLower().Contains("fieldweaver".ToLower()) || TypeName.ToLower().Contains("burst".ToLower()))
                    _abyssalTargetPriority = 3;
                if (IsNeutingEntity)
                    _abyssalTargetPriority = 2;
                if (IsAbyssalMarshal && DirectEve.Entities.Count(e => e.IsAbyssalMarshal) >= 3) // prio marshals if there is more than a few over neuts due their oneshot potential
                    _abyssalTargetPriority = 1.4;
                if (IsAbyssalBioAdaptiveCache)
                    _abyssalTargetPriority = 1.3;
                if (IsAbyssalDeadspaceTriglavianExtractionNode)
                    _abyssalTargetPriority = 1.2;
                if (TypeName.ToLower().Contains("Elite Lucifer Cynabal".ToLower()))
                    _abyssalTargetPriority = 1.11;
                if (TypeName.ToLower() == "Lucifer Cynabal".ToLower())
                    _abyssalTargetPriority = 1.10;


                AdjustAbyssalTargetPriority_Common();

                return _abyssalTargetPriority.Value;
            }
        }

        public bool IsDrifterBSSpawn => (TypeId == 56214 || TypeId == 47957 || TypeId == 47953 || TypeId == 47955 || TypeId == 47954 || TypeId == 47956);

        public bool IsBattlecruiser
        {
            get
            {
                bool result = false;
                result |= BracketType == BracketType.Battlecruiser;
                result |= GroupId == (int)Group.Battlecruiser;
                result |= GroupId == (int)Group.CommandShip;
                result |= GroupId == (int)Group.StrategicCruiser;
                return result;
            }
        }

        public bool IsBattleship
        {
            get
            {
                bool result = false;
                result |= BracketType == BracketType.Battleship;
                result |= GroupId == (int)Group.Battleship;
                result |= GroupId == (int)Group.EliteBattleship;
                result |= GroupId == (int)Group.BlackOps;
                result |= GroupId == (int)Group.Marauder;
                return result;
            }
        }

        private bool? _isBlue = null;

        public bool IsBlue
        {
            get
            {
                if (_isBlue != null)
                    return (bool)_isBlue;

                //
                // 11 is not possible, this must be an error, safest thing is to assume this is not a hostile entity
                //
                if (EffectiveStanding == 11)
                {
                    _isBlue = false;
                    return _isBlue ?? false;
                }

                if (Faction.PirateFaction)
                {
                    _isBlue = false;
                    return _isBlue ?? false;
                }

                if (EffectiveStanding > 0)
                {
                    _isBlue = true;
                    return _isBlue ?? true;
                }

                _isBlue = false;
                return _isBlue ?? false;
            }
        }

        public bool IsCelestial => CategoryId == (int)CategoryID.Celestial
                                   || CategoryId == (int)CategoryID.Station
                                   || GroupId == (int)Group.Moon
                                   || GroupId == (int)Group.AsteroidBelt;

        public bool IsContainer
        {
            get
            {
                if (GroupId == (int)Group.Wreck
                                   || GroupId == (int)Group.CargoContainer
                                   || GroupId == (int)Group.SpawnContainer
                                   || GroupId == (int)Group.MissionContainer
                                   || GroupId == (int)Group.DeadSpaceOverseersBelongings)
                {
                    return true;
                }

                if (Name.Contains("Cargo Container") && Velocity == 0 && !IsPlayer)
                {
                    return true;
                }

                return false;
            }
        }


        public bool IsCruiser
        {
            get
            {
                bool result = false;
                result |= BracketType == BracketType.Cruiser;
                result |= BracketType == BracketType.Fighter_Squadron;
                result |= GroupId == (int)Group.Cruiser;
                result |= GroupId == (int)Group.HeavyAssaultShip;
                result |= GroupId == (int)Group.AdvancedCruisers;
                result |= GroupId == (int)Group.Logistics;
                result |= GroupId == (int)Group.ForceReconShip;
                result |= GroupId == (int)Group.CombatReconShip;
                result |= GroupId == (int)Group.HeavyInterdictor;
                return result;
            }
        }

        public bool IsT2Cruiser
        {
            get
            {
                bool result = false;
                result |= GroupId == (int)Group.HeavyAssaultShip;
                result |= GroupId == (int)Group.AdvancedCruisers;
                result |= GroupId == (int)Group.Logistics;
                result |= GroupId == (int)Group.ForceReconShip;
                result |= GroupId == (int)Group.CombatReconShip;
                result |= GroupId == (int)Group.HeavyInterdictor;
                return result;
            }
        }

        public bool IsT2BattleCruiser
        {
            get
            {
                bool result = false;
                result |= GroupId == (int)Group.CommandShip;
                return result;
            }
        }

        public bool IsDread
        {
            get
            {
                bool result = false;
                result |= BracketType == BracketType.Dreadnought;
                result |= BracketType == BracketType.NPC_Dreadnought;
                result |= GroupId == (int)Group.Dreadnaught;
                return result;
            }
        }

        public bool IsMarauder
        {
            get
            {
                bool result = false;
                result |= GroupId == (int)Group.Marauder;
                return result;
            }
        }

        public bool IsEntityIShouldLeaveAlone
        {
            get
            {
                bool result = false;
                result |= GroupId == (int)Group.Merchant;
                result |= GroupId == (int)Group.Mission_Merchant;
                result |= GroupId == (int)Group.FactionWarfareNPC;
                result |= GroupId == (int)Group.Plagioclase;
                result |= GroupId == (int)Group.Spodumain;
                result |= GroupId == (int)Group.Kernite;
                result |= GroupId == (int)Group.Hedbergite;
                result |= GroupId == (int)Group.Arkonor;
                result |= GroupId == (int)Group.Bistot;
                result |= GroupId == (int)Group.Pyroxeres;
                result |= GroupId == (int)Group.Crokite;
                result |= GroupId == (int)Group.Jaspet;
                result |= GroupId == (int)Group.Omber;
                result |= GroupId == (int)Group.Scordite;
                result |= GroupId == (int)Group.Gneiss;
                result |= GroupId == (int)Group.Veldspar;
                result |= GroupId == (int)Group.Hemorphite;
                result |= GroupId == (int)Group.DarkOchre;
                result |= GroupId == (int)Group.Ice;
                result |= TypeId == (int)TypeID.TransportVessel;
                return result;
            }
        }

        public bool IsEwarImmune
        {
            get
            {
                if (TypeId == (int)TypeID.Zor)
                    return true;

                if (ESCache.Instance.InWormHoleSpace && IsNpcCapitalEscalation)
                    return true;

                if ((double)Ball.Attribute("disallowOffensiveModifiers") == 1)
                    return true;

                return false;
            }
        }

        public bool IsEwarTarget
        {
            get
            {
                var result = false;
                result |= IsWarpScramblingMe;
                result |= IsWebbingMe;
                result |= IsNeutralizingMe;
                result |= IsTryingToJamMe;
                result |= IsJammingMe;
                result |= IsSensorDampeningMe;
                result |= IsTargetPaintingMe;
                result |= IsTrackingDisruptingMe;
                result |= IsNeutingEntity;
                result |= IsSensorDampeningEntity;
                result |= IsWarpScramblingEntity;
                result |= IsWarpDisruptingEntity;
                result |= IsTrackingDisruptingEntity;
                result |= IsGuidanceDisruptingEntity;
                result |= IsJammingEntity;
                result |= IsWebbingEntity;
                result |= IsTargetPaintingEntity;
                return result;
            }
        }

        public bool IsFactionWarfareNPC => GroupId == (int)Group.FactionWarfareNPC;

        public bool IsDestroyer
        {
            get
            {
                if (TypeId == (int)TypeID.Nergal) return true;

                bool result = false;
                result |= BracketType == BracketType.Destroyer;
                result |= GroupId == (int)Group.Destroyer;
                result |= GroupId == (int)Group.Interdictor;
                return result;
            }
        }

        public bool IsFrigate
        {
            get
            {
                if (TypeId == (int)TypeID.Nergal) return true;

                bool result = false;
                result |= BracketType == BracketType.Frigate;
                result |= GroupId == (int)Group.Frigate;
                result |= GroupId == (int)Group.AssaultShip;
                result |= GroupId == (int)Group.StealthBomber;
                result |= GroupId == (int)Group.ElectronicAttackShip;
                result |= GroupId == (int)Group.PrototypeExplorationShip;
                result |= GroupId == (int)Group.Interceptor;
                return result;
            }
        }

        public bool IsLargeCollidable
        {
            get
            {
                //
                // hack to get this mission to complete and still not disable avoidbumpingthings
                //
                try
                {
                    if (DirectEve.AgentMissions.Any(i => i.Agent.IsAgentMissionAccepted && i.Name == "Cargo Delivery"))
                        if (DirectEve.AgentMissions.Find(i => i.Agent.IsAgentMissionAccepted && i.Name == "Cargo Delivery").Bookmarks.Any(bookmark => bookmark.Distance.Value < (double)Distances.OnGridWithMe))
                            if (TypeId == 17784)
                                return false;
                }
                catch (Exception)
                {
                    //ignore this exception
                }

                bool result = false;
                result |= BracketType == BracketType.Large_Collidable_Structure;
                result |= GroupId == (int)Group.LargeColidableObject;
                result |= GroupId == (int)Group.LargeColidableShip;
                result |= GroupId == (int)Group.LargeColidableStructure;
                //result |= GroupId == (int)GroupID.DeadSpaceOverseersStructure;
                //result |= GroupId == (int)GroupID.DeadSpaceOverseersBelongings;
                //result |= GroupId == (int)GroupID.AbyssalPrecursorCache;
                result |= GroupId == (int)Group.AbyssalDeadspaceWeather && Name.ToLower().Contains("Asteroid".ToLower());
                result |= GroupId == (int)Group.AbyssalDeadspaceNonScalableCloud && Name.ToLower().Contains("Asteroid".ToLower());
                return result;
            }
        }

        public bool IsDecloakedTransmissionRelay
        {
            get
            {
                if (TypeId == (int)TypeID.DecloakedTransmissionRelay)
                    return true;

                return false;
            }
        }

        public bool IsLargeWreck => !new[]
        {
            26563, 26569, 26575, 26581, 26587, 26593, 26933, 26939, 27041, 27044,
            27047, 27050, 27053, 27056, 27060, 30459
        }.All(x => x != TypeId);

        public bool IsMediumWreck => !new[]
        {
            26562, 26568, 26574, 26580, 26586, 26592, 26595, 26934, 26940, 27042,
            27045, 27048, 27051, 27054, 27057, 27061, 34440
        }.All(x => x != TypeId);

        public bool IsMiscJunk
        {
            get
            {
                bool result = false;
                result |= GroupId == (int)Group.PlayerDrone;
                result |= GroupId == (int)Group.Wreck;
                result |= IsAccelerationGate;
                result |= GroupId == (int)Group.GasCloud;
                return result;
            }
        }

        private const int HostileResponseThreshold = -4;

        private bool? _isHostile = null;

        public bool IsHostile
        {
            get
            {
                if (_isHostile != null)
                    return (bool)_isHostile;

                //
                // 11 is not possible, this must be an error, safest thing is to assume this is not a hostile entity
                //
                if (EffectiveStanding == 11)
                {
                    _isHostile = false;
                    return _isHostile ?? false;
                }

                //this is not 100% always true! may need to verify this against some kind of list is doing 0.0 missions?!
                //if (MissionSettings.ShootPirateFactions && Faction.PirateFaction)
                if (Faction.PirateFaction)
                {
                    _isHostile = true;
                    return _isHostile ?? true;
                }

                //if (MissionSettings.ShootEmpireFactions && Faction.EmpireFaction)
                //{
                //    _isHostile = true;
                //    return _isHostile ?? true;
                //}

                if (EffectiveStanding > HostileResponseThreshold)
                {
                    _isHostile = false;
                    return _isHostile ?? false;
                }

                _isHostile = true;
                return _isHostile ?? true;
            }
        }

        private float? _effectiveStanding = null;

        public float EffectiveStanding
        {
            get
            {
                try
                {
                    if (_effectiveStanding != null)
                        return (float)_effectiveStanding;

                    if (Faction.Id == -1)
                        return 0;

                    _effectiveStanding = DirectEve.Standings.EffectiveStanding(Faction.Id, (long)DirectEve.Session.CharacterId);
                    return (float)_effectiveStanding;
                }
                catch (Exception ex)
                {
                    Logging.Log.WriteLine("Exception [" + ex + "]");
                    return 10;
                }
            }
        }

        public bool IsNPCBattlecruiser
        {
            get
            {
                bool result = false;

                if (DirectEve.Session.IsAbyssalDeadspace)
                    result |= BracketType == BracketType.NPC_Battlecruiser;

                if (GroupId == (int)Group.InvadingPrecursorEntities)
                {
                    if (BracketType == BracketType.NPC_Battlecruiser)
                        return true;
                }

                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Guristas_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Serpentis_BattleCruiser;
                result |= GroupId == (int)Group.Deadspace_Angel_Cartel_BattleCruiser;
                result |= GroupId == (int)Group.Deadspace_Blood_Raiders_BattleCruiser;
                result |= GroupId == (int)Group.Deadspace_Guristas_BattleCruiser;
                result |= GroupId == (int)Group.Deadspace_Sanshas_Nation_BattleCruiser;
                result |= GroupId == (int)Group.Deadspace_Serpentis_BattleCruiser;
                result |= GroupId == (int)Group.Mission_Amarr_Empire_Battlecruiser;
                result |= GroupId == (int)Group.Mission_Caldari_State_Battlecruiser;
                result |= GroupId == (int)Group.Mission_Gallente_Federation_Battlecruiser;
                result |= GroupId == (int)Group.Mission_Minmatar_Republic_Battlecruiser;
                result |= GroupId == (int)Group.Mission_Khanid_Battlecruiser;
                result |= GroupId == (int)Group.Mission_CONCORD_Battlecruiser;
                result |= GroupId == (int)Group.Mission_Mordu_Battlecruiser;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_Commander_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_Commander_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Guristas_Commander_BattleCruiser;
                result |= GroupId == (int)Group.Deadspace_Rogue_Drone_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_Commander_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Serpentis_Commander_BattleCruiser;
                result |= GroupId == (int)Group.Mission_Thukker_Battlecruiser;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_Commander_BattleCruiser;
                return result;
            }
        }

        public bool IsNpcCapitalEscalation
        {
            get
            {
                if (TypeId == (int)TypeID.UpgradedAvenger)
                    return true;

                return false;
            }
        }

        public bool IsNPCWormHoleSpaceDrifter
        {
            get
            {
                //if (GroupId == (int)Group.DrifterResponseBattleship)
                //    return true;

                //if (TypeId == (int)TypeID.DrifterBattleshipWSpace)
                //    return true;

                if (ESCache.Instance.InWormHoleSpace && Name == "Arithmos Tyrannos")
                    return true;

                return false;
            }
        }

        public bool IsNPCCapitalShip
        {
            get
            {
                if (BracketType == BracketType.NPC_Super_Carrier)
                    return true;

                if (BracketType == BracketType.NPC_Carrier)
                    return true;

                if (BracketType == BracketType.NPC_Dreadnought)
                    return true;

                if (Name.ToLower().Contains("Revelation".ToLower()))
                    return true;

                if (Name.ToLower().Contains("Moros".ToLower()))
                    return true;

                if (Name.ToLower().Contains("Naglafar".ToLower()))
                    return true;

                if (Name.ToLower().Contains("Phoenix".ToLower()))
                    return true;

                return false;
            }
        }

        public bool IsNPCBattleship
        {
            get
            {
                bool result = false;

                if (DirectEve.Session.IsWspace)
                {
                    if (TypeId == (int)TypeID.SleeplessGuardian) //Sleepless Guardian
                        return true;

                    if (TypeId == (int)TypeID.SleeplessKeeper) //Sleepless Keeper
                        return true;

                    if (TypeId == (int)TypeID.SleeplessSentinel) //Sleepless Sentinel
                        return true;

                    if (TypeId == (int)TypeID.SleeplessWarden) //Sleepless Warden
                        return true;
                }

                if (DirectEve.Session.IsAbyssalDeadspace)
                {
                    result |= BracketType == BracketType.NPC_Battleship;
                    result |= BracketType == BracketType.NPC_Carrier;
                    result |= BracketType == BracketType.NPC_Freighter;
                    result |= BracketType == BracketType.NPC_Super_Carrier;
                    result |= TypeId == (int)TypeID.Karybdis_Tyrannos;
                }

                result |= GroupId == (int)Group.Storyline_Battleship;
                result |= GroupId == (int)Group.Storyline_Mission_Battleship;
                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_Battleship;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_Battleship;
                result |= GroupId == (int)Group.Asteroid_Guristas_Battleship;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_Battleship;
                result |= GroupId == (int)Group.Asteroid_Serpentis_Battleship;
                result |= GroupId == (int)Group.Deadspace_Angel_Cartel_Battleship;
                result |= GroupId == (int)Group.Deadspace_Blood_Raiders_Battleship;
                result |= GroupId == (int)Group.Deadspace_Guristas_Battleship;
                result |= GroupId == (int)Group.Deadspace_Sanshas_Nation_Battleship;
                result |= GroupId == (int)Group.Deadspace_Serpentis_Battleship;
                result |= GroupId == (int)Group.Mission_Amarr_Empire_Battleship;
                result |= GroupId == (int)Group.Mission_Caldari_State_Battleship;
                result |= GroupId == (int)Group.Mission_Gallente_Federation_Battleship;
                result |= GroupId == (int)Group.Mission_Khanid_Battleship;
                result |= GroupId == (int)Group.Mission_CONCORD_Battleship;
                result |= GroupId == (int)Group.Mission_Mordu_Battleship;
                result |= GroupId == (int)Group.Mission_Minmatar_Republic_Battleship;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_Battleship;
                result |= GroupId == (int)Group.Deadspace_Rogue_Drone_Battleship;
                result |= GroupId == (int)Group.Mission_Generic_Battleships;
                result |= GroupId == (int)Group.Deadspace_Overseer_Battleship;
                result |= GroupId == (int)Group.Mission_Thukker_Battleship;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_Commander_Battleship;
                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_Commander_Battleship;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_Commander_Battleship;
                result |= GroupId == (int)Group.Asteroid_Guristas_Commander_Battleship;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_Commander_Battleship;
                result |= GroupId == (int)Group.Asteroid_Serpentis_Commander_Battleship;
                result |= GroupId == (int)Group.Mission_Faction_Battleship;
                if (DirectEve.Session.IsAbyssalDeadspace || DirectEve.Session.IsWspace)
                {
                    result |= GroupId == (int)Group.Drifters_Battleship;
                    result |= GroupId == (int)Group.DrifterResponseBattleship;
                }

                return result;
            }
        }

        public bool IsNPCByBracketType =>
            BracketType == BracketType.NPC_Frigate
            || BracketType == BracketType.NPC_Destroyer
            || BracketType == BracketType.NPC_Cruiser
            || BracketType == BracketType.NPC_Drone
            || BracketType == BracketType.NPC_Rookie_Ship
            || BracketType == BracketType.NPC_Drone_EW
            || BracketType == BracketType.NPC_Battlecruiser
            || BracketType == BracketType.NPC_Battleship;
            //|| BracketType == BracketType.NPC_Carrier
            //|| BracketType == BracketType.NPC_Dreadnought
            //|| BracketType == BracketType.NPC_Titan
            //|| BracketType == BracketType.NPC_Super_Carrier;

        public bool IsOfficerOrOverseer
        {
            get
            {
                bool result = false;
                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_Officer;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_Officer;
                result |= GroupId == (int)Group.Asteroid_Guristas_Officer;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_Officer;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_Officer;
                result |= GroupId == (int)Group.Asteroid_Serpentis_Officer;
                result |= GroupId == (int)Group.Deadspace_Overseer_Battleship;
                result |= GroupId == (int)Group.Deadspace_Overseer_Cruiser;
                result |= GroupId == (int)Group.Deadspace_Overseer_Frigate;
                result |= GroupId == (int)Group.DeadspaceOverseersSentry;
                return result;
            }
        }

        public bool IsNpcByGroupID
        {
            get
            {
                bool result = false;
                result |= IsLargeCollidable;
                result |= IsSentry;

                if (DirectEve.Session.IsAbyssalDeadspace)
                    if (IsNPCBattleship || IsNPCBattlecruiser || IsNPCCruiser || IsNPCFrigate)
                        return true;

                if (GroupId == (int)Group.InvadingPrecursorEntities)
                {
                    return true;
                }

                result |= GroupId == (int)Group.DeadSpaceOverseersStructure;
                result |= GroupId == (int)Group.Storyline_Battleship;
                result |= GroupId == (int)Group.Storyline_Mission_Battleship;
                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_Battleship;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_Battleship;
                result |= GroupId == (int)Group.Asteroid_Guristas_Battleship;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_Battleship;
                result |= GroupId == (int)Group.Asteroid_Serpentis_Battleship;
                result |= GroupId == (int)Group.Deadspace_Angel_Cartel_Battleship;
                result |= GroupId == (int)Group.Deadspace_Blood_Raiders_Battleship;
                result |= GroupId == (int)Group.Deadspace_Guristas_Battleship;
                result |= GroupId == (int)Group.Deadspace_Sanshas_Nation_Battleship;
                result |= GroupId == (int)Group.Deadspace_Serpentis_Battleship;
                result |= GroupId == (int)Group.Mission_Amarr_Empire_Battleship;
                result |= GroupId == (int)Group.Mission_Caldari_State_Battleship;
                result |= GroupId == (int)Group.Mission_Gallente_Federation_Battleship;
                result |= GroupId == (int)Group.Mission_Khanid_Battleship;
                result |= GroupId == (int)Group.Mission_CONCORD_Battleship;
                result |= GroupId == (int)Group.Mission_Mordu_Battleship;
                result |= GroupId == (int)Group.Mission_Minmatar_Republic_Battleship;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_Battleship;
                result |= GroupId == (int)Group.Deadspace_Rogue_Drone_Battleship;
                result |= GroupId == (int)Group.Mission_Generic_Battleships;
                result |= GroupId == (int)Group.Deadspace_Overseer_Battleship;
                result |= GroupId == (int)Group.Mission_Thukker_Battleship;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_Commander_Battleship;
                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_Commander_Battleship;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_Commander_Battleship;
                result |= GroupId == (int)Group.Asteroid_Guristas_Commander_Battleship;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_Battleship;
                result |= GroupId == (int)Group.Asteroid_Serpentis_Commander_Battleship;
                result |= GroupId == (int)Group.Mission_Faction_Battleship;
                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Guristas_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Serpentis_BattleCruiser;
                result |= GroupId == (int)Group.Deadspace_Angel_Cartel_BattleCruiser;
                result |= GroupId == (int)Group.Deadspace_Blood_Raiders_BattleCruiser;
                result |= GroupId == (int)Group.Deadspace_Guristas_BattleCruiser;
                result |= GroupId == (int)Group.Deadspace_Sanshas_Nation_BattleCruiser;
                result |= GroupId == (int)Group.Deadspace_Serpentis_BattleCruiser;
                result |= GroupId == (int)Group.Mission_Amarr_Empire_Battlecruiser;
                result |= GroupId == (int)Group.Mission_Caldari_State_Battlecruiser;
                result |= GroupId == (int)Group.Mission_Gallente_Federation_Battlecruiser;
                result |= GroupId == (int)Group.Mission_Minmatar_Republic_Battlecruiser;
                result |= GroupId == (int)Group.Mission_Khanid_Battlecruiser;
                result |= GroupId == (int)Group.Mission_CONCORD_Battlecruiser;
                result |= GroupId == (int)Group.Mission_Mordu_Battlecruiser;
                result |= GroupId == (int)Group.Mission_Faction_Industrials;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_Commander_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_Commander_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Guristas_Commander_BattleCruiser;
                result |= GroupId == (int)Group.Deadspace_Rogue_Drone_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_Commander_BattleCruiser;
                result |= GroupId == (int)Group.Asteroid_Serpentis_Commander_BattleCruiser;
                result |= GroupId == (int)Group.Mission_Thukker_Battlecruiser;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_Commander_BattleCruiser;
                result |= GroupId == (int)Group.Storyline_Cruiser;
                result |= GroupId == (int)Group.Storyline_Mission_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Guristas_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Serpentis_Cruiser;
                result |= GroupId == (int)Group.Deadspace_Angel_Cartel_Cruiser;
                result |= GroupId == (int)Group.Deadspace_Blood_Raiders_Cruiser;
                result |= GroupId == (int)Group.Deadspace_Guristas_Cruiser;
                result |= GroupId == (int)Group.Deadspace_Sanshas_Nation_Cruiser;
                result |= GroupId == (int)Group.Deadspace_Serpentis_Cruiser;
                result |= GroupId == (int)Group.Mission_Amarr_Empire_Cruiser;
                result |= GroupId == (int)Group.Mission_Caldari_State_Cruiser;
                result |= GroupId == (int)Group.Mission_Gallente_Federation_Cruiser;
                result |= GroupId == (int)Group.Mission_Khanid_Cruiser;
                result |= GroupId == (int)Group.Mission_CONCORD_Cruiser;
                result |= GroupId == (int)Group.Mission_Mordu_Cruiser;
                result |= GroupId == (int)Group.Mission_Minmatar_Republic_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_Commander_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_Commander_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Guristas_Commander_Cruiser;
                result |= GroupId == (int)Group.Deadspace_Rogue_Drone_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_Commander_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Serpentis_Commander_Cruiser;
                result |= GroupId == (int)Group.Mission_Generic_Cruisers;
                result |= GroupId == (int)Group.Deadspace_Overseer_Cruiser;
                result |= GroupId == (int)Group.Mission_Thukker_Cruiser;
                result |= GroupId == (int)Group.Mission_Generic_Battle_Cruisers;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_Commander_Cruiser;
                result |= GroupId == (int)Group.Mission_Faction_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Guristas_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Serpentis_Destroyer;
                result |= GroupId == (int)Group.Deadspace_Angel_Cartel_Destroyer;
                result |= GroupId == (int)Group.Deadspace_Blood_Raiders_Destroyer;
                result |= GroupId == (int)Group.Deadspace_Guristas_Destroyer;
                result |= GroupId == (int)Group.Deadspace_Sanshas_Nation_Destroyer;
                result |= GroupId == (int)Group.Deadspace_Serpentis_Destroyer;
                result |= GroupId == (int)Group.Mission_Amarr_Empire_Destroyer;
                result |= GroupId == (int)Group.Mission_Caldari_State_Destroyer;
                result |= GroupId == (int)Group.Mission_Gallente_Federation_Destroyer;
                result |= GroupId == (int)Group.Mission_Minmatar_Republic_Destroyer;
                result |= GroupId == (int)Group.Mission_Khanid_Destroyer;
                result |= GroupId == (int)Group.Mission_CONCORD_Destroyer;
                result |= GroupId == (int)Group.Mission_Mordu_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_Commander_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_Commander_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Guristas_Commander_Destroyer;
                result |= GroupId == (int)Group.Deadspace_Rogue_Drone_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_Commander_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Serpentis_Commander_Destroyer;
                result |= GroupId == (int)Group.Mission_Thukker_Destroyer;
                result |= GroupId == (int)Group.Mission_Generic_Destroyers;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_Commander_Destroyer;
                result |= GroupId == (int)Group.TutorialDrone;
                result |= GroupId == (int)Group.asteroid_angel_cartel_frigate;
                result |= GroupId == (int)Group.asteroid_blood_raiders_frigate;
                result |= GroupId == (int)Group.asteroid_guristas_frigate;
                result |= GroupId == (int)Group.asteroid_sanshas_nation_frigate;
                result |= GroupId == (int)Group.asteroid_serpentis_frigate;
                result |= GroupId == (int)Group.deadspace_angel_cartel_frigate;
                result |= GroupId == (int)Group.deadspace_blood_raiders_frigate;
                result |= GroupId == (int)Group.deadspace_guristas_frigate;
                result |= GroupId == (int)Group.Deadspace_Overseer_Frigate;
                result |= GroupId == (int)Group.Deadspace_Rogue_Drone_Swarm;
                result |= GroupId == (int)Group.deadspace_sanshas_nation_frigate;
                result |= GroupId == (int)Group.deadspace_serpentis_frigate;
                result |= GroupId == (int)Group.mission_amarr_empire_frigate;
                result |= GroupId == (int)Group.mission_caldari_state_frigate;
                result |= GroupId == (int)Group.mission_gallente_federation_frigate;
                result |= GroupId == (int)Group.mission_minmatar_republic_frigate;
                result |= GroupId == (int)Group.mission_khanid_frigate;
                result |= GroupId == (int)Group.mission_concord_frigate;
                result |= GroupId == (int)Group.mission_mordu_frigate;
                result |= GroupId == (int)Group.asteroid_rouge_drone_frigate;
                result |= GroupId == (int)Group.deadspace_rogue_drone_frigate;
                result |= GroupId == (int)Group.asteroid_angel_cartel_commander_frigate;
                result |= GroupId == (int)Group.asteroid_blood_raiders_commander_frigate;
                result |= GroupId == (int)Group.asteroid_guristas_commander_frigate;
                result |= GroupId == (int)Group.asteroid_sanshas_nation_commander_frigate;
                result |= GroupId == (int)Group.asteroid_serpentis_commander_frigate;
                result |= GroupId == (int)Group.mission_generic_frigates;
                result |= GroupId == (int)Group.mission_thukker_frigate;
                result |= GroupId == (int)Group.asteroid_rouge_drone_commander_frigate;
                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_Officer;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_Officer;
                result |= GroupId == (int)Group.Asteroid_Guristas_Officer;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_Officer;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_Officer;
                result |= GroupId == (int)Group.Asteroid_Serpentis_Officer;
                result |= GroupId == (int)Group.Deadspace_Sleeper_Awakened_Defender;
                result |= GroupId == (int)Group.Deadspace_Sleeper_Awakened_Patroller;
                result |= GroupId == (int)Group.Deadspace_Sleeper_Awakened_Sentinel;
                result |= GroupId == (int)Group.Deadspace_Sleeper_Emergent_Defender;
                result |= GroupId == (int)Group.Deadspace_Sleeper_Emergent_Patroller;
                result |= GroupId == (int)Group.Deadspace_Sleeper_Emergent_Sentinel;
                result |= GroupId == (int)Group.Deadspace_Sleeper_Sleepless_Defender;
                result |= GroupId == (int)Group.Deadspace_Sleeper_Sleepless_Patroller;
                result |= GroupId == (int)Group.Deadspace_Sleeper_Sleepless_Sentinel;
                result |= GroupId == (int)Group.AbyssalSpaceshipEntities;
                result |= GroupId == (int)Group.AbyssalDeadspaceDroneEntities;
                result |= GroupId == (int)Group.InvadingPrecursorEntities;
                result |= GroupId == (int)Group.Pirate_Capsule;

                if (DirectEve.Session.IsAbyssalDeadspace || DirectEve.Session.IsWspace)
                {
                    result |= GroupId == (int)Group.AbyssalFlagCruiser;
                    result |= GroupId == (int)Group.Drifters_Battleship;
                    result |= GroupId == (int)Group.DrifterResponseBattleship;
                }

                return result;
            }
        }

        public bool IsNPCCruiser
        {
            get
            {
                bool result = false;

                if (BracketType == BracketType.NPC_Cruiser)
                    return true;

                if (DirectEve.Session.IsWspace)
                {
                    if (Name.Contains("Awakened Guardian"))
                        return true;

                    if (Name.Contains("Awakened Keeper"))
                        return true;

                    if (Name.Contains("Awakened Sentinel"))
                        return true;

                    if (Name.Contains("Awakened Warden"))
                        return true;
                }

                if (DirectEve.Session.IsAbyssalDeadspace)
                {
                    result |= BracketType == BracketType.NPC_Industrial;
                    result |= BracketType == BracketType.NPC_Mining_Barge;
                    result |= GroupId == (int)Group.AbyssalFlagCruiser;
                    result |= Name.Contains("Ephialtes");
                    result |= Name.Contains("Vedmak");
                }

                result |= GroupId == (int)Group.Storyline_Cruiser;
                result |= GroupId == (int)Group.Storyline_Mission_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Guristas_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Serpentis_Cruiser;
                result |= GroupId == (int)Group.Deadspace_Angel_Cartel_Cruiser;
                result |= GroupId == (int)Group.Deadspace_Blood_Raiders_Cruiser;
                result |= GroupId == (int)Group.Deadspace_Guristas_Cruiser;
                result |= GroupId == (int)Group.Deadspace_Sanshas_Nation_Cruiser;
                result |= GroupId == (int)Group.Deadspace_Serpentis_Cruiser;
                result |= GroupId == (int)Group.Mission_Amarr_Empire_Cruiser;
                result |= GroupId == (int)Group.Mission_Caldari_State_Cruiser;
                result |= GroupId == (int)Group.Mission_Gallente_Federation_Cruiser;
                result |= GroupId == (int)Group.Mission_Khanid_Cruiser;
                result |= GroupId == (int)Group.Mission_CONCORD_Cruiser;
                result |= GroupId == (int)Group.Mission_Mordu_Cruiser;
                result |= GroupId == (int)Group.Mission_Minmatar_Republic_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_Commander_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_Commander_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Guristas_Commander_Cruiser;
                result |= GroupId == (int)Group.Deadspace_Rogue_Drone_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_Commander_Cruiser;
                result |= GroupId == (int)Group.Asteroid_Serpentis_Commander_Cruiser;
                result |= GroupId == (int)Group.Mission_Generic_Cruisers;
                result |= GroupId == (int)Group.Deadspace_Overseer_Cruiser;
                result |= GroupId == (int)Group.Mission_Thukker_Cruiser;
                result |= GroupId == (int)Group.Mission_Generic_Battle_Cruisers;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_Commander_Cruiser;
                result |= GroupId == (int)Group.Mission_Faction_Cruiser;
                result |= GroupId == (int)Group.Mission_Faction_Industrials;
                if (DirectEve.Session.IsAbyssalDeadspace || DirectEve.Session.IsWspace)
                    result |= GroupId == (int)Group.AbyssalFlagCruiser;
                return result;
            }
        }

        public bool IsNPCDestroyer
        {
            get
            {
                if (BracketType == BracketType.NPC_Destroyer)
                    return true;

                return false;
            }
        }

        public bool IsNPCDrone
        {
            get
            {
                bool result = false;
                if (IsPlayer)
                    return false;

                if (DirectEve.Session.IsAbyssalDeadspace)
                {
                    result |= BracketType == BracketType.NPC_Drone;
                    result |= BracketType == BracketType.NPC_Drone_EW;
                    result |= BracketType == BracketType.NPC_Fighter;
                    result |= BracketType == BracketType.NPC_Fighter_Bomber;
                }

                return result;
            }
        }

        public bool IsNPCFrigate
        {
            get
            {
                bool result = false;

                if (BracketType == BracketType.NPC_Frigate)
                        return true;

                if (DirectEve.Session.IsWspace)
                {
                    if (Name.Contains("Emergent Escort"))
                        return true;

                    if (Name.Contains("Emergent Keeper"))
                        return true;

                    if (Name.Contains("Emergent Sentinel"))
                        return true;

                    if (Name.Contains("Emergent  Warden"))
                        return true;
                }

                if (DirectEve.Session.IsAbyssalDeadspace)
                {
                    //see: https://suitonia.wordpress.com/2018/05/28/the-demons-which-lurk-in-the-abyss/
                    result |= BracketType == BracketType.NPC_Frigate;
                    result |= BracketType == BracketType.NPC_Shuttle;
                    result |= BracketType == BracketType.NPC_Rookie_Ship;
                    result |= Name.Contains("Fieldweaver");
                    result |= Name.Contains("Plateweaver");
                    result |= Name.Contains("Snarecaster");
                    result |= Name.Contains("Fogcaster");
                    result |= Name.Contains("Gazedimmer");
                    result |= Name.Contains("Spotlighter");
                    result |= Name.Contains("Damavik");
                    return result;
                }

                result |= GroupId == (int)Group.DeepFlowFrigateDrone;
                result |= GroupId == (int)Group.DeepFlowDestroyerDrone;
                result |= GroupId == (int)Group.Frigate;
                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Guristas_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Serpentis_Destroyer;
                result |= GroupId == (int)Group.Deadspace_Angel_Cartel_Destroyer;
                result |= GroupId == (int)Group.Deadspace_Blood_Raiders_Destroyer;
                result |= GroupId == (int)Group.Deadspace_Guristas_Destroyer;
                result |= GroupId == (int)Group.Deadspace_Sanshas_Nation_Destroyer;
                result |= GroupId == (int)Group.Deadspace_Serpentis_Destroyer;
                result |= GroupId == (int)Group.Mission_Amarr_Empire_Destroyer;
                result |= GroupId == (int)Group.Mission_Caldari_State_Destroyer;
                result |= GroupId == (int)Group.Mission_Gallente_Federation_Destroyer;
                result |= GroupId == (int)Group.Mission_Minmatar_Republic_Destroyer;
                result |= GroupId == (int)Group.Mission_Khanid_Destroyer;
                result |= GroupId == (int)Group.Mission_CONCORD_Destroyer;
                result |= GroupId == (int)Group.Mission_Mordu_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Angel_Cartel_Commander_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Blood_Raiders_Commander_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Guristas_Commander_Destroyer;
                result |= GroupId == (int)Group.Deadspace_Rogue_Drone_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Sanshas_Nation_Commander_Destroyer;
                result |= GroupId == (int)Group.Asteroid_Serpentis_Commander_Destroyer;
                result |= GroupId == (int)Group.Mission_Thukker_Destroyer;
                result |= GroupId == (int)Group.Mission_Generic_Destroyers;
                result |= GroupId == (int)Group.Asteroid_Rogue_Drone_Commander_Destroyer;
                result |= GroupId == (int)Group.asteroid_angel_cartel_frigate;
                result |= GroupId == (int)Group.asteroid_blood_raiders_frigate;
                result |= GroupId == (int)Group.asteroid_guristas_frigate;
                result |= GroupId == (int)Group.asteroid_sanshas_nation_frigate;
                result |= GroupId == (int)Group.asteroid_serpentis_frigate;
                result |= GroupId == (int)Group.deadspace_angel_cartel_frigate;
                result |= GroupId == (int)Group.deadspace_blood_raiders_frigate;
                result |= GroupId == (int)Group.deadspace_guristas_frigate;
                result |= GroupId == (int)Group.deadspace_sanshas_nation_frigate;
                result |= GroupId == (int)Group.deadspace_serpentis_frigate;
                result |= GroupId == (int)Group.mission_amarr_empire_frigate;
                result |= GroupId == (int)Group.mission_caldari_state_frigate;
                result |= GroupId == (int)Group.mission_gallente_federation_frigate;
                result |= GroupId == (int)Group.mission_minmatar_republic_frigate;
                result |= GroupId == (int)Group.mission_khanid_frigate;
                result |= GroupId == (int)Group.mission_concord_frigate;
                result |= GroupId == (int)Group.mission_mordu_frigate;
                result |= GroupId == (int)Group.asteroid_rouge_drone_frigate;
                result |= GroupId == (int)Group.deadspace_rogue_drone_frigate;
                result |= GroupId == (int)Group.asteroid_angel_cartel_commander_frigate;
                result |= GroupId == (int)Group.asteroid_blood_raiders_commander_frigate;
                result |= GroupId == (int)Group.asteroid_guristas_commander_frigate;
                result |= GroupId == (int)Group.asteroid_sanshas_nation_commander_frigate;
                result |= GroupId == (int)Group.asteroid_serpentis_commander_frigate;
                result |= GroupId == (int)Group.mission_generic_frigates;
                result |= GroupId == (int)Group.mission_thukker_frigate;
                result |= GroupId == (int)Group.asteroid_rouge_drone_commander_frigate;
                result |= GroupId == (int)Group.TutorialDrone;
                return result;
            }
        }

        public bool IsPod
        {
            get
            {
                bool result = false;
                result |= BracketType == BracketType.Capsule;
                result |= GroupId == (int)Group.Capsule;
                if (result)
                    if (DirectEve.Interval(30000, 30000, result.ToString())) ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.ShipType), ESCache.Instance.MyShipEntity.TypeName);

                return result;
            }
        }

        public bool IsShuttle
        {
            get
            {
                bool result = false;
                result |= BracketType == BracketType.Shuttle;
                result |= GroupId == (int)Group.Shuttle;
                return result;
            }
        }

        public bool IsSentry
        {
            get
            {
                if (DirectEve.Session.IsWspace && Name.Contains("Orthrus"))
                        return true;

                bool result = false;
                result |= BracketType == BracketType.Sentry_Gun;
                result |= GroupId == (int)Group.ProtectiveSentryGun;
                result |= GroupId == (int)Group.MobileSentryGun;
                result |= GroupId == (int)Group.DestructibleSentryGun;
                result |= GroupId == (int)Group.MobileMissileSentry;
                result |= GroupId == (int)Group.MobileProjectileSentry;
                result |= GroupId == (int)Group.MobileLaserSentry;
                result |= GroupId == (int)Group.MobileHybridSentry;
                result |= GroupId == (int)Group.DeadspaceOverseersSentry;
                result |= GroupId == (int)Group.StasisWebificationBattery;
                result |= GroupId == (int)Group.EnergyNeutralizingBattery;
                return result;
            }
        }

        public bool IsSmallWreck => !new[]
        {
            26561, 26567, 26573, 26579, 26585, 26591, 26594, 26935,
            26941, 27043, 27046, 27049, 27052, 27055, 27058, 27062
        }.All(x => x != TypeId);

        #endregion Properties
    }
}