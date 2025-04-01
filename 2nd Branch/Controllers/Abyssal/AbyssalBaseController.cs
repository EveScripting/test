//
// (c) duketwo 2022
//

extern alias SC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Combat;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.Extensions;
using SharpDX.Direct2D1;

namespace EVESharpCore.Controllers.Abyssal
{
    public enum AbyssalState
    {
        Start,
        BuyItems,
        PrepareToArm,
        WaitingOnFleetMembers,
        LoadSavedFitting,
        Arm,
        IdleInStation,
        TravelToFilamentSpot,
        TravelToBuyLocation,
        TravelToHomeLocation,
        ReplaceShip,
        ActivateShip,
        ActivateAbyssalDeadspace,
        UseFilament,
        DumpSurveyDatabases,
        TravelToRepairLocation,
        RepairItems,
        AbyssalEnter,
        AbyssalClear,
        UnloadLoot,
        InvulnPhaseAfterAbyssExit,
        PVP,
        TrashItems,
        Error,
        OutOfDrones,
        OutOfAmmo,
        OutOfBoosters,
        OutOfNaniteRepairPaste,
        OutOfFilaments,
    }

    public enum MarketGroup
    {
        LightScoutDrone = 837,
        MediumScoutDrone = 838,
        HeavyAttackDrone = 839
    }

    public enum AbyssalStage
    {
        Stage1 = 1,
        Stage2 = 2,
        Stage3 = 3,
    }

    public abstract class AbyssalBaseController : BaseController
    {

        internal AbyssalState _prevState;
        internal AbyssalState _state;
        public AbyssalState myAbyssalState
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

        internal bool IsAnyPlayerAttacking => DirectEve.Entities.Any(e => e.IsAttacking && e.IsPlayer);

        internal bool AreWeResumingFromACrash;

        public void RecordDetectSpawnInfoForCurrentAbyssalStage()
        {
            if (AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.Undecided)
            {
                switch (CurrentAbyssalStage)
                {
                    case AbyssalStage.Stage1:
                        if (string.IsNullOrEmpty(_stage1DetectSpawn))
                        {
                            _stage1DetectSpawn = AbyssalSpawn.DetectSpawn.ToString();
                            Log("_stage1DetectSpawn [" + _stage1DetectSpawn + "]");
                        }
                        break;
                    case AbyssalStage.Stage2:
                        if (string.IsNullOrEmpty(_stage2DetectSpawn))
                        {
                            _stage2DetectSpawn = AbyssalSpawn.DetectSpawn.ToString();
                            Log("_stage2DetectSpawn [" + _stage2DetectSpawn + "]");
                        }
                        break;
                    case AbyssalStage.Stage3:
                        if (string.IsNullOrEmpty(_stage3DetectSpawn))
                        {
                            _stage3DetectSpawn = AbyssalSpawn.DetectSpawn.ToString();
                            Log("_stage3DetectSpawn [" + _stage3DetectSpawn + "]");
                        }

                        break;
                }
            }
        }

        internal AbyssalStage CurrentAbyssalStage
        {
            get
            {
                try
                {
                    if (_endGate != null)
                    {
                        return AbyssalStage.Stage3;
                    }
                    else if (_attemptsToJumpMidgate > 0)
                    {
                        return AbyssalStage.Stage2;
                    }

                    return AbyssalStage.Stage1;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return AbyssalStage.Stage1;
                }
            }
        }

        internal float CurrentStageRemainingSeconds => CurrentAbyssalStage == AbyssalStage.Stage1 ?
            _abyssRemainingSeconds - ((20 * 60 / 3) * 2) : CurrentAbyssalStage == AbyssalStage.Stage2 ?
            _abyssRemainingSeconds - ((20 * 60 / 3) * 1) : _abyssRemainingSeconds;

        internal double CurrentStageRemainingSecondsWithoutPreviousStages
        {
            get
            {
                var minus = CurrentAbyssalStage == AbyssalStage.Stage1 ? 800d : CurrentAbyssalStage == AbyssalStage.Stage2 ? 400d : 0d;
                var a = _abyssRemainingSeconds - minus;
                var b = 400 - Time.Instance.SecondsSinceLastSessionChange;
                var ret = Math.Min(a, b);
                return ret;
            }
        }

