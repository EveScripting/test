//
// (c) duketwo 2022
//

extern alias SC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Combat;
//using EVESharpCore.Questor.States;
//using SC::SharedComponents.EVE.ClientSettings;
//using SC::SharedComponents.EVE.DatabaseSchemas;
using SC::SharedComponents.Extensions ;
using SC::SharedComponents.SQLite;
using SC::SharedComponents.Utility;
using SC::SharedComponents.EVE;
using System.Globalization;
using EVESharpCore.Framework.Events;
using SC::SharedComponents.Events;
using ServiceStack.OrmLite;

namespace EVESharpCore.Controllers.Abyssal
{
    public partial class AbyssalController : AbyssalBaseController
    {

        private int _droneRecallsStage { get; set; }
        private int _droneRecallsStage2 { get; set; }

        public enum DroneSize
        {
            Small,
            Medium,
            Large,
            Gecko,
        }

        public class AbyssalDrone
        {
            public long DroneId { get; set; }

            public string MaskedDroneId
            {
                get
                {
                    try
                    {
                        int numofCharacters = DroneId.ToString(CultureInfo.InvariantCulture).Length;
                        if (numofCharacters >= 5)
                        {
                            string maskedID = DroneId.ToString(CultureInfo.InvariantCulture).Substring(numofCharacters - 4);
                            maskedID = "[MaskedID]" + maskedID;
                            return maskedID;
                        }

                        return "!0!";
                    }
                    catch (Exception ex)
                    {
                        Log("Exception [" + ex + "]");
                        return "!0!";
                    }
                }
            }

            public int TypeId { get; set; }
            public string TypeName { get; set; }
            public double Bandwidth { get; set; }
            public DroneActionState ActionState { get; set; }
            public int StackSize { get; set; }

            public double ShieldPercent { get; set; }
            public double ArmorPercent { get; set; }
            public double StructurePercent { get; set; }

            public enum DroneActionState
            {
                InSpace,
                DeployingLimbo,
                InBay,
            }

            public AbyssalDrone(DirectItem directItem)
            {
                DroneId = directItem.ItemId;
                TypeName = directItem.TypeName;
                TypeId = directItem.IsDynamicItem ? directItem.OrignalDynamicItem.TypeId : directItem.TypeId;
                Bandwidth = directItem.TryGet<double>("droneBandwidthUsed");
                ActionState = DroneActionState.InBay;
                StackSize = directItem.Stacksize;
                ShieldPercent = directItem.GetDroneInBayDamageState()?.X ?? 0d;
                ArmorPercent = directItem.GetDroneInBayDamageState()?.Y ?? 0d;
                StructurePercent = directItem.GetDroneInBayDamageState()?.Z ?? 0d;
            }

            public AbyssalDrone(DirectEntity directEntity)
            {
                DroneId = directEntity.Id;
                TypeName = directEntity.TypeName;
                TypeId = directEntity.IsDynamicItem ? directEntity.OrignalDynamicItem.TypeId : directEntity.TypeId;
                Bandwidth = directEntity.TryGet<double>("droneBandwidthUsed");
                ActionState = DroneActionState.InSpace;
                StackSize = 1;
                ShieldPercent = directEntity.ShieldPct;
                ArmorPercent = directEntity.ArmorPct;
                StructurePercent = directEntity.StructurePct;
            }

            /// <summary>
            /// Can be null!
            /// </summary>
            public DirectItem? GetDirectItem => ESCache.Instance.DirectEve.GetShipsDroneBay()?.Items.FirstOrDefault(e => e.ItemId == DroneId);

            /// <summary>
            /// Can be null!
            /// </summary>
            public DirectEntity? GetDirectEntity => ESCache.Instance.DirectEve.ActiveDrones.FirstOrDefault(e => e.Id == DroneId);

            public DirectInvType? GetInvType => ActionState == DroneActionState.InSpace ? (DirectInvType)GetDirectEntity : ActionState == DroneActionState.InBay ? (DirectInvType)GetDirectItem : null;
        }

        private List<AbyssalDrone> _limboDeployingAbyssalDrones = new List<AbyssalDrone>();
        private Dictionary<long, DateTime> _recentlyDeployedDronesTimer = new Dictionary<long, DateTime>();
        private Dictionary<long, DateTime> _recentlyRecalledDronesTimer = new Dictionary<long, DateTime>();
        private Dictionary<long, DateTime> _limboDeployingDronesTimer = new Dictionary<long, DateTime>();

        private List<long> _previouslyDeployedDronesDebug = new List<long>();

        private int _droneRecallsDueEnemiesBeingInASpeedCloud = 0;
        private int _tooManyEnemiesAreStillInASpeedCloudCount = 0;
        private DateTime _nextDroneRecallDueEnemiesBeingInASpeedCloud = DateTime.MinValue;

        public void UpdateDroneStateForDeployed(AbyssalDrone abyssalDrone)
        {
            if (abyssalDrone.StackSize > 1)
            {
                // Drone ids are created when stack is split, cannot track.
                return;
            }

            abyssalDrone.ActionState = AbyssalDrone.DroneActionState.DeployingLimbo;
            _limboDeployingDronesTimer[abyssalDrone.DroneId] = DateTime.UtcNow;
            _recentlyDeployedDronesTimer[abyssalDrone.DroneId] = DateTime.UtcNow;
            _limboDeployingAbyssalDrones.Add(abyssalDrone);
        }

        public void UpdateDroneStateForRecall(long droneId)
        {
            //_recentlyDeployedDronesTimer.Remove(droneId);
            _recentlyRecalledDronesTimer[droneId] = DateTime.UtcNow;
        }

        bool IsValidDroneHealth(AbyssalDrone drone)
            {
                if (drone.ActionState == AbyssalDrone.DroneActionState.InBay)
                {
                    if (drone.ShieldPercent >= (_droneLaunchShieldPerc[(int)drone.Bandwidth] / 100d))
                    {
                        return true;
                    }

                    if (DebugConfig.DebugDrones) Log("[InBay][" + drone.TypeName + "][" + drone.ShieldPercent + "] is less than [" + _droneLaunchShieldPerc[(int)drone.Bandwidth] / 100d + "] return false");
                    return false;
                }
                else if (drone.ActionState == AbyssalDrone.DroneActionState.DeployingLimbo)
                {
                    return true;
                }
                else if (drone.ActionState == AbyssalDrone.DroneActionState.InSpace)
                {
                    if (drone.ShieldPercent >= (_droneRecoverShieldPerc[(int)drone.Bandwidth] / 100d))
                    {
                        return true;
                    }

                    if (DebugConfig.DebugDrones) Log("[InSpace][" + drone.TypeName + "][" + drone.ShieldPercent + "] is less than [" + _droneLaunchShieldPerc[(int)drone.Bandwidth] / 100d + "] return false");
                    return false;
                }

                return true;
            }

        public List<EntityCache> _listOfDronesWanderingOffAttackingTheWrongTargets = new List<EntityCache>();

        public List<EntityCache> ListOfDronesWanderingOffAttackingTheWrongTargets
        {
            get
            {
                if (!DirectEve.HasFrameChanged())
                    return _listOfDronesWanderingOffAttackingTheWrongTargets;

                _listOfDronesWanderingOffAttackingTheWrongTargets = new List<EntityCache>();
                foreach (var droneInSpace in allDronesInSpace)
                {
                    if (droneInSpace == null)
                        continue;

                    if (droneInSpace._directEntity.FollowEntity == null)
                        continue;

                    if (droneInSpace.FollowId == 0)
                        continue;

                    if (droneInSpace._directEntity.DroneState == (int)Drones.DroneState.Attacking && droneInSpace._directEntity.FollowEntity != null && Combat.PotentialCombatTargets.All(i => i.Id != droneInSpace._directEntity.FollowEntity.Id))
                    {
                        Log("Drone [" + droneInSpace.TypeName + "][" + Math.Round(droneInSpace.Distance, 0) + "][" + droneInSpace.Id + "] DroneState [" + droneInSpace._directEntity.DroneStateName + "] FollowEntity [" + droneInSpace._directEntity.FollowEntityName + "] PotentialCombatTargets.All(i => i.Id != droneEntity.FollowEntity.Id))");
                        _listOfDronesWanderingOffAttackingTheWrongTargets.Add(droneInSpace);
                    }

                    continue;
                }

                return _listOfDronesWanderingOffAttackingTheWrongTargets ?? new List<EntityCache>();
            }
        }

        public List<AbyssalDrone> GetWantedDronesInSpace()
        {
            if (!Combat.PotentialCombatTargets.Any() && !ESCache.Instance.Targets.Any(i => i.IsAbyssalBioAdaptiveCache))
                return new List<AbyssalDrone>();

            if (_singleRoomAbyssal && DebugConfig.DebugFourteenBattleshipSpawnRunAway)
                return new List<AbyssalDrone>();

            var droneBay = ESCache.Instance.DirectEve.GetShipsDroneBay();
            // Reset any timer state
            // Remove any drones from recently deployed that are no longer recent

            var totalBandwidth = (double)ESCache.Instance.DirectEve.ActiveShip.DroneBandwidth;
            var remainingBandwidth = (double)ESCache.Instance.DirectEve.ActiveShip.GetRemainingDroneBandwidth();

            // we need to change this dynamically on what is current in space, i would suggest to use the remaining ships bandwidth as factor
            // (so if we don't occupy all or bandwidth, we lose damage). so if only 50 bandwidth is used with 5 drones, swap quicker
            // than if 5 drones and 110 bandwidth used for example
            var recentlyDeployedDelay = 5 + (20 * ((totalBandwidth - remainingBandwidth) / totalBandwidth));

            _recentlyDeployedDronesTimer = _recentlyDeployedDronesTimer
                .Where(entry => entry.Value > DateTime.UtcNow.AddSeconds(-recentlyDeployedDelay))
                .ToDictionary(entry => entry.Key, entry => entry.Value);
            _limboDeployingDronesTimer = _limboDeployingDronesTimer
                .Where(entry => entry.Value > DateTime.UtcNow.AddSeconds(-15))
                .ToDictionary(entry => entry.Key, entry => entry.Value);
            _limboDeployingAbyssalDrones = _limboDeployingAbyssalDrones
                .Where(d => _limboDeployingDronesTimer.ContainsKey(d.DroneId))
                .ToList();

            // Get all drones
            var dronesInSpace = allDronesInSpace.Select(d => new AbyssalDrone(d._directEntity)).ToList();
            var dronesInBay = alldronesInBay.Select(d => new AbyssalDrone(d)).ToList();
            var dronesInLimbo = _limboDeployingAbyssalDrones;

            // Clean up duplicates where a limbo drone now exists in space
            var allDrones = Enumerable.Empty<AbyssalDrone>()
                .Concat(dronesInSpace)
                .Concat(dronesInBay)
                .Concat(dronesInLimbo)
                .ToList();

            foreach (var drone in dronesInBay)
            {
                _limboDeployingDronesTimer[drone.DroneId] = DateTime.MinValue;
                _recentlyDeployedDronesTimer[drone.DroneId] = DateTime.MinValue;
            }

            // Remove duplicates that might exists in multiple lists
            // Space > Limbo > In bay
            // If in space and in limbo then should be removed from limbo as it's now outside limbo
            // If in limbo and in bay use limbo as it's deploying
            allDrones = allDrones
                .GroupBy(d => d.DroneId)
                .Select(g => g.OrderBy(d => d.ActionState).First())
                .ToList();


            var allDronesLookup = allDrones.ToDictionary(d => d.DroneId);


            //var validDronesToPick = allDrones.Where(i => IsValidDroneHealth(i) && !IsDroneWanderingOffAttackingTheWrongTargets(i));
            var validDronesToPick = allDrones.Where(i => IsValidDroneHealth(i));
            var numGeckos = validDronesToPick.Where(x => x.TypeId == _geckoDroneTypeId).Count();
            int PrioritizeDrone(AbyssalDrone drone)
            {
                // Returns grouped drones by type via an ordering number, 3 > 2 > 1 where everything in each number becomes it's own group for further stable
                // sorting below

                // Karen 3 Berserker, 2 Gecko, 1 All else
                // Normal 3 Gecko, 2 Berserker, 1 all else
                if (_roomStrategy == AbyssalRoomStrategy.HeavyDronesForExploitingNPCResistHole)
                {
                    // For Karen rooms we want to order Berserkers first to maximize DPS without caring about -tracking
                    //return drone.TypeId switch
                    //{
                    //    _karenDroneTypeId => 4, // Mutated first
                    //    _geckoDroneTypeId => 2, // Geckos
                    //    _ => 0, // The rest

                    //};

                    return drone.TypeId == _heavyDroneTypeId ? 4 : (drone.TypeId == _geckoDroneTypeId ? 2 : 0);
                }

                // Else we want to use Geckos with Berserkers ONLY if we are out of usable geckos
                //return drone.TypeId switch
                //{
                //    _geckoDroneTypeId => 4, // Geckos
                //    _karenDroneTypeId => numGeckos <= 1
                //        ? 3 // Not enough geckos, allow for Gecko + 3 Berserkers OR 5 berserkers deploy to try save the run.
                //        : 0, // Mutated last to ensure we don't get a 2 Gecko, 1 Mutated deploy
                //    _ => 2, // Everything else will be above Berserkers unless there is 1 or less Geckos available
                //};

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.VexorNavyIssue)
                {
                    if (!Combat.PotentialCombatTargets.Where(x => x.BracketType != BracketType.Large_Collidable_Structure).Any(i => i.IsNPCBattleship || i.IsNPCBattlecruiser || i.IsNPCCruiser))
                    {
                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn)
                        {
                            //higher number is higher priority!
                            //use medium drones, then small drones, then heavies
                            //usually will result in 5 medium drones in space (50m3)
                            return drone.TypeId == _geckoDroneTypeId ? 2 :
                                   drone.TypeId == _heavyDroneTypeId ? 1 :
                                   drone.TypeId == _mediumDroneTypeId ? 4 :
                                   3;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikVilaFrigateSwarmerSpawn)
                        {
                            //higher number is higher priority!
                            //use medium drones, then small drones, then heavies
                            //usually will result in 5 medium drones in space (50m3)
                            return drone.TypeId == _geckoDroneTypeId ? 2 :
                                    drone.TypeId == _heavyDroneTypeId ? 1 :
                                    drone.TypeId == _mediumDroneTypeId ? 4 :
                                    3;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn)
                        {
                            //higher number is higher priority!
                            //use medium drones, then small drones, then heavies
                            //usually will result in 5 medium drones in space (50m3)
                            return drone.TypeId == _geckoDroneTypeId ? 2 :
                                    drone.TypeId == _heavyDroneTypeId ? 1 :
                                    drone.TypeId == _mediumDroneTypeId ? 4 :
                                    3;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                        {
                            //higher number is higher priority!
                            //use medium drones, then small drones, then heavies
                            //usually will result in 5 medium drones in space (50m3)
                            return drone.TypeId == _geckoDroneTypeId ? 2 :
                                    drone.TypeId == _heavyDroneTypeId ? 1 :
                                    drone.TypeId == _mediumDroneTypeId ? 4 :
                                    3;
                        }

                        if (Combat.PotentialCombatTargets.Where(x => x.BracketType != BracketType.Large_Collidable_Structure).All(i => i.IsNPCFrigate))
                        {
                            //higher number is higher priority!
                            //use medium drones, then small drones, then heavies
                            //usually will result in 5 medium drones in space (50m3)
                            return drone.TypeId == _geckoDroneTypeId ? 2 :
                                    drone.TypeId == _heavyDroneTypeId ? 1 :
                                    drone.TypeId == _mediumDroneTypeId ? 4 :
                                    3;
                        }
                    }

                    if (Combat.PotentialCombatTargets.Where(x => x.BracketType != BracketType.Large_Collidable_Structure).Any(i => i.IsNPCCruiser))
                    {
                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn)
                        {
                            //higher number is higher priority!
                            //use medium drones, then small drones, then heavies
                            //usually will result in 5 medium drones in space (50m3)
                            return drone.TypeId == _geckoDroneTypeId ? 2 :
                                    drone.TypeId == _heavyDroneTypeId ? 1 :
                                    drone.TypeId == _mediumDroneTypeId ? 4 :
                                    3;
                        }
                    }

                    return drone.TypeId == _geckoDroneTypeId ? 4 :
                           drone.TypeId == _heavyDroneTypeId ? 3 :
                           drone.TypeId == _mediumDroneTypeId ? 2 :
                           1;
                }

                return drone.TypeId == _geckoDroneTypeId ? 4 :
                       drone.TypeId == _heavyDroneTypeId ? 3 :
                       drone.TypeId == _mediumDroneTypeId ? 2 :
                       1;
            }

            var selectableDrones = validDronesToPick
                .OrderBy(d => d.ActionState switch
                {
                    AbyssalDrone.DroneActionState.DeployingLimbo => 1,
                    AbyssalDrone.DroneActionState.InSpace => 2,
                    AbyssalDrone.DroneActionState.InBay => 3,
                }) // Put Limbo and InSpace drones on top
                .ThenByDescending(d => _recentlyDeployedDronesTimer.ContainsKey(d.DroneId))
                .ThenByDescending(PrioritizeDrone)
                .ThenByDescending(x => x.Bandwidth)
                .ThenBy(x => _recentlyRecalledDronesTimer.ContainsKey(x.DroneId) ? _recentlyRecalledDronesTimer[x.DroneId] : DateTime.MinValue)
                .ThenBy(x => x.StackSize)
                .ThenByDescending(x => x.ArmorPercent)
                .ThenByDescending(x => x.StructurePercent)
                .ToList();

            var dronesIWant = new List<AbyssalDrone>();

            // Handle always adding drones that are in space already or are in limbo
            var alwaysSelectedDrones = selectableDrones.Where(drone => drone.ActionState == AbyssalDrone.DroneActionState.DeployingLimbo);
            var droneBandwidthAvaliable = (double)ESCache.Instance.DirectEve.ActiveShip.DroneBandwidth;
            var droneCountAvaliable = 5;

            foreach (var alwaysSelectedDrone in alwaysSelectedDrones)
            {
                // Cannot deploy anymore drones
                if (droneCountAvaliable == 0 || alwaysSelectedDrone.Bandwidth > droneBandwidthAvaliable)
                {
                    continue;
                }

                droneBandwidthAvaliable -= alwaysSelectedDrone.Bandwidth;
                droneCountAvaliable--;
                dronesIWant.Add(alwaysSelectedDrone);
            }

            // Handle any additional drones in-bay that could be deployed
            var avaliableDrones = selectableDrones
                .Where(drone =>
                    drone.ActionState == AbyssalDrone.DroneActionState.InSpace
                    || drone.ActionState == AbyssalDrone.DroneActionState.InBay);

            foreach (var avaliableDroneInBay in avaliableDrones)
            {
                // Cannot deploy anymore drones
                if (droneCountAvaliable == 0) break;

                // Drone too big to deploy
                if (avaliableDroneInBay.Bandwidth > droneBandwidthAvaliable)
                    continue;

                droneBandwidthAvaliable -= avaliableDroneInBay.Bandwidth;
                droneCountAvaliable--;

                dronesIWant.Add(avaliableDroneInBay);
            }

            const bool LogState = true;
            if (LogState)
            {
                var changed = dronesIWant.Select(x => x.DroneId).Except(_previouslyDeployedDronesDebug).Any();

                if (changed)
                {
                    Log($"Strategy: {_roomStrategy}");
                    if (DebugConfig.DebugDrones)
                    {
                        foreach (var drone in allDrones)
                        {
                            var isWanted = dronesIWant.Any(e => e.DroneId == drone.DroneId);
                            var isValid = validDronesToPick.Any(e => e.DroneId == drone.DroneId);
                            DateTime recentlyDeployedTimer = DateTime.UtcNow.AddDays(-1);
                            _recentlyDeployedDronesTimer.TryGetValue(drone.DroneId, out recentlyDeployedTimer);
                            DateTime limboTimer = DateTime.UtcNow.AddDays(-1);
                            _limboDeployingDronesTimer.TryGetValue(drone.DroneId, out limboTimer);

                            var sb = new StringBuilder();
                            sb.Append("Id=").Append(drone.MaskedDroneId).Append("|");
                            sb.Append("TypeName=").Append(drone.TypeName).Append("|");
                            sb.Append("State=").Append(drone.ActionState).Append("|");
                            sb.Append("IsValid=").Append(isValid).Append("|");
                            sb.Append("TypeId=").Append(drone.TypeId).Append("|");
                            sb.Append("Bandwidth=").Append(drone.Bandwidth).Append("|");
                            sb.Append("Wanted=").Append(isWanted).Append("|");
                            sb.Append("Shield=").Append(drone.ShieldPercent).Append("|");
                            sb.Append("RDP=").Append(recentlyDeployedTimer).Append("|");
                            sb.Append("LT=").Append(limboTimer).Append("|");
                            Log(sb.ToString());
                        }
                    }

                    _previouslyDeployedDronesDebug.Clear();
                    _previouslyDeployedDronesDebug.AddRange(dronesIWant.Select(x => x.DroneId));
                }
            }