        internal double GetCurrentStageStageSeconds
        {
            get
            {
                var prevWithout = CurrentStageRemainingSecondsWithoutPreviousStages;
                if (prevWithout > 0)
                    prevWithout = 400 - prevWithout;
                else
                    prevWithout = 400 + prevWithout * -1;

                return Math.Min(Time.Instance.SecondsSinceLastSessionChange, prevWithout);
            }
        }

        public static void ClearPerPocketCache()
        {
            AbyssalSpawn.ClearPerPocketCache();
            return;
        }

        // are we in a single room abyssal? in those you can just activate the last gate, it is not locked
        // if we crash in the third room, after relog, this would evaluate to true. fixed with: && IsAbyssGateOpen
        // if we crash when no enemies are left, still return true -> are we scooping the mtu?

        // IsAbyssGateOpen and enemies on grid is also an indicator! (until enemies are gone)
        internal bool _singleRoomAbyssal
        {
            get
            {
                if (DebugConfig.DebugPretendEverySpawnIsTheFourteenBattleshipSpawn)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)
                    return true;

                if (_endGate == null)
                    return false;

                if (_attemptsToJumpMidgate > 0)
                    return false;

                if (!IsAbyssGateOpen)
                    return false;

                return true;
            }
        }

        public DirectEve DirectEve => ESCache.Instance.DirectEve;


        public bool IsSpotWithinAbyssalBounds(DirectWorldPosition p, long offset = 0)
        {
            try
            {
                if (!ESCache.Instance.DirectEve.Me.IsInAbyssalSpace())
                    return false;

                if (offset == 0)
                {
                    if (ESCache.Instance.AbyssalCenter != null)
                    {
                        if (ESCache.Instance.AbyssalCenter._directEntity != null)
                        {
                            if (ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition != null)
                            {
                                return ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.GetDistanceSquared(p) <= DirectEntity.AbyssBoundarySizeSquared;
                            }
                            else Log("if (ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition == null)");
                        }
                        else Log("if (ESCache.Instance.AbyssalCenter._directEntity == null)");
                    }
                    else Log("if (ESCache.Instance.AbyssalCenter == null)");
                }

                if (ESCache.Instance.AbyssalCenter != null)
                {
                    return ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.GetDistanceSquared(p) <= (DirectEntity.AbyssBoundarySize + offset) * (DirectEntity.AbyssBoundarySize + offset);
                }
                else Log("if (ESCache.Instance.AbyssalCenter == null)!!");

                Log("returning true?!?)");
                return false;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return true;
            }
        }

        //public ClientSetting ClientSetting => ESCache.Instance.EveAccount.ClientSetting;

        //internal int _homeStationId => Settings.Instance.HomeStationId;


        public double _stage1SecondsSpent { get; set; }
        public double _stage2SecondsSpent;
        public double _stage3SecondsSpent;
        public string _stage1DetectSpawn { get; set; }
        public string _stage2DetectSpawn { get; set; }
        public string _stage3DetectSpawn;
        public static DateTime stage1TimeLastNPCWasKilled { get; set; }
        public static DateTime stage2TimeLastNPCWasKilled { get; set; }
        public static DateTime stage3TimeLastNPCWasKilled { get; set; }

        public double? __stage1SecondsWastedAfterLastNPCWasKilled { get; set; } = null;
        public double stage1SecondsWastedAfterLastNPCWasKilled
        {
            get
            {
                if (__stage1SecondsWastedAfterLastNPCWasKilled != null)
                    return (double)__stage1SecondsWastedAfterLastNPCWasKilled;

                if (stage1TimeLastNPCWasKilled.AddMinutes(21) > DateTime.UtcNow)
                {
                    if (CurrentAbyssalStage == AbyssalStage.Stage1)
                    {
                        __stage1SecondsWastedAfterLastNPCWasKilled = (DateTime.UtcNow - stage1TimeLastNPCWasKilled).TotalSeconds;
                        return (double)__stage1SecondsWastedAfterLastNPCWasKilled;
                    }
                }

                return 0;
            }
        }

        public double? __stage2SecondsWastedAfterLastNPCWasKilled = null;
        public double _stage2SecondsWastedAfterLastNPCWasKilled
        {
            get
            {
                if (__stage2SecondsWastedAfterLastNPCWasKilled != null)
                    return (double)__stage2SecondsWastedAfterLastNPCWasKilled;

                if (stage2TimeLastNPCWasKilled.AddMinutes(21) > DateTime.UtcNow)
                {
                    if (CurrentAbyssalStage == AbyssalStage.Stage2)
                    {
                        __stage2SecondsWastedAfterLastNPCWasKilled = (DateTime.UtcNow - stage2TimeLastNPCWasKilled).TotalSeconds;
                        return (double)__stage2SecondsWastedAfterLastNPCWasKilled;
                    }
                }

                return 0;
            }
        }