            return dronesIWant;
        }

        public List<AbyssalDrone> GetHighestBandwidthAndAmountDrones(List<AbyssalDrone> drones, int maxDrones, int maxBandwidth)
        {

            if (!drones.Any())
                return new List<AbyssalDrone>();

            if (maxDrones <= 0)
                return new List<AbyssalDrone>();

            if (maxBandwidth <= 0)
                return new List<AbyssalDrone>();

            if (drones.Count <= maxDrones)
                maxDrones = drones.Count;

            if (drones.Sum(d => d.Bandwidth) <= maxBandwidth)
                maxBandwidth = drones.Sum(d => (int)d.Bandwidth);

            // Sort the original list in descending order by bandwidth
            drones = drones.OrderByDescending(d => d.Bandwidth).ToList();

            // Create a 2D array to store the dynamic programming results
            int[,] dp = new int[maxDrones + 1, maxBandwidth + 1];

            // Iterate through each drone in the sorted list
            for (int i = 0; i < drones.Count; i++)
            {
                AbyssalDrone currentDrone = drones[i];

                // Iterate from the maximum number of drones down to 1
                for (int j = maxDrones; j >= 1; j--)
                {
                    // Iterate from the maximum bandwidth down to the current drone's bandwidth
                    for (int k = maxBandwidth; k >= (int)currentDrone.Bandwidth; k--)
                    {
                        // Calculate the new bandwidth value if the current drone is selected
                        int newBandwidth = dp[j - 1, k - (int)currentDrone.Bandwidth] + (int)currentDrone.Bandwidth;

                        // Update the maximum bandwidth value in the DP array
                        if (newBandwidth > dp[j, k])
                        {
                            dp[j, k] = newBandwidth;
                        }
                    }
                }
            }

            List<AbyssalDrone> returnedDrones = new List<AbyssalDrone>();
            List<List<AbyssalDrone>> dronesCache = new List<List<AbyssalDrone>>();

            for (int j = drones.Count - 1; j >= 0; j--)
            {
                // Initialize variables to track remaining bandwidth and number of drones
                int remainingBandwidth = maxBandwidth;
                int remainingDrones = maxDrones;
                // Make a copy of the original list to choose drones from
                var dronesToChooseFrom = drones.ToList();
                // Create a list to store the selected drones
                List<AbyssalDrone> selectedDrones = new List<AbyssalDrone>();
                // Iterate through the sorted list in reverse order
                for (int i = j; i >= 0; i--)
                {
                    AbyssalDrone currentDrone = drones[i % drones.Count];

                    // Check if the current drone can be selected based on remaining bandwidth, remaining drones,
                    // and if selecting it leads to the maximum bandwidth value
                    if (currentDrone.Bandwidth <= remainingBandwidth && remainingDrones > 0 &&
                dp[remainingDrones, remainingBandwidth] == dp[remainingDrones - 1, remainingBandwidth - (int)currentDrone.Bandwidth] + (int)currentDrone.Bandwidth)
                    {
                        // Find the corresponding drone from the original list using the bandwidth value
                        var d = dronesToChooseFrom.FirstOrDefault(x => x.TypeId == currentDrone.TypeId);
                        if (d != null)
                        {
                            // Add the selected drone to the list
                            selectedDrones.Add(d);

                            // Remove the selected drone from the drones to choose from
                            dronesToChooseFrom.Remove(d);

                            // Update remaining bandwidth and number of drones
                            remainingBandwidth -= (int)currentDrone.Bandwidth;
                            remainingDrones--;
                        }
                    }
                }
                if (selectedDrones.Any())
                {
                    dronesCache.Add(selectedDrones);
                }
            }

            if (dronesCache.Count > 0)
            {
                var highestBW = dronesCache.Where(e => e.Sum(d => d.Bandwidth) <= maxBandwidth).OrderByDescending(e => e.Sum(d => d.Bandwidth)).FirstOrDefault().Sum(d => d.Bandwidth);
                // Select the highest drone amount from the group with highest bandwidth
                //Debug.WriteLine($"Highest BW {highestBW}");
                returnedDrones = dronesCache.Where(e => e.Sum(d => d.Bandwidth) == highestBW).OrderByDescending(e => e.Count).FirstOrDefault();
            }

            // Return the list of selected drones
            return returnedDrones;
        }

        private bool ShouldILaunchDronesNow
        {
            get
            {
                if (!Combat.PotentialCombatTargets.Any() && !ESCache.Instance.Targets.Any(i => i.IsAbyssalBioAdaptiveCache))
                    return false;


                if (Time.Instance.SecondsSinceLastSessionChange >= 18)
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                {
                    if (4 >= Time.Instance.SecondsSinceLastSessionChange)
                        return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                {
                    if (Time.Instance.LastDronesNeedToBePulledTimeStamp.AddSeconds(10) > DateTime.UtcNow)
                    {
                        if (!allDronesInSpace.Any() && !Combat.PotentialCombatTargets.All(i => i.IsAttacking))
                            return false;
                    }

                    return true;
                }

                /**
                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
                {
                    if (Time.Instance.LastDronesNeedToBePulledTimeStamp.AddSeconds(20) > DateTime.UtcNow)
                    {
                        if (!allDronesInSpace.Any() && !Combat.PotentialCombatTargets.All(i => i.IsAttacking))
                            return false;
                    }

                    return true;
                }
                **/
                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                {
                    if (Time.Instance.LastDronesNeedToBePulledTimeStamp.AddSeconds(12) > DateTime.UtcNow)
                    {
                        if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                        {
                            if (2 >= alldronesInBay.Count())
                            {
                                if (DirectEve.Interval(15000)) Log("ShouldILaunchDronesNow: IsFrigOrDestroyerWithDroneBonuses: if (2 >= alldronesInBay.Count())");
                                if (dronesNeedSeriousRepair)
                                {
                                    if (DirectEve.Interval(15000)) Log("ShouldILaunchDronesNow: IsFrigOrDestroyerWithDroneBonuses: if (dronesNeedSeriousRepair) return false");
                                    return false;
                                }
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser && !i.IsAttacking))
                            {
                                Log("ShouldILaunchDronesNow: if (!allDronesInSpace.Any() && Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser && !i.IsAttacking)) return false;");
                                return false;
                            }
                        }
                    }

                    if (ESCache.Instance.DictionaryCachedPerPocketLastAttackedDrones.Any() &&
                        ESCache.Instance.DictionaryCachedPerPocketLastAttackedDrones.Any(i => i.Value.AddSeconds(15) > DateTime.UtcNow &&
                            Combat.PotentialCombatTargets.Any(x => x.IsNPCBattlecruiser &&
                                                                   x.IsAttacking &&
                                                                   30000 > x.Distance &&
                                                                   x.Id == i.Key)))
                    {
                        Log("ShouldILaunchDronesNow: DictionaryCachedPerPocketLastAttackedDrones: return false!.!");
                        //return false;
                    }

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                {
                    if (Time.Instance.LastDronesNeedToBePulledTimeStamp.AddSeconds(10) > DateTime.UtcNow)
                    {
                        if (!allDronesInSpace.Any() && !Combat.PotentialCombatTargets.All(i => i.IsAttacking))
                            return false;
                    }

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.TypeName.ToLower().Contains("Devoted Knight".ToLower())) > 2)
                    {
                        if (4 >= Time.Instance.SecondsSinceLastSessionChange)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        private bool LaunchDrones(List<AbyssalDrone> dronesIWant)
        {
            if (!Combat.PotentialCombatTargets.Any())
                return false;

            if (!dronesIWant.Any())
                return false;

            if (!IsOurShipWithintheAbyssBounds(3500))
                return false;

            var dronesToDeploy = dronesIWant.Where(d => alldronesInBay.Any(dib => dib.ItemId == d.DroneId)).ToList();

            // remove all drones which are below drone launch perc
            dronesToDeploy.RemoveAll(x => (x.ShieldPercent < (_droneLaunchShieldPerc[(int)x.Bandwidth] / 100d)));

            if (!dronesToDeploy.Any())
                return false;

            if (ESCache.Instance.DirectEve.ActiveDrones.Count >= 5)
                return false;

            if (dronesToDeploy.Sum(d => d.Bandwidth) > _shipsRemainingBandwidth)
                return false;

            var remainingBandwidth = _shipsRemainingBandwidth;
            while (dronesToDeploy.Sum(d => d.Bandwidth) > remainingBandwidth && dronesToDeploy.Any())
            {
                var droneToRemove = dronesToDeploy.OrderBy(e => e.Bandwidth).FirstOrDefault();
                if (droneToRemove == null)
                    break;
                dronesToDeploy.Remove(droneToRemove);
            }

            if (!ShouldILaunchDronesNow)
                return false;

            var droneItems = dronesToDeploy.Select(e => e.GetDirectItem).Where(e => e != null).ToList();
            var droneDeploySuccess = ESCache.Instance.ActiveShip.LaunchDrones(droneItems);

            if (!droneDeploySuccess)
                return false;


            _allDronesInSpaceWhichAreNotReturning = new List<EntityCache>();

            Log($"Launching the following drones:");
            foreach (var drone in dronesToDeploy)
            {
                Log($"[{drone.TypeName}] S[{Math.Round(drone.ShieldPercent)}] A[{Math.Round(drone.ArmorPercent)}] H[{Math.Round(drone.StructurePercent)}] Id[{drone.MaskedDroneId}]");
                UpdateDroneStateForDeployed(drone);
            }

            return true;
        }

        private bool ReturnDrones(List<AbyssalDrone> dronesIWant)
        {
            if (!IsOurShipWithintheAbyssBounds())
                return false;

            if (Combat.PotentialCombatTargets.Any() && !dronesIWant.Any())
            {
                if (DebugConfig.DebugDrones) Log("if (Combat.PotentialCombatTargets.Any() && !dronesIWant.Any())");
                return false;
            }

            if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
            {
                _droneRecallsStage2 = 0;
                return false;
            }

            if (!allDronesInSpace.Any())
                return false;

            var dronesToRecall = allDronesInSpace
                .Where(dis => dronesIWant.All(d => d.DroneId != dis.Id))
                .Where(dis => dis._directEntity.DroneState != (int)Drones.DroneState.Returning && dis._directEntity.DroneState != (int)Drones.DroneState.Returning2)
                .ToList();

            if (!dronesToRecall.Any())
                return false;



            var droneIdsToRecall = dronesToRecall.DistinctBy(x => x.Id).Select(d => d.Id).ToList();

            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
            {
                //4 if no towers, 9 if there are towers killing drones?
                int _droneRecallLimit = 4;
                if (ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.Any())
                    _droneRecallLimit = 9;

                if (_droneRecallsStage2 >= _droneRecallLimit)
                {
                    Log("LuciferSpawn: We want to recall some drones: _droneRecallsStage [" + _droneRecallsStage + "] >= 4 - we should pull all drones so our ship gets aggro instead of drones, also resetting _droneRecallsStage2 to 0");
                    droneIdsToRecall = allDronesInSpace.DistinctBy(x => x.Id).Select(d => d.Id).ToList();
                    //_droneRecallsStage = 0;
                    _droneRecallsStage2 = 0;
                }
            }

            /**
            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
            {
                if (_droneRecallsStage >= 9)
                {
                    Log("LuciferSpawn: We want to recall some drones: _droneRecallsStage [" + _droneRecallsStage + "] >= 4 - we should pull all drones so our ship gets aggro instead of drones, also resetting _droneRecallsStage to 0");
                    droneIdsToRecall = allDronesInSpace.DistinctBy(x => x.Id).Select(d => d.Id).ToList();
                    _droneRecallsStage = 0;
                }
            }
            **/

            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
            {
                //4 if no towers, 9 if there are towers killing drones?
                int _droneRecallLimit = 4;
                if (ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.Any())
                    _droneRecallLimit = 9;

                if (_droneRecallsStage2 >= _droneRecallLimit)
                {
                    Log("LuciferSpawn: We want to recall some drones: _droneRecallsStage2 [" + _droneRecallsStage2 + "] >= 4 - we should pull all drones so our ship gets aggro instead of drones, also resetting _droneRecallsStage to 0");
                    droneIdsToRecall = allDronesInSpace.DistinctBy(x => x.Id).Select(d => d.Id).ToList();
                    _droneRecallsStage2 = 0;
                }
            }

            var droneRecallSuccess = ESCache.Instance.DirectEve.ActiveShip.ReturnDronesToBay(droneIdsToRecall);
            if (!droneRecallSuccess)
                return false;

            if (droneIdsToRecall.Any())
                Time.Instance.LastDronesNeedToBePulledTimeStamp = DateTime.UtcNow;

            _allDronesInSpaceWhichAreNotReturning = new List<EntityCache>();

            Log($"Recalling the following drones:");
            foreach (var drone in dronesToRecall)
            {
                UpdateDroneStateForRecall(drone.Id);
                Log("[" + drone.TypeName + "] " +
                    "S[" + Math.Round(drone.ShieldPct * 100, 0) +
                    "] A[" + Math.Round(drone.ArmorPct * 100, 0) +
                    "] H[" + Math.Round(drone.StructurePct * 100, 0) +
                    "]  @ [" + drone.Nearest1KDistance +
                    "k] Id[" + drone.MaskedId +
                    "] IsLocatedWithinSpeedCloud (3x speed) [" + drone._directEntity.IsLocatedWithinSpeedCloud +
                    "] IsLocatedWithinBioluminescenceCloud (300% signature radius) [" + drone._directEntity.IsLocatedWithinBioluminescenceCloud +
                    "] IsLocatedWithinFilamentCloud (shield boost penalty) [" + drone._directEntity.IsLocatedWithinFilamentCloud +
                    "] IsTooCloseToSmallDeviantAutomataSuppressor (shoots drones!) [" + drone._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor +
                    "] WhatisFollowingMe [" + drone.WhatisFollowingMe +
                    "]");
            }

            if (allDronesInSpace.Any())
            {
                Log($"All drones in space (before the recall):");
                foreach (var drone in allDronesInSpace)
                {
                    Log("[" + drone.TypeName + "] " +
                        "S[" + Math.Round(drone.ShieldPct * 100, 0) +
                        "] A[" + Math.Round(drone.ArmorPct * 100, 0) +
                        "] H[" + Math.Round(drone.StructurePct * 100, 0) +
                        "]  @ [" + drone.Nearest1KDistance +
                        "k] Id[" + drone.MaskedId +
                        "] IsLocatedWithinSpeedCloud (3x speed) [" + drone._directEntity.IsLocatedWithinSpeedCloud +
                        "] IsLocatedWithinBioluminescenceCloud (300% signature radius) [" + drone._directEntity.IsLocatedWithinBioluminescenceCloud +
                        "] IsLocatedWithinFilamentCloud (shield boost penalty) [" + drone._directEntity.IsLocatedWithinFilamentCloud +
                        "] FollowEntity [" + drone._directEntity.FollowEntityName + "]");
                }
            }

            Log($"dronesIWant:");
            foreach (var abyssaldrone in dronesIWant.Where(i => i.ActionState == AbyssalDrone.DroneActionState.InBay))
            {
                Log($"[{abyssaldrone.TypeName}]");
            }
            //
            // what happens if the drone doesnt come back? webbed, incapacitated, in speed cloud and going "too fast" to return properly
            //
            _droneRecallsStage += dronesToRecall.Count;
            _droneRecallsStage2 += dronesToRecall.Count;
            return true;
        }

        private DateTime _lastServerDownDroneRecallStarted = DateTime.MinValue;

        private bool WeHaveEnoughDPSUsingDronesInSpaceToKillStuffEffectively
        {
            get
            {
                var abyssalDronesInSpace = allDronesInSpace.Select(d => new AbyssalDrone(d._directEntity)).ToList();

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                {
                    if (2 > abyssalDronesInSpace.Count(x => x.Bandwidth >= 25))
                    {
                        //Not enough Geckos in space: Do we have Heavy Drones out?
                        if (4 > abyssalDronesInSpace.Count(x => x.Bandwidth >= 15))
                        {
                            //If we dont heave heavies either then we dont have enough DPS
                            return false;
                        }

                        return true;
                    }

                    return true;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                {
                    if (5 > abyssalDronesInSpace.Count(x => x.Bandwidth >= 10))
                    {
                        return false;
                    }

                    return true;
                }

                return false;
            }
        }

        public bool DronesInSpaceAreHealthyEnoughDoNotRiskPullingDrones()
        {
            var abyssalDronesInSpace = allDronesInSpace.Select(d => new AbyssalDrone(d._directEntity)).ToList();

            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
            {
                if (allDronesInSpace.Any() && allDronesInSpace.Where(x => x.TypeName.Contains("Gecko")).Count() >= 2)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains("Skybreaker".ToLower()) ||
                                                               i.Name.ToLower().Contains("Stormbringer".ToLower()) ||
                                                               i.Name.ToLower().Contains("Thunderchild".ToLower())))
                    {
                        if (DirectEve.Interval(5000))
                            Log("Concord Spawn: We have EDENCOM ships in grid: Do not pull drones while they are on grid");

                        return true;
                    }

                    return false;
                }

                return false;
            }


            if (allDronesInSpace.Any() && allDronesInSpace.Count() >= 5)
            {
                if (DebugConfig.DebugDrones) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (allDronesInSpace.Any() && allDronesInSpace.Count() >= 5)");
                if (abyssalDronesInSpace.All(drone => drone.ShieldPercent >= (_droneRecoverShieldPerc[(int)drone.Bandwidth] / 100d)))
                {
                    if (DebugConfig.DebugDrones) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if drones are healthy return true");
                    return true;
                }

                return false;
            }

            return false;
        }

        public bool HandleDroneRecallAndDeployment()
        {
            var droneBay = ESCache.Instance.DirectEve.GetShipsDroneBay();
            if (droneBay == null)
                return false;

            if (!ESCache.Instance.InAbyssalDeadspace && !Combat.PotentialCombatTargets.Any() && !allDronesInSpace.Any())
                return false;

            if (!IsOurShipWithintheAbyssBounds(3500))
                return false;


            var dronesIWant = GetWantedDronesInSpace();

            if (ESCache.Instance.DirectEve.Me.TimeTillDownTime.TotalSeconds <= 40 && ESCache.Instance.DirectEve.Me.TimeTillDownTime.TotalSeconds >= 0 && allDronesInSpace.Any() || _lastServerDownDroneRecallStarted.AddSeconds(45) > DateTime.UtcNow)
            {
                if (DirectEve.Interval(5000))
                {
                    Log($"Server will be DOWN in [{ESCache.Instance.DirectEve.Me.TimeTillDownTime.TotalSeconds}] seconds.");
                }

                var droneAvgDist = allDronesInSpace.Any() ? allDronesInSpace.Average(e => e.Distance) : 0;
                var minDroneSpeed = 2000d;
                if (ESCache.Instance.DirectEve.Me.TimeTillDownTime.TotalSeconds < 15 || ESCache.Instance.DirectEve.Me.TimeTillDownTime.TotalSeconds <= (droneAvgDist / minDroneSpeed) || _lastServerDownDroneRecallStarted.AddSeconds(45) > DateTime.UtcNow)
                {
                    var ds = allDronesInSpace.Where(d => d._directEntity.DroneState != (int)Drones.DroneState.Returning).Select(d => d.Id).ToList();
                    if (ds.Any())
                    {
                        Log($"-- Server will shutdown in less than [{ESCache.Instance.DirectEve.Me.TimeTillDownTime.TotalSeconds}] seconds, recalling all drones.");
                        ESCache.Instance.DirectEve.ActiveShip.ReturnDronesToBay(ds);
                        // keep track of the last recall, because the timer will be gone if it reaches 0
                        _lastServerDownDroneRecallStarted = DateTime.UtcNow;
                        return true;
                    }

                    return false;
                }
            }

            if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
            {
                var droneAvgDist = allDronesInSpace.Any() ? allDronesInSpace.Average(e => e.Distance) : 0;
                //var minDroneSpeed = 2000d;

                var ds = allDronesInSpace.Where(d => d._directEntity.DroneState != (int)Drones.DroneState.Returning).Select(d => d.Id).ToList();
                if (ds.Any())
                {
                    Log($"-- recalling all drones: no NPCs left to kill");
                    _droneRecallsStage2 = 0;
                    ESCache.Instance.DirectEve.ActiveShip.ReturnDronesToBay(ds);
                    return true;
                }

                return false;
            }

            if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
            {
                if (Combat.PotentialCombatTargets.Any(i => i.IsInRangeOfWeapons) && Combat.PotentialCombatTargets.Where(x => x.IsInRangeOfWeapons).OrderBy(i => i._directEntity.AbyssalTargetPriority).FirstOrDefault()._directEntity.IsTargetingDrones)
                {
                    if (DirectEve.Interval(3000))
                        RecallDrones();

                    return false;
                }

                //If the skybreaker is within lightning range of our ship
                if (Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Skybreaker")) && Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Skybreaker") && 10000 > i.Distance))
                {
                    //if the skybreaker is indexer lightning range of our drones! pull them ffs 0 let the skybreaker kill itself with its own lightning
                    if (allDronesInSpace.Any(i => 10000 > i.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.TypeName.Contains("Skybreaker")))))
                    {
                        //If
                        if (DirectEve.Interval(35000))
                            RecallDrones();

                        return false;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship) && !Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i._directEntity.IsInSpeedCloud && i.DistanceTo(_nextGate) > 25000))
                        {
                            Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i._directEntity.IsInSpeedCloud && i.DistanceTo(_nextGate) > 25000)) this is bad - dont use drones is speed cloud is at the edge of the arena!");
                            if (DirectEve.Interval(35000))
                                RecallDrones();

                            if (DirectEve.Interval(15000))
                            {
                                string msg = "Notification: Karen is in a speed cloud far from gate! not deploying drones!";
                                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                                Log(msg);

                                if (PlayNotificationSounds) Util.PlayNoticeSound();
                                if (PlayNotificationSounds) Util.PlayNoticeSound();
                            }

                            return false;
                        }
                    }
                }
            }

            if (DronesInSpaceAreHealthyEnoughDoNotRiskPullingDrones())
                return false;

            if (ReturnDrones(dronesIWant))
                return true;

            if (LaunchDrones(dronesIWant))
                return true;

            return false;
        }

        private Dictionary<long, DateTime> _droneRecallTimers = new Dictionary<long, DateTime>();
        private int _dronesRecalledWhileWeStillhaveTargets = 0;

        private void TrackDroneRecalls()
        {
            if (!ESCache.Instance.InAbyssalDeadspace)
                return;

            var allDronesInSpaceIds = allDronesInSpace.Select(d => d.Id).ToList();
            // we only allow any of the in space drones to be part of the dict
            foreach (var key in _droneRecallTimers.Keys.ToList())
            {
                if (!allDronesInSpaceIds.Any(e => e == key))
                {
                    _droneRecallTimers.Remove(key);
                }
            }

            // add returning drones to the dict
            foreach (var d in allDronesInSpace.Where(e => e._directEntity.DroneState == (int)Drones.DroneState.Returning || e._directEntity.DroneState == (int)Drones.DroneState.Returning2))
            {
                if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                {
                    _dronesRecalledWhileWeStillhaveTargets++;
                }

                if (!_droneRecallTimers.ContainsKey(d.Id))
                {
                    _droneRecallTimers[d.Id] = DateTime.UtcNow;
                }
            }

            if (DirectEve.Interval(30000)) Log("_dronesRecalledWhileWeStillhaveTargets [" + _dronesRecalledWhileWeStillhaveTargets + "]");
        }

        private int DroneReturningSinceSeconds(long droneId)
        {
            if (_droneRecallTimers.ContainsKey(droneId))
            {
                return Math.Abs((int)(DateTime.UtcNow - _droneRecallTimers[droneId]).TotalSeconds);
            }
            return 0;
        }

        private bool ThisSpawnEatsDrones
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsFrigate ||
                    ESCache.Instance.ActiveShip.IsAssaultShip ||
                    ESCache.Instance.ActiveShip.IsDestroyer ||
                    (ESCache.Instance.ActiveShip.IsCruiser && !ESCache.Instance.ActiveShip.IsCruiserWithDroneBonuses))
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                        {
                            if (ESCache.Instance.Weapons.Any())
                                return true;

                            return false;
                        }

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        private bool ManageDrones()
        {
            if (DebugConfig.DebugDrones)
                Log("ManageDrones()");

            if (!ESCache.Instance.Modules.Any())
                return true;

            if (ESCache.Instance.InWarp)
                return true;

            if (!ESCache.Instance.ActiveShip.HasDroneBay)
            {
                if (DebugConfig.DebugDrones)
                    Log("if (!ESCache.Instance.ActiveShip.HasDroneBay)");

                return true;
            }

            if (!ESCache.Instance.InAbyssalDeadspace && !Combat.PotentialCombatTargets.Any())
                return true;

            TrackDroneRecalls();

            // If we are in a single room abyss, don't do anything currently
            if (_singleRoomAbyssal && DebugConfig.DebugFourteenBattleshipSpawnRunAway)
            {
                if (allDronesInSpace.Any())
                {
                    if (RecallDrones())
                        return true;
                }
                return false;
            }

            // If drones are outside of the bounds, recall them.
            if (!AreDronesWithinAbyssBounds)
            {
                if (RecallDronesOutsideAbyss())
                    return true;
            }

            if (ThisSpawnEatsDrones && _droneRecallsStage2 > 4)
            {
                if (allDronesInSpace.Any())
                {
                    if (DirectEve.Interval(90000)) _droneRecallsStage2 = 0;

                    if (RecallDrones())
                        return true;
                }

                return false;
            }

            if (DirectEve.Interval(5000) && Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate) && _droneRecallsDueEnemiesBeingInASpeedCloud < 2 && !ESCache.Instance.ActiveShip.IsLocatedWithinSpeedCloud && AreAllOurDronesInASpeedCloud && _nextDroneRecallDueEnemiesBeingInASpeedCloud < DateTime.UtcNow)
            {
                //
                // we should make sure we are moving to a spot that isnt near the speed cloud
                //
                if (DirectEve.Interval(10000))
                {
                    _tooManyEnemiesAreStillInASpeedCloudCount++;
                }

                if (_tooManyEnemiesAreStillInASpeedCloudCount > 4)
                {
                    _droneRecallsDueEnemiesBeingInASpeedCloud++;
                    _nextDroneRecallDueEnemiesBeingInASpeedCloud = DateTime.UtcNow.AddSeconds(Rnd.Next(35, 45));
                    Log($"-- All frigate targets are in a speed cloud and we aren't, recalling drones. _tooManyEnemiesAreStillInASpeedCloudCount [" + _tooManyEnemiesAreStillInASpeedCloudCount + "] Amount of recalls this stage including this recall [{_droneRecallsDueEnemiesBeingInASpeedCloud}]");
                    if (RecallDrones())
                        return true;
                }
            }

            if (DronesEngageTargets())
            {
                if (DebugConfig.DebugDrones) Log("if (DronesEngageTargets())");
                return true;
            }

            if (DebugConfig.DebugDrones) Log("!if (DronesEngageTargets())");

            if (HandleDroneRecallAndDeployment())
            {
                if (DebugConfig.DebugDrones) Log("if (HandleDroneRecallAndDeployment())");
                return true;
            }

            if (DebugConfig.DebugDrones) Log("!if (HandleDroneRecallAndDeployment())");

            // If the mtu was dropped and scooped again, we don't launch the drones again for the remaining cache
            // Maybe still want to do that?
            if (IsOurShipWithintheAbyssBounds() && _mtuAlreadyDroppedDuringThisStage && Combat.PotentialCombatTargets.All(e => e.IsAbyssalDeadspaceTriglavianBioAdaptiveCache || e.IsAbyssalDeadspaceTriglavianExtractionNode || e.IsWreck) && _getMTUInSpace == null)
            {
                if (allDronesInSpace.Any())
                {
                    if (RecallDrones())
                        return true;
                }
            }

            try
            {
                //If drones are attacking an entity that is not a target, recall them all: so NPCs reset their targets to the ship and we are more likely to get the drones to attack correctly.
                if (ESCache.Instance.Targets.Any(i => i.GroupId != (int)Group.AssaultShip) && allDronesInSpace.Any(e => e.FollowId != 0 && e._directEntity.FollowId != ESCache.Instance.ActiveShip.Entity.Id && e._directEntity.FollowEntity != null && !e._directEntity.FollowEntity.IsTarget))
                {
                    try
                    {
                        foreach (EntityCache myWanderingDrone in allDronesInSpace.Where(i => i.FollowId != 0 && i._directEntity.FollowEntity != null && !i._directEntity.FollowEntity.IsTarget && Combat.PotentialCombatTargets.All(t => t.Id != i.FollowId)))
                        {
                            Log("Wandering Drone(s)? [" + myWanderingDrone.TypeName + "][" + myWanderingDrone.Nearest1KDistance + "][" + myWanderingDrone.MaskedId + "] DroneState [" + myWanderingDrone._directEntity.DroneStateName + "] FollowEntity [" + myWanderingDrone._directEntity.FollowEntityName + "]");
                        }
                    }
                    catch (Exception) { }

                    //This should never be needed because we reengage drones that are attacking the wrong targets

                    if (ESCache.Instance.Targets.All(i => i.IsAbyssalBioAdaptiveCache))
                    {
                        if (DirectEve.Interval(7000))
                        {
                            Log("Recalling drones: they are wandering");
                            if (allDronesInSpace.Any())
                            {
                                if (RecallDrones())
                                    return true;
                            }
                        }
                    }
                }
            }
            catch (Exception) { }

            // If drones are outside of the bounds, recall them.
            var areDronesWithinBounds = AreDronesWithinAbyssBounds;
            if (!areDronesWithinBounds)
            {
                try
                {
                    if (DirectEve.Interval(5000))
                        Log($"Drones are not within bounds. Furthest drone distance to center [{allDronesInSpace.OrderByDescending(e => e.Distance).FirstOrDefault()._directEntity.DirectAbsolutePosition.GetDistance(ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition)}]");
                }
                catch (Exception) { }

                if (RecallDrones())
                    return true;
            }

            return false;

        }


        internal bool RecallDrones()
        {
            if (DirectEve.ActiveDrones.Any())
            {
                if (IsOurShipWithintheAbyssBounds())
                {
                    if (DirectEve.Interval(5000, 6500) && allDronesInSpace.All(i => i.Velocity > 4000 && (i._directEntity.DroneState == (int)Drones.DroneState.Returning || i._directEntity.DroneState == (int)Drones.DroneState.Returning2)))
                    {
                        DirectEve.ExecuteCommand(DirectCmd.CmdDronesReturnAndOrbit);
                        return true;
                    }

                    if (DirectEve.Interval(2500, 3500))
                    {
                        if (DirectEve.ActiveShip.ReturnDronesToBay(DirectEve.ActiveDrones.Where(e => e.DroneState != (int)Drones.DroneState.Returning && e.DroneState != (int)Drones.DroneState.Returning2)
                            .Select(e => e.Id).ToList()))
                        {
                            Time.Instance.LastDronesNeedToBePulledTimeStamp = DateTime.UtcNow;
                            Log($"Calling non returning drones to return to the bay.");
                            _startedToRecallDronesWhileNoTargetsLeft = null;
                            return true;
                        }
                    }

                    int intcount = 0;
                    foreach (var mydrone in DirectEve.ActiveDrones.OrderByDescending(i => i.Volume))
                    {
                        intcount++;
                        Log("[" + intcount + "][" + mydrone.TypeName + "][" + Math.Round(mydrone.Velocity, 0) + "m/s] at [" + Math.Round(mydrone.Distance/1000, 0) + "k] [" + mydrone.DroneStateName + "] S[" + Math.Round(mydrone.ShieldPct) + "] A[" + Math.Round(mydrone.ArmorPct) + "] H[" + Math.Round(mydrone.StructurePct) + "]");
                    }
                }

                return false;
            }

            return true;
        }

        internal bool RecallDronesOutsideAbyss()
        {
            if (DirectEve.ActiveDrones.Any() && Framework.Me.IsInAbyssalSpace() && !AreDronesWithinAbyssBounds)
            {
                if (DirectEve.Interval(2500, 3500))
                {
                    var dronesToRecall = DirectEve.ActiveDrones.Where(e => e.DroneState != 4 && !IsSpotWithinAbyssalBounds(e.DirectAbsolutePosition)).ToList();

                    foreach (var recalledDrone in dronesToRecall.ToList())
                    {
                        if (recalledDrone.FollowEntity != null)
                        {
                            if (IsSpotWithinAbyssalBounds(recalledDrone.FollowEntity.DirectAbsolutePosition))
                            {
                                dronesToRecall.Remove(recalledDrone);
                            }
                        }
                    }

                    if (DirectEve.ActiveShip.ReturnDronesToBay(dronesToRecall.Select(e => e.Id).ToList()))
                    {
                        Log($"Drones are not within bounds. Recalling. Furthest drone distance to center [{allDronesInSpace.OrderByDescending(e => e.Distance).FirstOrDefault()._directEntity.DirectAbsolutePosition.GetDistance(ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition)}]");
                        _startedToRecallDronesWhileNoTargetsLeft = null;
                        return true;
                    }
                }
            }
            return false;
        }

        private bool _toggle;

        private int _droneEngageCount;

        private List<long> _currentTargetsStage { get; set; } = new List<long>();

        private bool SplitDronesIntoTwoGroups(EntityCache target1, EntityCache target2, List<EntityCache> dronesToUse)
        {
            //
            // reasons to not split drones
            //
            if (ESCache.Instance.ActiveShip.IsFrigate)
                return false;

            if (ESCache.Instance.ActiveShip.IsAssaultShip)
                return false;

            if (ESCache.Instance.ActiveShip.IsDestroyer)
                return false;

            if (ESCache.Instance.ActiveShip.IsCruiser)
            {
                if (!ESCache.Instance.ActiveShip.IsCruiserWithDroneBonuses)
                {
                    //Ishtar and Gila both have bonused drones that can handle being split up and not waste DPS, unbonused drones probably should not do that
                    return false;
                }
            }

            if (target1 == null || target2 == null)
            {
                return false;
            }

            var t2 = new List<EntityCache> { target1, target2 };

            if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
            {
                if (t2.Any(e => e.TypeName.ToLower().Contains("Lucifer Cynabal".ToLower())))
                {
                    if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.TypeName.ToLower().Contains(Lucifer Cynabal))) false");
                    return false;
                }

                if (t2.Any(e => e.TypeName.ToLower().Contains("Rodiva".ToLower())))
                {
                    if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.TypeName.ToLower().Contains(Rodiva))) false");
                    return false;
                }

                if (t2.Any(e => e.TypeName.ToLower().Contains("Devoted Knight".ToLower())))
                {
                    if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.TypeName.ToLower().Contains(Devoted Knight))) false");
                    return false;
                }
            }


            if (t2.Any(e => e.TypeName.ToLower().Contains("kikimora".ToLower())))
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.TypeName.ToLower().Contains(kikimora))) false");
                return false;
            }

            if (t2.Any(e => e.TypeName.ToLower().Contains("Harrowing Vedmak".ToLower())))
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.TypeName.ToLower().Contains(Harrowing Vedmak))) false");
                return false;
            }

            if (t2.Any(e => e.TypeName.ToLower().Contains("Starving Vedmak".ToLower())))
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.TypeName.ToLower().Contains(Starving Vedmak))) false");
                return false;
            }

            if (t2.Any(e => e.TypeName.ToLower().Contains("Tessera".ToLower())))
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.TypeName.ToLower().Contains(Tessera))) false");
                return false;
            }

            if (t2.Any(e => e.TypeName.ToLower().Contains("Skybreaker".ToLower())))
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.TypeName.ToLower().Contains(Skybreaker.ToLower())))");
                return false;
            }

            if (t2.Any(e => e.TypeName.ToLower().Contains("Stormbringer".ToLower())))
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.TypeName.ToLower().Contains(Stormbringer.ToLower())))");
                return false;
            }

            if (t2.Any(e => e.TypeName.ToLower().Contains("Thunderchild".ToLower())))
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.TypeName.ToLower().Contains(Thunderchild.ToLower())))");
                return false;
            }

            if (t2.Any(e => e.TypeName.ToLower().Contains("Marshal Disparu Troop".ToLower())))
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.TypeName.ToLower().Contains(Marshal Disparu Troop.ToLower())))");
                return false;
            }

            if (t2.Any(e => e.TypeName.ToLower().Contains("Lucid Deepwatcher".ToLower())))
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.TypeName.ToLower().Contains(Lucid Deepwatcher.ToLower())))");
                return false;
            }

            //if (t2.Any(e => e.IsAbyssalLeshak))
            //{
            //    if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.IsAbyssalLeshak)) false");
            //    return false;
            //}

            if (t2.Any(e => e._directEntity.IsAbyssalMarshal))
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.IsAbyssalMarshal)) false");
                return false;
            }

            //if (Combat.PotentialCombatTargets.Count(e => e.IsNPCFrigate && e._directEntity.IsRemoteRepairEntity) >= 3)
            //{
            //    if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (Combat.PotentialCombatTargets.Count(e => e.IsNPCFrigate && e._directEntity.IsRemoteRepairEntity) >= 3)");
            //    return false;
            //}

            //if (Combat.PotentialCombatTargets.Count(e => e.IsNPCDestroyer && e._directEntity.IsRemoteRepairEntity) >= 3)
            //{
            //    if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (Combat.PotentialCombatTargets.Count(e => e.IsNPCDestroyer && e._directEntity.IsRemoteRepairEntity) >= 3)");
            //    return false;
            //}

            //if (Combat.PotentialCombatTargets.Count(e => e.IsNPCCruiser && e._directEntity.IsRemoteRepairEntity) >= 3)
            //{
            //    if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (Combat.PotentialCombatTargets.Count(e => e.IsNPCCruiser && e._directEntity.IsRemoteRepairEntity) >= 3)");
            //    return false;
            //}

            if (2 > dronesToUse.Count)
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (2 > dronesToUse.Count) false");
                return false;
            }

            //
            // reasons to split drones
            //
            if (t2.Any(e => e._directEntity.IsAbyssalKaren) && t2.Count(i => i.IsNPCCruiser) > 1)
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e._directEntity.IsAbyssalKaren) && t2.Any(i => i.IsNPCCruiser)) true");
                return true;
            }

            if (t2.Any(e => e._directEntity.IsNPCBattleship) && t2.Count(i => i.TypeName.Contains("Leshak")) > 1)
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e._directEntity.IsBattleship) && t2.Any(i => i.TypeName.Contains(Leshak))) true");
                return true;
            }

            if (t2.All(e => e.IsNPCFrigate))
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.All(e => e.IsNPCFrigate)) true");
                return true;
            }

            if (t2.Count(e => e.IsNPCCruiser && e.TypeName.Contains("Ephialtes ")) > 1)
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.IsNPCCruiser)) true");
                return true;
            }

            if (t2.Count(e => e.IsNPCCruiser && e.Name.Contains("Scylla Tyrannos")) > 1)
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.IsNPCCruiser)) true");
                return true;
            }

            if (t2.All(e => e.IsNPCDestroyer))
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.All(e => e.IsNPCDestroyer)) true");
                return true;
            }

            if (t2.Any(e => e.IsNPCFrigate) && t2.Any(e => e.IsNPCDestroyer))
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.IsNPCFrigate) && t2.Any(e => e.IsNPCDestroyer)) true");
                return true;
            }

            if (t2.Any(e => e.IsNPCDestroyer) && t2.Any(e => e.IsNPCCruiser))
            {
                if (DebugConfig.DebugDrones) Log("SplitDronesIntoTwoGroups: if (t2.Any(e => e.IsNPCDestroyer) && t2.Any(e => e.IsNPCCruiser)) true");
                return true;
            }

            return false;
        }

        private DateTime _nextCurrentDroneTargetClear;

        private List<EntityCache> _allDronesInSpaceWhichAreNotReturning = new List<EntityCache>();
        private List<EntityCache> allDronesInSpaceWhichAreNotReturning
        {
            get
            {
                //if (_allDronesInSpaceWhichAreNotReturning.Any())
                //    return _allDronesInSpaceWhichAreNotReturning;

                if (allDronesInSpace.Any(i => i._directEntity.DroneState != (int)Drones.DroneState.Returning && i._directEntity.DroneState != (int)Drones.DroneState.Returning2 && ListOfDronesKillingCaches.All(x => x.Id != i.Id)))  // all drones which are not returning
                {

                    _allDronesInSpaceWhichAreNotReturning =  allDronesInSpace.Where(i => i._directEntity.DroneState != (int)Drones.DroneState.Returning && i._directEntity.DroneState != (int)Drones.DroneState.Returning2 && ListOfDronesKillingCaches.All(x => x.Id != i.Id)).OrderBy(d => d.Id).ToList();
                    return _allDronesInSpaceWhichAreNotReturning;
                }

                return new List<EntityCache>();
            }
        }
        private EntityCache smallestDroneInSpaceWhichIsNotReturning
        {
            get
            {
                if (smallestDronesInSpaceWhichAreNotReturning.Any())
                {
                    return smallestDronesInSpaceWhichAreNotReturning.OrderBy(d => d._directEntity.BandwidthUsed).FirstOrDefault();
                }

                return null;
            }
        }

        private List <EntityCache> smallestDronesInSpaceWhichAreNotReturning
        {
            get
            {
                if (allDronesInSpace.Any(i => i._directEntity.DroneState != (int)Drones.DroneState.Returning && i._directEntity.DroneState != (int)Drones.DroneState.Returning2))  // all drones which are not returning
                {
                    //If no NPCs use any drone
                    if (!Combat.PotentialCombatTargets.Any(pct => pct.BracketType != BracketType.Large_Collidable_Structure))
                    {
                        allDronesInSpaceWhichAreNotReturning.OrderBy(d => d._directEntity.BandwidthUsed)
                                                            .ToList();
                    }

                    //If NPCs use any medium or small drone
                    if (allDronesInSpace.Where(i => i._directEntity.DroneState != (int)Drones.DroneState.Returning && i._directEntity.DroneState != (int)Drones.DroneState.Returning2).Any(x => x.TypeId != (int)TypeID.Gecko))
                    {
                        return allDronesInSpace.Where(i => i._directEntity.DroneState != (int)Drones.DroneState.Returning && i._directEntity.DroneState != (int)Drones.DroneState.Returning2 && i.TypeId != (int)TypeID.Gecko)
                                                                    .OrderBy(d => d._directEntity.BandwidthUsed)
                                                                    .ToList();
                    }

                    return new List<EntityCache>();
                }

                return new List<EntityCache>();
            }
        }

        private List<EntityCache> ListOfDronesKillingCaches = new List<EntityCache>();

        private bool IsItSafeToKillCachesRightNow
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Kikimora")))
                            return false;

                        return true;
                    }

                    return true;
                }

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Gila)
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Kikimora")))
                            return false;

                        return true;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Vedmak")))
                            return false;

                        return true;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship || i.IsNPCCruiser))
                            return false;

                        return true;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.TypeName.ToLower().Contains("Devoted Knight".ToLower())))
                            return false;

                        return true;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.TypeName.ToLower().Contains("Lucifer Cynabal".ToLower())))
                            return false;

                        return true;
                    }

                    return true;
                }

                return true;
            }
        }

        private static long _group1OrSingleTargetId = -1;
        private static long _group2TargetId = -1;

        private DirectEntity _groupTarget1OrSingleTarget => Framework.EntitiesById.ContainsKey(_group1OrSingleTargetId) ? Framework.EntitiesById[_group1OrSingleTargetId] : null;
        private DirectEntity _groupTarget2 => Framework.EntitiesById.ContainsKey(_group2TargetId) ? Framework.EntitiesById[_group2TargetId] : null;

        private bool UseSmallDronesOnCaches()
        {
            if (!IsItSafeToKillCachesRightNow)
                return false;

            if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila && ESCache.Instance.Weapons.Any(i => i.Charge != null))
                return false;

            ListOfDronesKillingCaches.Clear();

            if (ESCache.Instance.Targets.Any(e => e.GroupId != (int)Group.AssaultShip && (e.IsAbyssalBioAdaptiveCache || e.IsAbyssalDeadspaceTriglavianExtractionNode) && e.Distance < ESCache.Instance.ActiveShip.GetDroneControlRange()) && allDronesInSpaceWhichAreNotReturning.Count > 0)
            {
                if (DebugConfig.DebugDrones)
                    Log("DronesEngageTargets: we have a AbyssalPrecursorCache to shoot");

                if (smallestDroneInSpaceWhichIsNotReturning != null)
                {
                    ListOfDronesKillingCaches.Add(smallestDroneInSpaceWhichIsNotReturning);
                    if (ListOfDronesKillingCaches.Any(i => i._directEntity.DroneState == (int)Drones.DroneState.Attacking && i._directEntity.FollowEntity != null && i._directEntity.FollowEntity.IsAbyssalBioAdaptiveCache))
                    {
                        return false;
                    }
                }

                //dronesToUse = largeMedDronesInSpace.Where(i => i.DroneState != 4).OrderBy(d => d.Id).ToList();

                var caches = ESCache.Instance.Targets.Where(e => e.GroupId != (int)Group.AssaultShip && (e.IsAbyssalBioAdaptiveCache || e.IsAbyssalDeadspaceTriglavianExtractionNode) && !e.IsWreck && (e.Distance <= ESCache.Instance.ActiveShip.GetDroneControlRange()))
                    .OrderBy(e => DirectEntity.AnyIntersectionAtThisPosition(e._directEntity.DirectAbsolutePosition, false,
                        true, true, true, true, true, false).Count()) // pref caches in non speed clouds first
                    .ThenBy(e => e.Id);
                var cacheIds = caches.Select(e => e.Id).OrderByDescending(e => e);
                if (DirectEve.Interval(1500, 2000))
                {
                    _droneEngageCount = 0;
                    foreach (var cache in caches)
                    {
                        // is any drone already targeting this cache
                        if (smallestDronesInSpaceWhichAreNotReturning.Any(e => e.FollowId == cache.Id))
                            continue;

                        // get a drone which is NOT targeting a cache
                        var unusedDrone = smallestDronesInSpaceWhichAreNotReturning.FirstOrDefault(e => !cacheIds.Contains(e.FollowId));

                        // if there is none left pick one of a group which has the same follow id
                        if (unusedDrone == null)
                        {
                            var followIds = smallestDronesInSpaceWhichAreNotReturning.Where(e => e.FollowId > 0).Select(e => e.FollowId);
                            foreach (var id in followIds)
                            {
                                if (smallestDronesInSpaceWhichAreNotReturning.Count(e => e.FollowId == id) > 1)
                                {
                                    unusedDrone = smallestDronesInSpaceWhichAreNotReturning.FirstOrDefault(e => e.FollowId == id);
                                    break;
                                }
                            }
                        }
                        // pwn that thing
                        if (unusedDrone != null)
                        {
                            if (DirectEve.Interval(1800, 2200))
                            {
                                if (cache._directEntity.EngageTargetWithDrones(new List<long>() { unusedDrone.Id }))
                                {
                                    _droneEngageCount++;
                                    Log($"Engaging [{cache.TypeName}]@[{cache.Nearest1KDistance}k] with [{unusedDrone.TypeName}]@[{unusedDrone.Nearest1KDistance}k]");
                                    return false; //we do this so that we will engage the other drones without waiting for the next pulse
                                }
                            }
                        }
                    }
                }
            }

            return true;
        }

        private bool DronesEngageTargets()
        {

            if (DebugConfig.DebugDrones || DirectEve.Interval(30000))
            {
                foreach (var mydrone in allDronesInSpace)
                {
                    Log("allDronesInSpace [" + mydrone.TypeName + "] S[" + Math.Round(mydrone.ShieldPct * 100, 0) + "] A[" + Math.Round(mydrone.ArmorPct * 100, 0) + " %] H[" + Math.Round(mydrone.StructurePct * 100, 0) + " %] DroneState [" + mydrone._directEntity.DroneStateName + "] FollowEntity [" + mydrone._directEntity.FollowEntityName + "][" + Math.Round(mydrone.Distance / 1000, 0) + "k away] IsInSpeedCloud [" + mydrone._directEntity.IsInSpeedCloud + "] WhatisFollowingMe [" + mydrone.WhatisFollowingMe + "]");
                }
            }

            if (ESCache.Instance.InAbyssalDeadspace)
            {
                foreach (var id in _currentTargetsStage.ToList())
                {
                    if (!DirectEve.EntitiesById.ContainsKey(id))
                    {
                        _currentTargetsStage.Remove(id);
                    }
                }
            }

            if (allDronesInSpace == null)
                return false;

            if (DebugConfig.DebugDrones)
                Log("if (allDronesInSpace != null)");

            if (!allDronesInSpace.Any())
                return false;

            if (DebugConfig.DebugDrones)
                Log("We have [" + allDronesInSpace.Count() + "] drones in space");


            if (!allDronesInSpaceWhichAreNotReturning.Any())
                return false;

            if (DebugConfig.DebugDrones)
            {
                foreach (var mydrone in allDronesInSpaceWhichAreNotReturning)
                {
                    Log("dronesToUse [" + mydrone.TypeName + "] S[" + Math.Round(mydrone.ShieldPct * 100, 0) + "] A[" + Math.Round(mydrone.ArmorPct* 100, 0) + "%] H[" + Math.Round(mydrone.StructurePct * 100, 0) + "%] DroneState [" + mydrone._directEntity.DroneStateName + "] FollowEntity [" + mydrone._directEntity.FollowEntityName + "][" + Math.Round(mydrone.Distance / 1000, 0) + "k away]");
                }
            }

            //
            // dont allow geckos to kill caches unless all NPCs are dead
            //

            if (ESCache.Instance.InAbyssalDeadspace)
            {
                if (_nextCurrentDroneTargetClear < DateTime.UtcNow)
                {
                    _currentTargetsStage = new List<long>();
                    _nextCurrentDroneTargetClear = DateTime.UtcNow.AddSeconds(Rnd.Next(8, 16));
                }

                // use small drones on caches
                UseSmallDronesOnCaches();
            }

            // engage target
            if (DirectEve.Interval(800, 1200) && ESCache.Instance.Targets.Where(e => e.GroupId != (int)Group.AssaultShip && !e.IsAbyssalDeadspaceTriglavianBioAdaptiveCache && !e.IsAbyssalDeadspaceTriglavianExtractionNode && !e.IsWreck).Any())
            {
                if (DebugConfig.DebugDrones)
                    Log("DronesEngageTargets: we have a target to shoot");
                var group1 = allDronesInSpaceWhichAreNotReturning.DistinctBy(e => e._directEntity.TryGet<double>("droneBandwidthUsed")).ToList();
                var group2 = allDronesInSpaceWhichAreNotReturning.Except(group1).ToList();

                //if (!group2.Any()) // edge case, if all drones use the same bandwidth
                //{
                //    group1 = dronesToUse.Take(2);
                //    group2 = dronesToUse.Skip(2);
                //}

                //if (!group2.Any()) // additional case for the gila
                //{
                //    group1 = dronesToUse.Take(1);
                //    group2 = dronesToUse.Skip(1);
                //}

                while (group1.Count() - group2.Count() > 1)
                {
                    var swap = group1.Last();
                    group2.Append(swap);
                    group1.Remove(swap);
                }

                while (group2.Count() - group1.Count() > 1)
                {
                    var swap = group2.Last();
                    group1.Append(swap);
                    group2.Remove(swap);
                }

                //
                // take the drone groups defined above and pick a target to have each drone group kill
                //
                var target1 = GetSortedTargetList(ESCache.Instance.Targets.Where(e => e.GroupId != (int)Group.AssaultShip && e.Distance < ESCache.Instance.ActiveShip.GetDroneControlRange() && !e.IsAbyssalDeadspaceTriglavianExtractionNode && !e.IsWreck)).OrderByDescending(x => !x.IsAbyssalDeadspaceTriglavianBioAdaptiveCache).Take(1).FirstOrDefault();
                var target2 = GetSortedTargetList(ESCache.Instance.Targets.Where(e => e.GroupId != (int)Group.AssaultShip && e.Distance < ESCache.Instance.ActiveShip.GetDroneControlRange() && !e.IsAbyssalDeadspaceTriglavianExtractionNode && !e.IsWreck)).OrderByDescending(x => !x.IsAbyssalDeadspaceTriglavianBioAdaptiveCache).Skip(1).Take(1).FirstOrDefault();

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Scylla Tyrannos")))
                    {
                        target1 = GetSortedTargetList(ESCache.Instance.Targets.Where(e => e.GroupId != (int)Group.AssaultShip && e.Distance < ESCache.Instance.ActiveShip.GetDroneControlRange() && !e.IsAbyssalDeadspaceTriglavianExtractionNode && !e.IsWreck)).OrderByDescending(x => !x.IsAbyssalDeadspaceTriglavianBioAdaptiveCache).Take(1).FirstOrDefault();
                        target2 = GetSortedTargetList(ESCache.Instance.Targets.Where(e => e.GroupId != (int)Group.AssaultShip && e.Distance < ESCache.Instance.ActiveShip.GetDroneControlRange() && !e.IsAbyssalDeadspaceTriglavianExtractionNode && !e.IsWreck)).OrderByDescending(x => !x.IsAbyssalDeadspaceTriglavianBioAdaptiveCache).Skip(1).Take(1).FirstOrDefault();
                    }
                }

                //var t2 = new List<DirectEntity>() { target1, target2 };

                //var targetsTargetingDronesCount = Combat.PotentialCombatTargets.Count(e => e._directEntity.IsTargetingDrones);
                // if the two highest priority targets are frigs or 1x frig/1x cruiser or cruiser/cruiser, split drones in two groups
                if (SplitDronesIntoTwoGroups(target1, target2, allDronesInSpaceWhichAreNotReturning))
                {
                    if (DebugConfig.DebugDrones)
                        Log("SplitDronesIntoTwoGroups: if (SplitDronesIntoTwoGroups(target1, target2, dronesToUse))");

                    //var target1 = targets.Where(e => e.AbyssalTargetPriority == targets.Take(1).First().AbyssalTargetPriority).OrderBy(e => e.DistanceTo(group1.FirstOrDefault())).Take(1).First();
                    //var target2 = targets.Where(e => e.AbyssalTargetPriority == targets.Skip(1).Take(1).First().AbyssalTargetPriority).OrderBy(e => e.DistanceTo(group2.FirstOrDefault())).Take(1).First();

                    // swap targets in case if there is already a member drone of the current group targeting a target of the other group
                    if (group1.Any(e => e.FollowId == target2.Id) || group2.Any(e => e.FollowId == target1.Id))
                    {
                        var tmp = target1;
                        target1 = target2;
                        target2 = tmp;
                    }

                    var asyncDroneId = -1L;
                    // if for whatever reason drones are attacking more than 3 targets and we are currently not attacking a cache
                    var anyDroneAttackingACache = allDronesInSpace.Any(e => e._directEntity.FollowEntity != null && (e._directEntity.FollowEntity.IsAbyssalBioAdaptiveCache || e._directEntity.FollowEntity.IsAbyssalDeadspaceTriglavianExtractionNode));
                    if (!anyDroneAttackingACache)
                    {
                        var distinctFollowIdEntities = allDronesInSpaceWhichAreNotReturning.Where(e => e.FollowId > 0).DistinctBy(e => e.FollowId);
                        if (distinctFollowIdEntities.Count() > 2)
                        {
                            try
                            {
                                foreach (var droneWithIndividualFollowIdEntity in distinctFollowIdEntities)
                                {
                                    if (droneWithIndividualFollowIdEntity._directEntity.FollowEntity != null)
                                    {
                                        var individualFollowIdEntity = droneWithIndividualFollowIdEntity._directEntity.FollowEntity;
                                        if (DirectEve.Interval(10000)) Log("Drones are Attacking: [" + individualFollowIdEntity.TypeName + "][" + Math.Round(individualFollowIdEntity.Distance / 1000, 0) + "k] Priority [" + individualFollowIdEntity.AbyssalTargetPriority + "][" + individualFollowIdEntity.TypeName + "] Target IsInSpeedCloud [" + individualFollowIdEntity.IsInSpeedCloud + "]");
                                    }
                                }
                            }
                            catch (Exception){}

                            var activeDronesWithAFollowId = allDronesInSpaceWhichAreNotReturning.Where(e => e.FollowId > 0).Select(e => e.FollowId);
                            var min = distinctFollowIdEntities.OrderBy(e => activeDronesWithAFollowId.Count(x => x == e.FollowId)).FirstOrDefault();
                            // remove the third follow id from the list
                            var followIdsCurrentActiveDronesWithoutLowest = activeDronesWithAFollowId.Except(new List<long>() { min.FollowId }).ToList();
                            // now there are 2 follow id's left, pin them as the current targets 1,2
                            if (followIdsCurrentActiveDronesWithoutLowest.Count() >= 2)
                            {
                                if (ESCache.Instance.DirectEve.EntitiesById.ContainsKey(followIdsCurrentActiveDronesWithoutLowest[0])
                                    && ESCache.Instance.DirectEve.EntitiesById.ContainsKey(followIdsCurrentActiveDronesWithoutLowest[1]))
                                {
                                    if (min != null)
                                    {
                                        // order does not matter, as it's fine if the drones from the least used group attack a target of ANY of the other groups
                                        target1 = ESCache.Instance.EntityById(followIdsCurrentActiveDronesWithoutLowest[0]);
                                        target2 = ESCache.Instance.EntityById(followIdsCurrentActiveDronesWithoutLowest[1]);
                                        asyncDroneId = min.Id;
                                        Log($"-- Drones are attacking more than 3 targets, forcing [{min.TypeName}] ID[{min.Id}] to be again part of the corresponding group.");
                                    }
                                }

                            }
                        }
                    }

                    //Log($"---------- Group 1 information START ---------- Target TypeName [{target1.TypeName}] Id [{target1.Id}]");
                    //foreach (var item in group1)
                    //{
                    //    Log($"G1 -|- Id [{item.Id}] TypeName [{item.TypeName}] DroneState [{item.DroneState}] FollowId [{item.FollowId}]");
                    //}
                    //Log($"---------- Group 1 information END ----------");

                    //Log($"---------- Group 2 information START ---------- TypeName [{target2.TypeName}] Target Id [{target2.Id}]");
                    //foreach (var item in group2)
                    //{
                    //    Log($"G2 -|- Id [{item.Id}] TypeName [{item.TypeName}] DroneState [{item.DroneState}] FollowId [{item.FollowId}]");
                    //}
                    //Log($"---------- Group 2 information END ----------");

                    int t = _toggle ? 1 : 2; // Toggle between group 1 and 2, it works without that, but drone followIds take some time to update, so it should be faster if we toggle between g1 and g2
                    _toggle = !_toggle;

                    for (int i = t; i <= 2; i++)
                    {
                        var currentGroup = i == 1 ? group1 : group2;
                        var newTarget = i == 1 ? target1 : target2;

                        // make sure every drone of the group is targeting the target
                        var dronesToAttack = new List<DirectEntity>();
                        _droneEngageCount = 0;
                        foreach (var drone in currentGroup)
                        {
                            try
                            {
                                if (drone._directEntity.DroneState == (int)Drones.DroneState.Returning || drone._directEntity.DroneState == (int)Drones.DroneState.Returning2) // exclude returning drones
                                    continue;

                                if (newTarget != null && drone.FollowId == newTarget.Id)
                                    continue;

                                if (_currentTargetsStage != null && _currentTargetsStage.Any())
                                {
                                    if (_currentTargetsStage.Contains(drone.FollowId))
                                    {
                                        if (drone._directEntity.DroneState != (int)Drones.DroneState.Idle && drone.Id != asyncDroneId) // if the drone is already targeting the target, skip it)
                                            continue;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Log("Exception [" + ex + "]");
                            }

                            //Log($"G{i}Engage drone Id [{drone.Id}] TypeName [{drone.TypeName}] FollowId [{drone.FollowId}] on target Id [{currentTarget.Id}] TypeName [{currentTarget.TypeName}]");

                            if (DebugConfig.DebugDrones)
                                Log("SplitDronesIntoTwoGroups: dronesToAttack.Add(drone._directEntity);");

                            // only engage with non returning drones
                            dronesToAttack.Add(drone._directEntity);
                        }

                        if (dronesToAttack.Any()) // launch all of each group
                        {
                            if (DebugConfig.DebugDrones)
                                Log("SplitDronesIntoTwoGroups: if (dronesToAttack.Any()) // launch all of each group");

                            var currentTarget = group1 == currentGroup ? _groupTarget1OrSingleTarget : _groupTarget2;
                            var ignore = IgnoreTargetSwap(currentTarget, newTarget, currentGroup) && asyncDroneId == -1L;
                            if (ignore)
                            {
                                Log($"(GROUP) Ignoring to swap drone target. Current target [{currentTarget.TypeName}] Prio [{currentTarget.AbyssalTargetPriority}] New Target [{newTarget.TypeName}] Prio [{newTarget._directEntity.AbyssalTargetPriority}]");
                            }

                            try
                            {
                                if (!ignore && newTarget != null && dronesToAttack != null && newTarget._directEntity.EngageTargetWithDrones(dronesToAttack.Select(e => e.Id).ToList()))
                                {
                                    if (currentGroup == group1)
                                    {
                                        _group1OrSingleTargetId = newTarget.Id;
                                    }
                                    else
                                    {
                                        _group2TargetId = newTarget.Id;
                                    }

                                    _droneEngageCount++;
                                    Log($"Engaging [" + dronesToAttack.Count() + "] Drones in DroneGroup[" + i + "] on [" + newTarget.TypeName + "] Priority [" + newTarget._directEntity.AbyssalTargetPriority + "]@[" + Math.Round(newTarget.Distance / 1000, 0) + "k][" + newTarget.MaskedId + "]"); //Priority [{currentTarget.AbyssalTargetPriority}]");
                                    _currentTargetsStage.Add(newTarget.Id);
                                    return true;
                                }
                            }
                            catch (Exception ex)
                            {
                                Log("Exception [" + ex + "]");
                            }
                        }
                    }
                }
                else
                {
                    if (DebugConfig.DebugDrones)
                        Log("SplitDronesIntoTwoGroups False");
                    //
                    // pick a target to have the drones kill (all drones in one group)
                    //
                    var target = GetSortedTargetList(ESCache.Instance.Targets.Where(e => e.GroupId != (int)Group.AssaultShip && e.Distance < ESCache.Instance.ActiveShip.GetDroneControlRange() && !e.IsAbyssalDeadspaceTriglavianBioAdaptiveCache && !e.IsAbyssalDeadspaceTriglavianExtractionNode && !e.IsWreck)).FirstOrDefault();
                    if (target != null)
                    {
                        if (DebugConfig.DebugDrones)
                            Log("if (target != null)");

                        _droneEngageCount = 0;
                        var ds = allDronesInSpaceWhichAreNotReturning.Where(d => (d.FollowId != target.Id && !_currentTargetsStage.Contains(d.FollowId)) || d._directEntity.DroneState == (int)Drones.DroneState.Idle);
                        if (ds.Any()) // is there any drone not attacking our current target
                        {
                            if (DebugConfig.DebugDrones)
                                Log("if (ds.Any()) // is there any drone not attacking our current target");

                            if (target._directEntity.EngageTargetWithDrones(ds.Select(d => d.Id).ToList()))
                            {
                                //_targetFirstAttackedWhen[target.Id] = DateTime.UtcNow;
                                // only engage with non returning drones
                                _droneEngageCount++;
                                try
                                {
                                    Log($"Engaging [{ds.Count()}] drones on [{target.TypeName}][{Math.Round(target.Distance / 1000, 0)}][{target._directEntity.AbyssalTargetPriority}][{target.Id}]"); //Priority [{target.AbyssalTargetPriority}]");
                                }
                                catch (Exception ex)
                                {
                                    Log(ex.ToString());
                                }

                                _currentTargetsStage.Add(target.Id);
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }
        private bool IgnoreTargetSwap(DirectEntity oldTarget, EntityCache newTarget, List<EntityCache> drones)
        {
            try
            {
                if (newTarget == null)
                    return false;

                if (oldTarget == null)
                    return false;

                if (oldTarget.Id == newTarget.Id)
                    return false;


                // Priority check
                // Reminder: The drone state can fail with auto attack settings ON after they were assigned to a new target after one was killed.
                bool dronesFocusingSingleTarget = drones.Where(e => e._directEntity.DroneState != 4).All(e => e._directEntity.DroneState == 1) && drones.Where(e => e._directEntity.DroneState == 1).Where(e => e.FollowId > 0).Select(e => e.FollowId).Distinct().Count() == 1;
                bool ignoreNewTarget = oldTarget.AbyssalTargetPriority <= newTarget._directEntity.AbyssalTargetPriority && dronesFocusingSingleTarget;

                // Old target conditions
                var secondsToKillCurrentTargetWithDrones = oldTarget.GetSecondsToKillWithActiveDrones(drones);
                var hasCurrentTargetHighLocalReps = oldTarget.FlatShieldArmorLocalRepairAmountCombined >= 10;

                // New target conditions
                var timeForDronesToGetToTheNewTarget = drones.Count == 0 ? int.MaxValue : (drones.Sum(d => d.DistanceTo(newTarget)) / drones.Count) / 1500;

                Log($"NewTarget [{newTarget.TypeName}] Id [{newTarget.Id}] OldTarget [{oldTarget.TypeName}] Id [{oldTarget.Id}] secondsToKillThatTargetWithDrones [{secondsToKillCurrentTargetWithDrones}] timeForDronesToGetToTheTarget [{timeForDronesToGetToTheNewTarget}] hasTargetHighLocalReps [{hasCurrentTargetHighLocalReps}]");
                ignoreNewTarget = ignoreNewTarget || secondsToKillCurrentTargetWithDrones <= 39 || hasCurrentTargetHighLocalReps || timeForDronesToGetToTheNewTarget > secondsToKillCurrentTargetWithDrones;
                return ignoreNewTarget;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }
    }
}