        public double? __stage3SecondsWastedAfterLastNPCWasKilled = null;
        public double _stage3SecondsWastedAfterLastNPCWasKilled
        {
            get
            {
                if (__stage3SecondsWastedAfterLastNPCWasKilled != null)
                    return (double)__stage3SecondsWastedAfterLastNPCWasKilled;

                if (stage2TimeLastNPCWasKilled.AddMinutes(21) > DateTime.UtcNow)
                {
                    if (CurrentAbyssalStage == AbyssalStage.Stage2)
                    {
                        __stage3SecondsWastedAfterLastNPCWasKilled = (DateTime.UtcNow - stage3TimeLastNPCWasKilled).TotalSeconds;
                        return (double)__stage3SecondsWastedAfterLastNPCWasKilled;
                    }
                }

                return 0;
            }
        }



        internal float _abyssRemainingSeconds
        {
            get
            {
                switch (CurrentAbyssalStage)
                {
                    case AbyssalStage.Stage1:
                        _stage1SecondsSpent = Time.Instance.SecondsSinceLastSessionChange;
                        break;
                    case AbyssalStage.Stage2:
                        _stage2SecondsSpent = Time.Instance.SecondsSinceLastSessionChange;
                        break;
                    case AbyssalStage.Stage3:
                        _stage3SecondsSpent = Time.Instance.SecondsSinceLastSessionChange;
                        break;
                }

                if (ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds > 0)
                    return ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds;

                //
                // this needs to be calculated off the 1st instant we jumped into the abyss since the timer is broken if we hit this code: ccp is bad!
                //
                return 0;
            }
        }

        internal bool TargetingOfFleetMembersIsNeeded
        {
            get
            {
                if (DirectEve.Session.IsAbyssalDeadspace)
                {
                    if (ESCache.Instance.EveAccount.UseFleetMgr)
                    {
                        if (DebugConfig.DebugTargetCombatants) Log("UseFleetMgr true");
                        if (!ESCache.Instance.Modules.Any(x => (x.GroupId == (int)Group.RemoteShieldRepairer ||
                                        x.GroupId == (int)Group.RemoteArmorRepairer ||
                                        x.GroupId == (int)Group.RemoteEnergyTransfer ||
                                        x.GroupId == (int)Group.AncillaryRemoteShieldBooster ||
                                        x.GroupId == (int)Group.AncillaryRemoteArmorRepairer)))
                        {
                            if (DebugConfig.DebugTargetCombatants) Log("No Rep modules found");
                            return false;
                        }

                        if (ESCache.Instance.DirectEve.Me.IsInvuln)
                        {
                            Log("IsInvuln: Waiting");
                            return false;
                        }

                        if (ESCache.Instance.DirectEve.Me.IsSessionChangeActive)
                        {
                            Log("IsSessionChangeActive: Waiting");
                            return false;
                        }

                        if (DebugConfig.DebugTargetCombatants) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Count(i => i.TypeName.ToLower().Contains("Blastlance".ToLower())) >= 3)
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.ToLower().Contains(\"Blastlance\".ToLower())) > 3) return true");
                                return true;
                            }

                            return false;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.TypeName.ToLower().Contains("Lucifer Cynabal".ToLower())))
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("if (Combat.PotentialCombatTargets.Any(i => i.TypeName.ToLower().Contains(\"Lucifer Cynabal\".ToLower()))) return true");

                                return true;
                            }

                            if (Combat.PotentialCombatTargets.Count(i => i.TypeName.ToLower().Contains("Lucifer Dremiel".ToLower())) >= 3)
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.ToLower().Contains(\"Lucifer Dremiel\".ToLower())) >= 3)) return true");

                                return true;
                            }

                            return false;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.TypeName.ToLower().Contains("Vedmak".ToLower())))
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("if (Combat.PotentialCombatTargets.Any(i => i.TypeName.ToLower().Contains(\"Vedmak\".ToLower()))) return true");

                                return true;
                            }

                            return false;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship || i.IsNPCCruiser))
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship || i.IsNPCCruiser)) return true");

                                return true;
                            }

                            return false;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser)) return true");

                                return true;
                            }

                            return false;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser)) return true");

                                return true;
                            }

                            return false;
                        }


                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsAbyssalKaren))
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsAbyssalKaren)) return true");

                                return true;
                            }

                            return false;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser)) return true");

                                return true;
                            }

                            return false;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser)) return true");

                                return true;
                            }

                            return false;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn ||
                            AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.RodivaSpawn ||
                            AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn
                            )
                        {
                            if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 2)
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 2) return true");

                                return true;
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer)) return true");

                                return true;
                            }

                            if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) > 6)
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) > 6) return true");

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
        }

        internal int _maximumLockedTargets
        {
            get
            {
                if (TargetingOfFleetMembersIsNeeded)
                {
                    if (ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate)
                    {
                        return 4;
                    }

                    if (ESCache.Instance.MaxLockedTargets != null)
                    {
                        if (ESCache.Instance.MaxLockedTargets - 2 >= 2)
                        {
                            return ESCache.Instance.MaxLockedTargets ?? 0 - 2;
                        }

                        return ESCache.Instance.MaxLockedTargets ?? 0;
                    }
                }

                if (ESCache.Instance.MaxLockedTargets != null)
                    return (int)ESCache.Instance.MaxLockedTargets;

                return 0;
            }
        }

        internal IEnumerable<EntityCache> _trigItemCaches => ESCache.Instance.Entities.Where(e => IsEntityWeWantToLoot(e._directEntity));

        internal bool forceRecreatePath = false;

        internal int _remainingNonEmptyWrecksAndCacheCount => ESCache.Instance.Wrecks.Count(w => !w.IsWreckEmpty) + _trigItemCaches.Count();

        internal double _maxTargetRange => DirectEve.ActiveShip.MaxTargetRange;

        public bool SimulateGankToggle { get; set; } = false;

        internal bool CanAFilamentBeOpened(bool ignoreAbyssTrace = false)
        {
            if (DirectEve.Me.NPCTimerExists)
            {
                Log("We have an NPC Timer: we cant use a Filament right now");
                return false;
            }

            if (DirectEve.Me.PVPTimerExist)
            {
                Log("We have an PVP Timer: we cant use a Filament right now");
                return false;
            }

            if (DirectEve.Entities.Any(e => e.GroupId == (int)Group.MobileDepot))
            {
                Log("Found a Mobile Depot on grid: we cant use this spot");
                return false;
            }

            if (DirectEve.Entities.Any(e => e.IsStation && e.Distance <= 1000001))
            {
                Log("Found a Station within 1000001m: we cant use this spot");
                return false;
            }

            if (DirectEve.Entities.Any(e => e.IsStargate && e.Distance <= 1000001))
            {
                Log("Found a Stargate within 1000001m on grid: we cant use this spot");
                return false;
            }

            if (ESCache.Instance.EveAccount.UseFleetMgr && !ESCache.Instance.EveAccount.IsLeader)
            {
                if (DebugConfig.DebugFleetMgr) Log("UseFleetMgr [" + ESCache.Instance.EveAccount.UseFleetMgr + "] IsLeader [" + ESCache.Instance.EveAccount.IsLeader + "] we are not he leader the leader will activate the filament we just need to enter");
                return true;
            }

            if (DirectEve.Entities.Any(e => e.GroupId == (int)Group.AbyssalTrace && !ignoreAbyssTrace))
            {
                Log("Found an AbyssalTrace on grid: we cant use this spot");
                return false;
            }

            return true;
        }

        //internal bool IsAnyOtherPlayerOnGrid => ESCache.Instance.EntitiesNotSelf.Any(e => e.IsPlayer && e.Distance < 1000001 && DirectEve.Standings.GetCorporationRelationship(e.DirectEntity.CorpId) <= 0);
        internal bool IsAnyOtherNonFleetPlayerOnGrid => ESCache.Instance.EntitiesNotSelf.Any(e => e.IsPlayer && e.Distance < 1000001 && DirectEve.FleetMembers.All(x => x.CharacterId != e._directEntity.CharId));

        internal List<EntityCache> OtherNonFleetPlayersOnGrid => ESCache.Instance.EntitiesNotSelf.Where(e => e.IsPlayer && e.Distance < 1000001 && Framework.FleetMembers.All(x => x.CharacterId != e._directEntity.OwnerId)).ToList();

        internal bool SkipExtractionNodes
        {
            get
            {
                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakVilaCruiserSwarmerSpawn)
                    return false;

                if (180 > AbyssalController.__getEstimatedStageRemainingTimeToClearGrid)
                    return true;

                return true;
            }
        }

        internal double _currentlyUsedDroneBandwidth => ESCache.Instance.DirectEve.ActiveDrones.Sum(d => d.TryGet<double>("droneBandwidthUsed"));

        internal double _shipsRemainingBandwidth => ESCache.Instance.DirectEve.ActiveShip.GetRemainingDroneBandwidth();

        internal IEnumerable<DirectUIModule> ShieldBoosters => DirectEve.Modules.Where(e => e.GroupId == (int)Group.ShieldBoosters && e.IsOnline && e.HeatDamagePercent <= 99);

        internal IEnumerable<DirectUIModule> ShieldHardeners => DirectEve.Modules.Where(e => e.GroupId == (int)Group.ShieldHardeners && e.IsOnline && e.HeatDamagePercent <= 99);

        internal IEnumerable<DirectUIModule> Afterburners => DirectEve.Modules.Where(e => e.GroupId == (int)Group.Afterburner && e.IsOnline && e.HeatDamagePercent <= 99);


        internal bool IsEntityWeWantToLoot(DirectEntity entity)
        {
            try
            {
                if (entity.IsWreck && entity.IsEmpty)
                    return false;

                if (entity.IsAbyssalBioAdaptiveCache)
                    return true;

                //
                // the rest of this is for extraction nodes
                //
                if (!SkipExtractionNodes && entity.IsAbyssalDeadspaceTriglavianExtractionNode)
                {
                    if (entity.IsReadyToTarget && Combat.PotentialCombatTargets.Count() > 0) // Triglavian Extraction Node // Triglavian Extraction SubNode)
                    {
                        if (_getMTUInSpace == null && AbyssalController._mtuAlreadyDroppedDuringThisStage)
                        {
                            //we already dropped the MTU and picked it up... dont shoot any more caches
                            return false;
                        }

                        return true;
                    }
                }

                //
                // Always grab the ones that are close
                //
                if (entity.IsAbyssalDeadspaceTriglavianExtractionNode)
                {
                    if (true)// > entity.Distance) // Triglavian Extraction Node // Triglavian Extraction SubNode)
                    {
                        if (entity.IsReadyToTarget && Combat.PotentialCombatTargets.Count() > 0) // Triglavian Extraction Node // Triglavian Extraction SubNode)
                        {
                            if (_getMTUInSpace == null && AbyssalController._mtuAlreadyDroppedDuringThisStage)
                            {
                                //we already dropped the MTU and picked it up... dont shoot any more caches
                                return false;
                            }

                            return true;
                        }
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }

        //internal DirectStation HomeStation =>
        //    DirectEve.Stations.ContainsKey(_homeStationId) ? DirectEve.Stations[_homeStationId] : null;

        internal int _attemptsToJumpMidgate { get; set; }

        internal const int _smallMutatedDroneTypeId = 60478;
        internal const int _mediumMutatedDroneTypeId = 60479;
        internal const int _heavyMutatedDroneTypeId = 60480;

        internal int _attemptsToJumpFrigateDestroyerAbyss;

        internal EntityCache _midGate =>
            ESCache.Instance.Entities.FirstOrDefault(e => e.TypeId == 47685 && e.BracketType == BracketType.Warp_Gate);

        internal EntityCache _endGate =>
            ESCache.Instance.Entities.FirstOrDefault(e => e.TypeId == 47686 && e.BracketType == BracketType.Warp_Gate);

        internal EntityCache _nextGate => _midGate ?? _endGate;

        internal bool IsAbyssGateOpen
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.Undecided)
                {
                    if (_nextGate != null)
                    {
                        return _nextGate._directEntity.IsAbyssGateOpen();
                    }

                    return false;
                }

                return false;
            }
        }

        internal bool _prevIsAbyssGateOpen;

        internal int _remainingDronesInBay => DirectEve?.GetShipsDroneBay()?.Items?.Sum(i => i.Quantity) ?? 0;

        internal List<DirectItem> _getDronesInBay(MarketGroup marketGroup) => DirectEve?.GetShipsDroneBay()?.Items?.Where(d => d.MarketGroupId == (int)marketGroup)?.ToList() ?? new List<DirectItem>();

        internal List<DirectItem> _getDronesInBayByTypeId(int typeId) => DirectEve?.GetShipsDroneBay()?.Items?.Where(d => d.TypeId == typeId)?.ToList() ?? new List<DirectItem>();

        internal List<EntityCache> _getDronesInSpace(MarketGroup marketGroup) => Drones.ActiveDrones?.Where(d => d._directEntity.MarketGroupId == (int)marketGroup)?.ToList() ?? new List<EntityCache>();

        internal List<EntityCache> _getDronesInSpaceByTypeId(int typeId) => Drones.ActiveDrones?.Where(d => d.TypeId == typeId)?.ToList() ?? new List<EntityCache>();

        internal bool _isInLastRoom => _endGate != null;

        internal DateTime _lastFilamentActivation;

        internal DirectItem _getMTUInBay
        {
            get
            {
                try
                {
                    if (ESCache.Instance.CurrentShipsCargo == null)
                        return null;

                    if (ESCache.Instance.CurrentShipsCargo.Items.Any(i => i.GroupId == (int)Group.MobileTractor))
                        return ESCache.Instance.CurrentShipsCargo.Items.FirstOrDefault(i => i.GroupId == (int)Group.MobileTractor);

                    return null;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        internal EntityCache _getMTUInSpace
        {
            get
            {
                try
                {
                    if (ESCache.Instance.CurrentShipsCargo == null)
                        return null;

                    if (ESCache.Instance.Entities.Any(i => i.GroupId == (int)Group.MobileTractor))
                        return ESCache.Instance.Entities.FirstOrDefault(i => i.GroupId == (int)Group.MobileTractor);

                    return null;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return null;
                }
            }
        }

        internal bool _MTUAvailable => _getMTUInBay != null || _getMTUInSpace != null;

        internal DateTime _lastMTULaunch { get; set; } = DateTime.UtcNow;

        internal bool _MTUReady => _lastMTULaunch.AddSeconds(12) < DateTime.UtcNow;

        internal DateTime _timeStarted;

        internal double _valueLooted;

        internal List<DirectItem> smallDronesInBay => _getDronesInBay(MarketGroup.LightScoutDrone).Concat(_getDronesInBayByTypeId(_smallMutatedDroneTypeId)).ToList();
        internal List<DirectItem> mediumDronesInBay => _getDronesInBay(MarketGroup.MediumScoutDrone).Concat(_getDronesInBayByTypeId(_mediumMutatedDroneTypeId)).ToList();
        internal List<DirectItem> largeDronesInBay => _getDronesInBay(MarketGroup.HeavyAttackDrone).Concat(_getDronesInBayByTypeId(_heavyMutatedDroneTypeId)).ToList();
        internal List<EntityCache> smallDronesInSpace => _getDronesInSpace(MarketGroup.LightScoutDrone).Concat(_getDronesInSpaceByTypeId(_smallMutatedDroneTypeId)).ToList();
        internal List<EntityCache> mediumDronesInSpace => _getDronesInSpace(MarketGroup.MediumScoutDrone).Concat(_getDronesInSpaceByTypeId(_mediumMutatedDroneTypeId)).ToList();
        internal List<EntityCache> largeDronesInSpace => _getDronesInSpace(MarketGroup.HeavyAttackDrone).Concat(_getDronesInSpaceByTypeId(_heavyMutatedDroneTypeId)).ToList();
        internal List<DirectItem> alldronesInBay => smallDronesInBay.Concat(mediumDronesInBay).Concat(largeDronesInBay).ToList();
        internal List<EntityCache> allDronesInSpace => smallDronesInSpace.Concat(mediumDronesInSpace).Concat(largeDronesInSpace).ToList();
        internal List<EntityCache> largeMedDronesInSpace => mediumDronesInSpace.Concat(largeDronesInSpace).ToList();

        public bool IsOurShipWithintheAbyssBounds(int offset = 0)
        {
            if (!ESCache.Instance.InAbyssalDeadspace)
                return true;

            if (IsSpotWithinAbyssalBounds(ESCache.Instance.DirectEve.ActiveShip.Entity.DirectAbsolutePosition, offset))
                return true;

            return false;
        }

        // need to extend abyssal boundaries in this case, because some npcs sit on the edge -- and drones flying towards it passing through the 75k diameter
        public bool AreDronesWithinAbyssBounds
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return true;

                if (allDronesInSpace.All(d => IsSpotWithinAbyssalBounds(d._directEntity.DirectAbsolutePosition, 25000)))
                    return true;

                return false;
            }
        }

        public bool IsOurShipOutSideFromEntitiesWeWantToAvoid => !DirectEntity.AnyIntersectionAtThisPosition(ESCache.Instance.DirectEve.ActiveShip.Entity.DirectAbsolutePosition).Any();

        internal DateTime _lastMTUScoop { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Ensure that the container is open before calling this.
        /// </summary>
        /// <param name="typeId"></param>
        /// <returns></returns>
        internal int GetAmountofTypeIdLeftInCargo(int typeId, bool isMutated = false)
        {
            var shipsCargo = ESCache.Instance.DirectEve.GetShipsCargo();
            return shipsCargo.Items.Where(i => (i.TypeId == typeId && !isMutated) || (isMutated && i.IsDynamicItem && i.OrignalDynamicItem.TypeId == typeId)).Sum(e => e.Stacksize);
        }

        /// <summary>
        /// Ensure that the container is open before calling this.
        /// </summary>
        /// <param name="typeId"></param>
        /// <returns></returns>
        internal int GetAmountofTypeIdLeftItemhangar(int typeId, bool isMutated = false)
        {
            var itemHangar = ESCache.Instance.DirectEve.GetItemHangar();
            return itemHangar.Items
                .Where(i => (i.TypeId == typeId && !isMutated) ||
                            (isMutated && i.IsDynamicItem && i.OrignalDynamicItem.TypeId == typeId))
                .Sum(e => e.Stacksize);
        }



        /// <summary>
        /// Ensure that the container is open before calling this.
        /// </summary>
        /// <param name="typeId"></param>
        /// <returns></returns>
        internal int GetAmountofTypeIdLeftItemhangarAndCargo(int typeId, bool isMutated = false)
        {
            return GetAmountofTypeIdLeftInCargo(typeId, isMutated) + GetAmountofTypeIdLeftItemhangar(typeId, isMutated);
        }

        /// <summary>
        /// Ensure that the container is open before calling this.
        /// </summary>
        /// <param name="typeId"></param>
        /// <returns></returns>
        internal int GetAmountofTypeIdLeftItemhangarAndDroneBay(int typeId, bool isMutated = false)
        {
            return GetAmountOfTypeIdLeftInDroneBay(typeId, isMutated) + GetAmountofTypeIdLeftItemhangar(typeId, isMutated);
        }

        /// <summary>
        /// Ensure that the container is open before calling this.
        /// </summary>
        /// <param name="typeId"></param>
        /// <returns></returns>
        internal int GetAmountOfTypeIdLeftInDroneBay(int typeId, bool isMutated = false)
        {
            if (!Framework.ActiveShip.HasDroneBay)
                return 0;

            var droneBay = ESCache.Instance.DirectEve.GetShipsDroneBay();
            return droneBay.Items.Where(i => (i.TypeId == typeId && !isMutated) || (isMutated && i.IsDynamicItem && i.OrignalDynamicItem.TypeId == typeId)).Sum(e => e.Stacksize);
        }

        public void LogNextGateState()
        {
            try
            {
                var isOpen = IsAbyssGateOpen;
                if (_prevIsAbyssGateOpen != isOpen)
                {
                    Log($"IsAbyssgateOpen [{isOpen}]");
                    _prevIsAbyssGateOpen = isOpen;
                }
            }
            catch (Exception){ }
        }

        public void SendBroadcastMessageToAbyssalGuardController(string command, string param)
        {
            /**
            if (!String.IsNullOrEmpty(ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting
                    .AbyssalGuardCharacterName))
            {
                var abyssalGuardCharName = ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting
                    .AbyssalGuardCharacterName;

                SendBroadcastMessage(abyssalGuardCharName, nameof(AbyssalGuardController), command, param);
            }
            **/
        }
        public string GenerateGridDump(IEnumerable<DirectEntity> ents, bool includeEhp = true)
        {
            //var ents = DirectEve.Entities.Where(e => e.IsNPCByBracketType || e.GroupId == 2009);
            var distinctTypes = ents.DistinctBy(e => e.TypeId);
            var ret = string.Empty;
            foreach (var type in distinctTypes.OrderBy(e => e.AbyssalTargetPriority))
            {
                var count = ents.Count(e => e.TypeId == type.TypeId);
                if (includeEhp)
                    ret += $"{count}x {type.TypeName}[{Math.Round(type.ShieldPct, 2)} {Math.Round(type.ArmorPct, 2)} {Math.Round(type.StructurePct, 2)}],";
                else
                    ret += $"{count}x {type.TypeName},";
            }

            if (ret.Length > 1)
                ret = ret.Remove(ret.Length - 1);
            return ret;
        }

        public bool LaunchMTU()
        {

            var mtu = _getMTUInBay;

            if (mtu != null && DirectEve.Interval(4000, 5000))
            {
                if (mtu.LaunchForSelf())
                {
                    Log($"Launching the MTU.");
                    _lastMTULaunch = DateTime.UtcNow;
                    return true;
                }
            }
            return false;
        }

        public bool ScoopMTU()
        {
            var mtu = _getMTUInSpace;
            if (mtu != null && mtu.Distance <= (double)Distances.ScoopRange && DirectEve.Interval(2000, 3000))
            {
                if (mtu._directEntity.Scoop())
                {
                    Log($"Scooping the MTU.");
                    _lastMTUScoop = DateTime.UtcNow;
                    return true;
                }

                return false;
            }

            return false;
        }

        public AbyssalBaseController()
        {
            //ESCache.Instance.InitInstances();
            Log("public AbyssalBaseBehavior()");
            Form = new AbyssalControllerForm(this);
            _timeStarted = DateTime.UtcNow;
            _valueLooted = 0;
            // if we start in abyss space, we assume we have to recover from a crash

            AreWeResumingFromACrash = false;
            //AreWeResumingFromACrash = ESCache.Instance.DirectEve.Me.IsInAbyssalSpace(); // need a workaround for that
        }

        internal void UpdateIskLabel(double value)
        {
            var frm = this.Form as AbyssalControllerForm;
            frm.IskPerHLabel.Invoke(new Action(() =>
            {
                frm.IskPerHLabel.Text = Math.Round(value, 2).ToString(CultureInfo.InvariantCulture);
            }));
        }

        internal void UpdateStageEHPValues(double v1, double v2)
        {
            var frm = this.Form as AbyssalControllerForm;
            frm.TotalStageEhp.Invoke(new Action(() =>
            {
                frm.TotalStageEhp.Text = Math.Round(v1, 2).ToString(CultureInfo.InvariantCulture) + "/" + Math.Round(v2, 2).ToString(CultureInfo.InvariantCulture);
            }));
        }

        internal void UpdateStageKillEstimatedTime(double v1)
        {
            try
            {
                var frm = this.Form as AbyssalControllerForm;
                frm.EstimatedNpcKillTime.Invoke(new Action(() =>
                {
                    frm.EstimatedNpcKillTime.Text = Math.Round(v1, 2).ToString(CultureInfo.InvariantCulture);
                }));
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        internal void UpdateIgnoreAbyssEntities(bool v1)
        {
            try
            {
                var frm = this.Form as AbyssalControllerForm;
                frm.IgnoreAbyssEntities.Invoke(new Action(() =>
                {
                    frm.IgnoreAbyssEntities.Text = v1.ToString();
                }));
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        internal void UpdateStageLabel(AbyssalStage value)
        {
            try
            {
                var frm = this.Form as AbyssalControllerForm;
                frm.StageLabel.Invoke(new Action(() =>
                {
                    frm.StageLabel.Text = value.ToString();
                }));
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        internal void UpdateStageRemainingSecondsLabel(int value)
        {
            var frm = this.Form as AbyssalControllerForm;
            frm.StageRemainingSeconds.Invoke(new Action(() =>
            {
                frm.StageRemainingSeconds.Text = value.ToString();
            }));
        }


        internal void UpdateTimeNeededToGetToTheGate(int value)
        {
            var frm = this.Form as AbyssalControllerForm;
            frm.TimeNeededToGetToTheGate.Invoke(new Action(() =>
            {
                frm.TimeNeededToGetToTheGate.Text = value.ToString();
            }));
        }



        internal void UpdateWreckLootTime(int value)
        {
            var frm = this.Form as AbyssalControllerForm;
            frm.WreckLootTime.Invoke(new Action(() =>
            {
                frm.WreckLootTime.Text = value.ToString();
            }));
        }


        internal void UpdateAbyssTotalTime(int value)
        {
            var frm = this.Form as AbyssalControllerForm;
            frm.AbyssTotalTime.Invoke(new Action(() =>
            {
                frm.AbyssTotalTime.Text = value.ToString();
            }));
        }

    }
}
