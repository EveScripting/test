//
// (c) duketwo 2022
//

extern alias SC;
using EVESharpCore.Cache;
using EVESharpCore.Framework;
using SC::SharedComponents.Utility;
//using ServiceStack.OrmLite;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE.DatabaseSchemas;
using SC::SharedComponents.Extensions;
using SharpDX.Direct2D1;
using SC::SharedComponents.EVE;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Framework.Events;
using EVESharpCore.Framework.Lookup;
using System.Xml;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using System.Linq.Expressions;
using System.ComponentModel;
using System.Windows.Controls;
using EVESharpCore.Questor.BackgroundTasks;
using System.Windows.Documents;
using SC::SharedComponents.IPC;
using SC::SharedComponents.Events;
using System.Drawing.Text;

namespace EVESharpCore.Controllers.Abyssal
{

    enum MoveDirection
    {
        None,
        TowardsEnemy,
        AwayFromEnemy,
        Gate
    }

    public partial class AbyssalController : AbyssalBaseController
    {
        private DirectWorldPosition _moveToOverride = null;
        private DateTime _lastMoveToOverride = DateTime.MinValue;

        private Vec3? _moveBackwardsDirection { get; set; } = null;
        private Vec3? _moveTestDirection { get; set; } = null;
        //private Vec3? _moveTowardGateHighTransversal { get; set; } = null;
        //private Vec3? _moveBackwardsDirectionHighTransversal = null;

        private Vec3? __moveUp;
        private Vec3 _moveUp
        {
            get
            {
                if (!__moveUp.HasValue)
                {
                    //var direction = _nextGate._directEntity.DirectAbsolutePosition.GetDirectionalVectorTo(_activeShipPos)
                    // pick spot Below at 100k and find the directional vector back up to our ship and that will be the vector used
                    DirectWorldPosition Spot100kBelowOurShip = null;
                    Spot100kBelowOurShip = new DirectWorldPosition((Vec3)ESCache.Instance.ActiveShip.Entity.PointInSpaceDirectlyDown);
                    var direction = Spot100kBelowOurShip.GetDirectionalVectorTo(_activeShipPos)
                        .Normalize(); // go UP!
                    __moveUp = DirectSceneManager.RotateVector(direction, 5);
                }

                return (Vec3)__moveUp;
            }
        }

        private Vec3? __moveDown;
        private Vec3 _moveDown
        {
            get
            {
                if (!__moveDown.HasValue)
                {
                    //var direction = _nextGate._directEntity.DirectAbsolutePosition.GetDirectionalVectorTo(_activeShipPos)
                    // pick spot Above at 100k and find the directional vector back down to our ship and that will be the vector used
                    DirectWorldPosition Spot100kAboveOurShip = null;
                    Spot100kAboveOurShip = new DirectWorldPosition((Vec3)ESCache.Instance.ActiveShip.Entity.PointInSpaceDirectlyUp);
                    var direction = Spot100kAboveOurShip.GetDirectionalVectorTo(_activeShipPos)
                        .Normalize(); // go UP!
                    __moveDown = DirectSceneManager.RotateVector(direction, 5);
                }

                return (Vec3)__moveDown;
            }
        }

        //private bool _shipWasCloseToTheBoundaryAndEnemiesInOptimalDuringThisStage = false;
        private bool AreWeCloseToTheAbyssBoundary => !IsOurShipWithintheAbyssBounds(-7500);

        private MoveDirection _moveDirection;

        private int _keepAtRangeDistance = 1000;
        private int _gateMTUOrbitDistance
        {
            get
            {
                if (ESCache.Instance.ActiveShip == null)
                    return 1500;

                if (ESCache.Instance.ActiveShip.Entity == null)
                    return 1500;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Ishtar)
                {
                    return _gateMTUOrbitDistances[Rnd.Next(_gateMTUOrbitDistances.Count)];
                }

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.StormBringer)
                {
                    return _gateMTUOrbitDistances[Rnd.Next(_gateMTUOrbitDistances.Count)];
                }

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Vagabond)
                {
                    return _gateMTUOrbitDistances[Rnd.Next(_gateMTUOrbitDistances.Count)];
                }

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Gila)
                {
                    return 500;
                }

                if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses) //worm, algos, dragoon
                {
                    //Note: Worm does not use an MTU so it does not run into the orbiting too far away and cant scoop MTU issue.
                    return _gateMTUOrbitDistances[Rnd.Next(_gateMTUOrbitDistances.Count)];
                }

                return _gateMTUOrbitDistances[Rnd.Next(_gateMTUOrbitDistances.Count)];
            }
        }

        private int _enemyOrbitDistance = 7500;
        private int _speedCloudDistance = 8000;

        private List<int> _keepAtRangeDistances = new List<int>() { 500, 1500, 2000 };
        private List<int> _gateMTUOrbitDistances = new List<int>() { 1000, 1200, 1500 };
        private List<int> _enemyOrbitDistances = new List<int>() { 7500, 10000 };


        private bool IsNextGateInASpeedCloud => _nextGate != null && _nextGate._directEntity.IsInSpeedCloud;

        private List<DirectEntity> SpeedClouds => Framework.Entities.Where(e => e.IsTachCloud).ToList();

        private bool IsNextGateNearASpeedCloud => _nextGate != null && !SpeedClouds.Any(e => e.DirectAbsolutePosition.GetDistanceSquared(_nextGate._directEntity.DirectAbsolutePosition) < (e.RadiusOverride + _speedCloudDistance) * (e.RadiusOverride + _speedCloudDistance));

        private bool WeHavePotentialCombatTargetsThatArentReadyToShoot
        {
            get
            {
                if (!Combat.PotentialCombatTargets.Any(i => i.Velocity > 0))
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn
                    )
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                            return false;

                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate))
                            return false;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) > 1)
                    {
                        return false;
                    }
                }



                if (Combat.PotentialCombatTargets.Any(i => i.Velocity > 0 && !i.IsTarget))
                {
                    if (DirectEve.Interval(5000)) Log("WeHavePotentialCombatTargetsThatArentReadyToShoot: if (Combat.PotentialCombatTargets.Any(i => i.Velocity > 0 && !i.IsTarget))");
                    return true;
                }

                if (ESCache.Instance.ActiveShip.IsCruiserWithDroneBonuses ||
                    ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.Distance > Math.Min(ESCache.Instance.ActiveShip.GetDroneControlRange(), ESCache.Instance.ActiveShip.MaxTargetRange)))
                    {
                        if (AreAllDronesAttacking)
                        {
                            //All drones must be attacking!
                            return false;
                        }

                        //All drones must not be attacking!
                        if (DirectEve.Interval(5000)) Log("WeHavePotentialCombatTargetsThatArentReadyToShoot: We have targets our of range and All drones must not be attacking!");
                        return true;
                    }

                    return false;
                }

                return false;
            }
        }

        private bool boolSpeedTank_IsItSafeToMoveToMTUOrLootWrecks
        {
            get
            {
                int percentageToConsiderTooLow = 50;
                if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                    percentageToConsiderTooLow = 53;

                if (ESCache.Instance.ActiveShip.IsShieldTanked && percentageToConsiderTooLow > ESCache.Instance.ActiveShip.ShieldPercentage)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("percentageToConsiderTooLow [" + percentageToConsiderTooLow + "] > S[" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "%] return false");
                    return false;
                }

                if (ESCache.Instance.ActiveShip.IsArmorTanked && percentageToConsiderTooLow > ESCache.Instance.ActiveShip.ArmorPercentage)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("percentageToConsiderTooLow [" + percentageToConsiderTooLow + "] > A[" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "%] return false");
                    return false;
                }

                if (25 > ESCache.Instance.ActiveShip.CapacitorPercentage)
                {
                    if (ESCache.Instance.ActiveShip.IsShieldTanked && 65 > ESCache.Instance.ActiveShip.ShieldPercentage)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("25 > C[" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 2) + "%] 65 > S[" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "%] return false");
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.IsArmorTanked && 65 > ESCache.Instance.ActiveShip.ArmorPercentage)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("25 > C[" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 2) + "%] 65 > A[" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "%] return false");
                        return false;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)
                    return true;

                if (WeHavePotentialCombatTargetsThatArentReadyToShoot)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (WeHavePotentialCombatTargetsThatArentReadyToShoot [" + WeHavePotentialCombatTargetsThatArentReadyToShoot + "]) return false");
                    return false;
                }

                if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure && !i.IsNPCFrigate))
                {
                    if (ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor != null && 22000 > ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor.DistanceTo(ESCache.Instance.AbyssalGate) && !ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor != null && 22000 > ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor.DistanceTo(ESCache.Instance.AbyssalGate)) return false");
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId != (int)TypeID.Ishtar)
                    {
                        if (ESCache.Instance.AbyssalDeadspaceMediumDeviantAutomataSuppressor != null && 40000 > ESCache.Instance.AbyssalDeadspaceMediumDeviantAutomataSuppressor.DistanceTo(ESCache.Instance.AbyssalGate) && !ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalDeadspaceMediumDeviantAutomataSuppressor != null && 40000 > ESCache.Instance.AbyssalDeadspaceMediumDeviantAutomataSuppressor.DistanceTo(ESCache.Instance.AbyssalGate)) return false");
                            return false;
                        }
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                {
                    if (1 == Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (1 == Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship))");
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser || i.IsNPCFrigate))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser || i.IsNPCFrigate)) return false");
                            return false;
                        }

                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                        {
                            if (Combat.PotentialCombatTargets.All(i => i.IsNPCBattleship))
                            {
                                if (!WeHavePotentialCombatTargetsThatArentReadyToShoot)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid) Log("if (!WeHavePotentialCombatTargetsThatArentReadyToShoot [" + WeHavePotentialCombatTargetsThatArentReadyToShoot + "]) return true");
                                    return true;
                                }
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.ArmorPct > .10))
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.ArmorPct > .10)) return false");
                                return false;
                            }

                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip) //range is low, dont go wandering around ffs!
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip) return false");
                                return false;
                            }

                            if (DebugConfig.DebugNavigateOnGrid) Log("//wait till armor is low! return true");
                            //wait till armor is low!
                            return true;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("return true");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) > 3)
                    {
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) > 6)
                    {
                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn)
                {
                    if (_cruiserTankThreshold >= Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (_cruiserTankThreshold [" + _cruiserTankThreshold + "] >= Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser)) return true");
                        return true;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar && 8 >= Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Ishtar && 8 >= Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser)) return true");
                        return true;
                    }

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.RodivaSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 3)
                    {
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                    {
                        if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate && i.Name.ToLower().Contains("lance".ToLower())) >= 4)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate && i.Name.ToLower().Contains("lance".ToLower())) >= 2)
                        {
                            return false;
                        }
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer) >= 2)
                    {
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
                    {
                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip) //range is low, dont go wandering around ffs!
                            return false;

                        if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer && i.ShieldPct > .50))
                            {
                                return false;
                            }
                        }
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 7)
                    {
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                    {
                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 3)
                    {
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 2)
                    {
                        if (Combat.PotentialCombatTargets.Any(x => x.IsNPCBattlecruiser && .25 > x.ShieldPct))
                        {
                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip) //range is low, dont go wandering around ffs!
                                return false;

                            return true;
                        }

                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                {
                    if (1 == Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship))
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                        {
                            //All drones are engaged
                            if (!ESCache.Instance.ActiveShip.IsFrigate && !WeHavePotentialCombatTargetsThatArentReadyToShoot)
                                return true;

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.StructurePct > .75))
                                return false;

                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip) //range is low, dont go wandering around ffs!
                                return false;

                            //wait till armor is low!
                            return true;
                        }

                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser)) //high angle BCs are mean!
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser)) return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) > 3)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) > 3) return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate && i.Name.Contains("Blast")) >= 3)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate && i.Name.Contains(\"Blast\")) >= 3) return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 12)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 9) return false");
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.IsFrigate)
                    {
                        if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 5)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 5) return false");
                            return false;
                        }
                    }

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                {
                    if (!Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Lucifer Cynabal")))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (!Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains(\"Lucifer Cynabal\")))");
                        if (6 > Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (4 > Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate)) return true");
                            return true;
                        }
                    }

                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn || AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedHunter)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Devoted Hunter")) >= 2)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(\"Devoted Hunter\")) >= 2) return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Devoted Hunter")) && .75 > ESCache.Instance.ActiveShip.ShieldPercentage)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains(\"Devoted Hunter\")) && .75 > ESCache.Instance.ActiveShip.ShieldPercentage) return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Devoted Knight")))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains(\"Devoted Knight\"))) return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 2)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser)) return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 6)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (4 > Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate)) return false");
                        return false;
                    }

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 3)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 3) return false");
                        return false;
                    }

                    if (2 >= Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (2 >= Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship))");
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))");
                            if (Combat.PotentialCombatTargets.All(i => i.IsNPCBattleship))
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.All(i => i.IsNPCBattleship))");
                                if (!WeHavePotentialCombatTargetsThatArentReadyToShoot)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid) Log("if (!WeHavePotentialCombatTargetsThatArentReadyToShoot) return true");
                                    return true;
                                }
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser)) return false");
                                return false;
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.ArmorPct > .33))
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.IsTarget && i.ArmorPct > .33)) return false");
                                return false;
                            }

                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip) //range is low, dont go wandering around ffs!
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip) return false");
                                return false;
                            }

                            //wait till armor is low!
                            if (DebugConfig.DebugNavigateOnGrid) Log("our targets armor is low! return true");
                            return true;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 2)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 2) return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 6)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate)) >= 3 return false");
                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }


                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) > 1)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) > 1) return false");
                        return false;
                    }

                    if (1 == Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (1 == Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship))");

                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser)) return false");
                            return false;
                        }

                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))");
                            if (Combat.PotentialCombatTargets.All(i => i.IsNPCBattleship))
                            {
                                if (!WeHavePotentialCombatTargetsThatArentReadyToShoot)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid) Log("if (!WeHavePotentialCombatTargetsThatArentReadyToShoot) return true;");
                                    return true;
                                }
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.ArmorPct > .33))
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.ArmorPct > .33)) return false;");
                                return false;
                            }

                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip) //range is low, dont go wandering around ffs!
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip) return false");
                                return false;
                            }

                            //wait till armor is low!
                            if (DebugConfig.DebugNavigateOnGrid) Log("target armor is low! return true");
                            return true;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 4)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 4) return true");
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)");
                        if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 2)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 2) return false");
                            return false;
                        }

                        if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 4)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 3) return false");
                            return false;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("return true");
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 7)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 7) return true");
                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }


                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 2)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 2) return false");
                        return false;
                    }

                    if (1 == Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship))
                    {
                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)");
                            return false;
                        }

                        if (AreAllTargetsInASpeedCloud && Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.DistanceTo(_nextGate) > 35000))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (AreAllTargetsInASpeedCloud && Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.DistanceTo(_nextGate) > 35000))");
                            return false;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("if (1 == Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship))");
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                        {
                            if (Combat.PotentialCombatTargets.All(i => i.IsNPCBattleship))
                            {
                                if (!WeHavePotentialCombatTargetsThatArentReadyToShoot)
                                    return true;
                            }

                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.IsTarget))");
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.ArmorPct > .3))
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.IsTarget && i.ArmorPct > .3))");
                                return false;
                            }

                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip) //range is low, dont go wandering around ffs!
                                return false;

                            //wait till armor is low!
                            if (DebugConfig.DebugNavigateOnGrid) Log("return true");
                            return true;
                        }

                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                {
                    if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses && Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Vedmak")))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(Vedmak)) >= 2) return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Vedmak")) >= 2)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(Vedmak)) >= 2) return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer) > 2)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer) > 2) return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) > 3)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) > 3) return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 9)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 9) return false");
                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakVilaCruiserSwarmerSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 3)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser)) return true");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 7)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (4 > Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate)) return true");
                        return true;
                    }

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikVilaFrigateSwarmerSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 3)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 3) return true");
                        return true;
                    }

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                //
                // for the AbyssalSpawnType(s) detected above we never fall through to these conditions below: in mast cases that is okay
                //
                if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                {
                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] No NPCs On Grid return true");
                    return true;
                }

                if (5 > Combat.PotentialCombatTargets.Count(i => i.BracketType != BracketType.Large_Collidable_Structure))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (5 > Combat.PotentialCombatTargets.Count()) return true");
                    return true;
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("return false");
                return false;
            }
        }

        private bool IsItSafeToMoveToMTUOrLootWrecks
        {
            get
            {
                if (WeHavePotentialCombatTargetsThatArentReadyToShoot)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (WeHavePotentialCombatTargetsThatArentReadyToShoot [" + WeHavePotentialCombatTargetsThatArentReadyToShoot + "]) return false");
                    return false;
                }

                if ((ESCache.Instance.ActiveShip.IsShipWithDroneBonuses && !AreAllDronesAttacking) || !ESCache.Instance.ActiveShip.IsShipWithDroneBonuses)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure && !i.IsNPCFrigate))
                    {
                        if (ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor != null && 22000 > ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor.DistanceTo(ESCache.Instance.AbyssalGate) && !ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor != null && 22000 > ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor.DistanceTo(ESCache.Instance.AbyssalGate)) return false");
                            return false;
                        }

                        if (!ESCache.Instance.ActiveShip.IsCruiserWithDroneBonuses)
                        {
                            if (ESCache.Instance.AbyssalDeadspaceMediumDeviantAutomataSuppressor != null && 40000 > ESCache.Instance.AbyssalDeadspaceMediumDeviantAutomataSuppressor.DistanceTo(ESCache.Instance.AbyssalGate) && !ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalDeadspaceMediumDeviantAutomataSuppressor != null && 40000 > ESCache.Instance.AbyssalDeadspaceMediumDeviantAutomataSuppressor.DistanceTo(ESCache.Instance.AbyssalGate)) return false");
                                return false;
                            }
                        }
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 1)
                    {
                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                            return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 7)
                    {
                        if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("PotentialCombatTargets IsNPCCruiser [" + Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) + "] >= 6 false");
                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && Math.Min(ESCache.Instance.ActiveShip.GetDroneControlRange(), ESCache.Instance.ActiveShip.MaxTargetRange) > i.Distance))
                    {
                        if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && 45000 > i.Distance))  return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 1)
                    {
                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                        {
                            if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 1) return false");
                            return false;
                        }
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                    {
                        //if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila || ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                        //{
                        //    if (AreAllDronesAttacking)
                        //        return true;
                        //
                        //    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("!AreAllDronesAttacking return false");
                        //   return false;
                        //}

                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip) //range is low, dont go wandering around ffs!
                            return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
                {
                    if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Gila && Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 2)
                    {
                        if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("PotentialCombatTargets if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Gila && Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 2)");
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip) //range is low, dont go wandering around ffs!
                        return false;

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 1)
                    {
                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                            return false;

                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip) //range is low, dont go wandering around ffs!
                            return false;
                    }

                    if (_lucidDeepwatcherBattleshipsOnGridCount > _lucidDeepwatcherBattleshipsTankThreshold)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("_lucidDeepwatcherBattleshipsOnGridCount [" + _lucidDeepwatcherBattleshipsOnGridCount + "] > _lucidDeepwatcherBattleshipsTankThreshold [" + _lucidDeepwatcherBattleshipsTankThreshold + "]");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                    {
                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip) //range is low, dont go wandering around ffs!
                            return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                {
                    if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer)
                    {
                        if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 1)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 5) return false");
                            return false;
                        }

                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip) //range is low, dont go wandering around ffs!
                            return false;

                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 4)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("PotentialCombatTargets IsNPCBattlecruiser [" + Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) + "] > [ 4 ] return false");
                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                {
                    if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                    {
                        if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Lucifer Cynabal")) >= 1)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(\"Lucifer Cynabal\")) >= 1) return false");
                            return false;
                        }

                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Lucifer Cynabal")) >= 5)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(\"Lucifer Cynabal\")) >= 5) return false");
                        return false;
                    }

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn || AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedHunter)
                {
                    if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                    {
                        if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Devoted Hunter")) >= 2)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(\"Devoted Hunter\")) >= 2) return true");
                            return false;
                        }

                        if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Devoted Knight")) >= 1)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(\"Devoted Knight\")) >= 2) return true");
                            return false;
                        }

                        return true;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                {
                    if (_marshalsOnGridCount >= _marshalTankThreshold)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("_marshalsOnGridCount [" + _marshalsOnGridCount + "] > _marshalTankThreshold [" + _marshalTankThreshold + "] return false");
                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                {
                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn)
                {
                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.TypeName.ToLower().Contains("kiki".ToLower())) >= 5)
                    {
                        if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return false: >= 5 kikis");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Where(i => i.TypeName.ToLower().Contains("kiki".ToLower())).Any(x => x.Distance > 25000))
                    {
                        if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return false some kikis are too far out: we need to stay close!");
                        return false;
                    }

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Lucifer Cynabal")) >= 3)
                    {
                        if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(Lucifer Cynabal)) >= 3) return false");
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                    {
                        if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer) return false");
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                        {
                            if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser)) return false");
                            return false;
                        }
                    }

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn)
                {
                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= _cruiserTankThreshold)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= _cruiserTankThreshold [" + _cruiserTankThreshold + "])");
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar && Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 9)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 9");
                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Leshak")) >= 7)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(Leshak)) >= 7) return false");
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip) //range is low, dont go wandering around ffs!
                        return false;

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Vedmak")) >= 4)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(Vedmak)) >= 4) return false");
                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakVilaCruiserSwarmerSpawn)
                {
                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  return true");
                    return true;
                }

                //
                // for the AbyssalSpawnType(s) detected above we never fall through to these conditions below: in mast cases that is okay
                //
                if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                {
                    if (DebugConfig.DebugNavigateOnGrid || DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] No NPCs On Grid return true");
                    return true;
                }

                if (5 > Combat.PotentialCombatTargets.Count(i => i.BracketType != BracketType.Large_Collidable_Structure))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (5 > Combat.PotentialCombatTargets.Count()) return true");
                    return true;
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("return false");
                return false;
            }
        }

        private bool WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck
        {
            get
            {
                try
                {
                    if (_getMTUInSpace != null)
                    {
                        if (DebugConfig.DebugMobileTractor) Log("if (_getMTUInSpace != null)");
                        return false;
                    }

                    //If there are no wrecks we cant have picked up the loot
                    if (ESCache.Instance.Wrecks == null)
                        return false;

                    //If there are no wrecks we cant have picked up the loot
                    if (!ESCache.Instance.Wrecks.Any())
                        return false;

                    if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) && Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure && (!i.IsTarget || !i.IsReadyForDronesToShoot)))
                        return false;

                    if (ESCache.Instance.Wrecks.Any(i => i.IsWreckEmpty && i.IsAbyssalCacheWreck))
                        return true;

                    return false;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        private bool IsSituationThatNecessitatesWeUseOurUniqueOrbitRoutine
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (_singleRoomAbyssal && DebugConfig.DebugFourteenBattleshipSpawnRunAway)
                    return false;

                if (boolShouldWeBeSpeedTanking) //never use our unique orbit routine when speed tanking?!
                    return false;

                if (IsItSafeToMoveToMTUOrLootWrecks)
                    return true;

                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && i.BracketType != BracketType.Large_Collidable_Structure) && AreAllOurDronesInASpeedCloud && _tooManyEnemiesAreStillInASpeedCloudCount > 4)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (DirectEve.Interval(5000) && Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && i.BracketType != BracketType.Large_Collidable_Structure) && AreAllOurDronesInASpeedCloud && _tooManyEnemiesAreStillInASpeedCloudCount > 4)");
                    return true;
                }

                if ((ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate) && Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                {
                    if (ESCache.Instance.ActiveShip.IsShieldTanked && (overHeatReps || 99 > ESCache.Instance.ActiveShip.ArmorPercentage))
                    {
                        //Log("We are in danger!?! Trying to put some distance between us and our aggressor");
                        return true;
                    }

                    if (ESCache.Instance.ActiveShip.IsArmorTanked && (overHeatReps || 99 > ESCache.Instance.ActiveShip.StructurePercentage))
                    {
                        //Log("We are in danger!?! Trying to put some distance between us and our aggressor");
                        return true;
                    }
                }

                if (ESCache.Instance.ActiveShip.IsAssaultShip && Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                    return true;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                    if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsAbyssalKaren))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsAbyssalKaren))");
                        if (_getMTUInSpace != null && WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck && !Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser) && ESCache.Instance.ActiveShip.TypeId != (int)TypeID.StormBringer)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (_getMTUInSpace != null && WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck && !Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser)) return false");
                            return false;
                        }

                        if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsAbyssalKaren))
                            {
                                return false;
                            }

                            return true;
                        }

                        if (Drones.DroneControlRange > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Drones.DroneControlRange > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Distance) return true;");
                            return true;
                        }

                        if (AreAllDronesAttacking || NoDroneBayAllNPCsInRange || ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && .25 > i.ArmorPct))
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsAbyssalKaren && 4 > i.ShieldPct && 25 > i.ArmorPct)) return false; We can move away and use longer range ammo now?");
                                return false;
                            }

                            if (DebugConfig.DebugNavigateOnGrid) Log("if (AreAllDronesAttacking || NoDroneBayAllNPCsInRange || ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer) return true;");
                            return true;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("return false.");
                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("return false!");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn && (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Gila || ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer || ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))");
                        if (_getMTUInSpace != null && WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck && !Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (_getMTUInSpace != null && WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck && !Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser)) return false");
                            return false;
                        }

                        if (Drones.DroneControlRange > Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Drones.DroneControlRange > Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Distance) return true;");
                            return true;
                        }

                        if (AreAllDronesAttacking || NoDroneBayAllNPCsInRange || ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && .25 > i.ArmorPct))
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsAbyssalKaren && 4 > i.ShieldPct && 25 > i.ArmorPct)) return false; We can move away and use longer range ammo now?");
                                return false;
                            }

                            if (DebugConfig.DebugNavigateOnGrid) Log("if (AreAllDronesAttacking || NoDroneBayAllNPCsInRange) return true..");
                            return true;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("return false.");
                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("return false!");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn && ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Gila)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 2)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 2)");
                        if (_getMTUInSpace != null && WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck && 2 > Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (_getMTUInSpace != null && WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck && !Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 2) return false");
                            return false;
                        }

                        if (Drones.DroneControlRange > Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattlecruiser).Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Drones.DroneControlRange > Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Distance) return true;");
                            return true;
                        }

                        if (AreAllDronesAttacking || NoDroneBayAllNPCsInRange || ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (AreAllDronesAttacking || NoDroneBayAllNPCsInRange) return true...");
                            return true;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("return false.");
                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("return false!");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleBattleCruiserSpawn)");
                    if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer)
                    {
                        if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 1)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 1) return true");
                            return true;
                        }

                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 4)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 4) return true");
                        return true;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("return false!");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                    if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer)
                    {
                        if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Lucifer Cynabal")) >= 1)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(\"Lucifer Cynabal\")) >= 1) return true");
                            return true;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Lucifer Cynabal")) >= 2)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(\"Lucifer Cynabal\")) >= 2 return true");
                        return true;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("return false!");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn || AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedHunter)
                {
                    if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Devoted Hunter")))
                        {
                            return true;
                        }

                        if (Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Devoted Knight")))
                        {
                            return true;
                        }
                    }
                }

                /**
                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 2)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 2) return true");
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Count() >= 7 && Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 3)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 2) return true");
                        return true;
                    }
                }
                **/

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)");
                    if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Vedmak")) >= 4)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(Vedmak)) >= 4) return true");
                        return true;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("return false. [" + AbyssalSpawn.DetectSpawn + "]");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)");
                    if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Kikimora")) >= _kikimoraTankThreshold)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(Vedmak)) >= 5) return true");
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Where(i => i.TypeName.Contains("Kikimora")).Any(x => x.Distance > 24000))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Where(i => i.TypeName.Contains(\"Kikimora\")).All(x => x.Distance > 24000)) return true");
                        return true;
                    }

                    if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Kikimora")))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(Vedmak)) >= 5) return true");
                            return true;
                        }
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("return false. [" + AbyssalSpawn.DetectSpawn + "]");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                    if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Lucifer Cynabal")) >= 2 && (ESCache.Instance.ActiveShip.Entity.IsInSpeedCloud || Combat.PotentialCombatTargets.Any(i => i._directEntity.IsInSpeedCloud)))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(\"Lucifer Cynabal\")) >= 2 && (ESCache.Instance.ActiveShip.Entity.IsInSpeedCloud || Combat.PotentialCombatTargets.Any(i => i._directEntity.IsInSpeedCloud))) return true");
                        return true;
                    }

                    if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))");
                            return true;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("return false. [" + AbyssalSpawn.DetectSpawn + "]");
                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("return false. [" + AbyssalSpawn.DetectSpawn + "]");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)");
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 3)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 3) return true");
                        return true;
                    }
                }

                if (ESCache.Instance.ActiveShip.IsLocatedWithinBioluminescenceCloud)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsLocatedWithinBioluminescenceCloud) return true");
                    return true;
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("return false! [" + AbyssalSpawn.DetectSpawn + "]");
                return false;
            }
        }

        internal bool MoveToThisDirectWorldPosition(DirectWorldPosition thisDWP)
        {
            if (thisDWP != null)
            {
                if (DirectEve.Interval(5000))
                    Log($"MoveToOverride exists. Moving there.");
                DirectEntity.MoveToViaAStar(2000, distanceToTarget:0, forceRecreatePath: forceRecreatePath, dest: thisDWP,
                        ignoreAbyssEntities: true,
                        ignoreTrackingPolyons: ESCache.Instance.IgnoreTrackingPolyons_ImprovesTracking_HelpsToHitSmallThings,
                        ignoreAutomataPylon: ESCache.Instance.IgnoreAutomataPylon_Kills_NPCDrones_OurDrones_And_Missiles,
                        ignoreWideAreaAutomataPylon: true,
                        ignoreBioClouds: ESCache.Instance.IgnoreBioClouds_Blue_4xSignatureRadius,
                        ignoreFilaCouds: ESCache.Instance.IgnoreFilaClouds_Orange_ShieldBoostCapacitorUsePenalty,
                        ignoreTachClouds: ESCache.Instance.IgnoreTachClouds_White_4xSpeedBoost
                        );

                return true;
            }

            return false;
        }

        internal bool MoveToUsingBestMethod(
            EntityCache NearestTargetWeWantToAvoid,
            double _minMoveToRange,
            double _maxMoveToRange)
        {
            try
            {
                if (NearestTargetWeWantToAvoid == null)
                    return false;

                if (_maxMoveToRange > Drones.MaxDroneRange || _maxMoveToRange > ESCache.Instance.ActiveShip.MaxTargetRange)
                {
                    if (allDronesInSpace.All(i => i._directEntity.DroneState != (int)Drones.DroneState.Attacking))
                    {
                        try
                        {
                            _minMoveToRange = Math.Min(Drones.MaxDroneRange, ESCache.Instance.ActiveShip.MaxTargetRange) - 7000;
                            _maxMoveToRange = Math.Min(Drones.MaxDroneRange, ESCache.Instance.ActiveShip.MaxTargetRange) - 1000;
                        }
                        catch (Exception){}
                    }
                }

                if (boolShouldWeBeSpeedTanking)
                {
                    if (ESCache.Instance.AbyssalCenter.Distance > ESCache.Instance.SafeDistanceFromAbyssalCenter)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("AbyssalCenter [" + ESCache.Instance.AbyssalCenter.Nearest1KDistance + "] > SafeDistanceFromAbyssalCenter [" + Math.Round((double)ESCache.Instance.SafeDistanceFromAbyssalCenter / 1000, 0) + "]");
                        return false;
                    }
                }
                else if (ESCache.Instance.AbyssalGate.Distance > 70000 || (!boolShouldWeBeSpeedTanking && Time.Instance.LastTooFarFromGate.AddSeconds(30) > DateTime.UtcNow))
                {
                    Log("[" + NearestTargetWeWantToAvoid.TypeName + "] Orbit Gate so we dont sit in one spot: we are [" + ESCache.Instance.AbyssalGate.Nearest1KDistance + "k] from the gate [" + ESCache.Instance.AbyssalCenter.Nearest1KDistance + "k] from AbyssalCenter");
                    _lastMoveToOverride = DateTime.UtcNow;
                    if (ESCache.Instance.AbyssalGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(42000, ESCache.Instance.AbyssalGate._directEntity))
                    {
                        Time.Instance.LastTooFarFromGate = DateTime.UtcNow;
                        return true;
                    }

                    return true;
                }

                var direction = ESCache.Instance.AbyssalGate._directEntity.DirectAbsolutePosition.GetDirectionalVectorTo(_activeShipPos)
                        .Normalize(); // go backwards!
                if (direction == null)
                    Log("direction == null - this is bad!");
                Vec3 DirectionToMove = new Vec3(0, 0, 0);
                DirectionToMove = DirectSceneManager.RotateVector(direction, 5);

                if (boolShouldWeBeSpeedTanking)
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                    {
                        DirectionToMove = (Vec3)_moveUp;
                    }
                }

                // stay between 70k and 74.5 k (max range will be 75 after the next patch)
                // if we are already further way do not override
                if (_minMoveToRange > NearestTargetWeWantToAvoid.Distance)
                {
                    Log("[" + NearestTargetWeWantToAvoid.TypeName + "][" + NearestTargetWeWantToAvoid.Nearest1KDistance + "] Moving Away");
                    _moveToOverride = CalculateMoveToOverrideSpot(DirectionToMove, NearestTargetWeWantToAvoid._directEntity, _minMoveToRange, _maxMoveToRange);
                    if (_moveToOverride != null)
                        _lastMoveToOverride = DateTime.UtcNow;
                    if (MoveToThisDirectWorldPosition(_moveToOverride))
                    {
                        return true;
                    }

                    return true;
                }

                if (AreAllDronesAttacking || NoDroneBayAllNPCsInRange)
                {
                    Log("[" + NearestTargetWeWantToAvoid.TypeName + "] Drones are aggressed, stay here");
                    _moveToOverride = CalculateMoveToOverrideSpot(DirectionToMove, NearestTargetWeWantToAvoid._directEntity, _minMoveToRange, _maxMoveToRange);
                    if (_moveToOverride != null)
                        _lastMoveToOverride = DateTime.UtcNow;
                    if (MoveToThisDirectWorldPosition(_moveToOverride))
                    {
                        return true;
                    }

                    return true;
                }

                if (boolShouldWeBeSpeedTanking)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return true;
            }
        }

        internal void MoveToThisTestSpot()
        {
            //make a new directAbsolutePosition of the gate, move that to 30k up and 30k to the right of the gate
            DirectWorldPosition SpotToUse = _nextGate._directEntity.DirectAbsolutePosition;
            //
            //SpotToUse = SpotToUse add 30 k? - FixMe
            Vec3 direction = SpotToUse.GetDirectionalVectorTo(_activeShipPos).Normalize(); // go backwards!
            _moveTestDirection = DirectSceneManager.RotateVector(direction, 5);
            //CalculateMoveToOverrideSpot(_moveTestDirection, NearestTargetWeWantToAvoid._directEntity, _minMoveToRange, _maxMoveToRange);
        }

        internal bool boolShouldWeBeSpeedTanking
        {
            get
            {
                if (NavigateOnGrid.SpeedTank)
                    return true;

                if (ESCache.Instance.ActiveShip.IsAssaultShip)
                    return true;

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Vagabond)
                {
                    return true;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                {
                    return false;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.VexorNavyIssue)
                {
                    return true;
                }

                /**
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                    {
                        return true;
                    }
                }
                **/

                if (ESCache.Instance.ActiveShip.IsCruiser && NavigateOnGrid.SpeedTank && ESCache.Instance.ActiveShip.HasSpeedMod && ESCache.Instance.ActiveShip.TypeId != (int)TypeID.Gila && ESCache.Instance.ActiveShip.TypeId != (int)TypeID.Ishtar)
                    return true;

                if (ESCache.Instance.ActiveShip.IsFrigate)
                    return true;

                if (ESCache.Instance.ActiveShip.IsDestroyer)
                    return true;

                return false;
            }
        }

        internal int KeepMeSafelyInsideTheArena_AbyssalOvermindDroneBSSpawn_OrbitDistance_MWD
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                    return 500;
                }

                return (int)Combat.MaxRange - 28000;
            }
        }

        internal int KeepMeSafelyInsideTheArena_AbyssalOvermindDroneBSSpawn_OrbitDistance_AB
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                    return 500;
                }

                return (int)Combat.MaxRange - 32000;
            }
        }

        internal int KeepMeSafelyInsideTheArena_ConcordSpawn_OrbitDistance_MWD
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                    return 3000;
                }

                return (int)Combat.MaxRange - 10000;
            }
        }

        internal int KeepMeSafelyInsideTheArena_ConcordSpawn_OrbitDistance_AB
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                    return 500;
                }

                return (int)Combat.MaxRange - 10000;
            }
        }

        internal int KeepMeSafelyInsideTheArena_DrekavacBattleCruiserSpawn_OrbitDistance_MWD
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                    return 3000;
                }

                return (int)Combat.MaxRange - 10000;
            }
        }

        internal int KeepMeSafelyInsideTheArena_DrekavacBattleCruiserSpawn_OrbitDistance_AB
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                    return 500;
                }

                return (int)Combat.MaxRange - 10000;
            }
        }

        internal int KeepMeSafelyInsideTheArena_DroneFrigateSpawn_OrbitDistance_MWD
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                    return 3000;
                }

                return (int)Combat.MaxRange - 7000;
            }
        }

        internal int KeepMeSafelyInsideTheArena_DroneFrigateSpawn_OrbitDistance_AB
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                    return 500;
                }

                return ESCache.Instance.SafeDistanceFromAbyssalCenter - 4000;
            }
        }


        internal int KeepMeSafelyInsideTheArena_HighAngleDroneBattleCruiserSpawn_OrbitDistance_MWD
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                    return 3000;
                }

                return (int)Combat.MaxRange - 10000;
            }
        }

        internal int KeepMeSafelyInsideTheArena_HighAngleDroneBattleCruiserSpawn_OrbitDistance_AB
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                    return 12000;
                }

                //fix me: (int)Combat.MaxRange (52k?) - 10000 = 42k!
                return (int)Combat.MaxRange - 10000;
            }
        }

        internal bool WeAreRetrievingAnMTUThatIsFarAway
        {
            get
            {
                if (_getMTUInSpace == null) return false;

                if (!ShouldWeScoopMTU) return false;

                if (ESCache.Instance.SafeDistanceFromAbyssalCenter > _getMTUInSpace.DistanceTo(ESCache.Instance.AbyssalCenter))
                    return false;

                return true;
            }
        }



        internal bool boolIsDangerousSituationToHaveTrackingBoost
        {
            get
            {
                if (!ESCache.Instance.ActiveShip.Entity.IsLocatedWithinRangeOfPulsePlatformTrackingPylon)
                    return false;

                if (ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon == null || !ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon.Any())
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                {
                    Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] boolIsDangerousSpawnToHaveTrackingBoost  return true;");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsAbyssalKaren && 30000 > i.Distance))
                    {
                        Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] boolIsDangerousSpawnToHaveTrackingBoost  return true;");
                        return true;
                    }

                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                {
                    Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] boolIsDangerousSpawnToHaveTrackingBoost  return true;");
                    return true;
                }

                //any more?

                Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] boolIsDangerousSpawnToHaveTrackingBoost  return false;");
                return false;
            }
        }

        internal bool IsKarenHuggingSuppressor
        {
            get
            {
                if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsAbyssalKaren))
                {
                    if (ESCache.Instance.Entities.Any(i => i.IsAbyssalDeadspaceDeviantAutomataSuppressor))
                    {
                        if (ESCache.Instance.Entities.Any(i => i.IsAbyssalDeadspaceSmallDeviantAutomataSuppressor))
                        {
                            if (ESCache.Instance.Entities.Any(i => i.IsAbyssalDeadspaceSmallDeviantAutomataSuppressor && i._directEntity.IsAbyssalKaren && i._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor))
                            {
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

        internal bool AreFleetMembersAllHere
        {
            get
            {
                if (ESCache.Instance.EveAccount.UseFleetMgr)
                {
                    if (DebugConfig.DebugFleetMgr) Log("UseFleetMgr [" + ESCache.Instance.EveAccount.UseFleetMgr + "]");

                    foreach (DirectFleetMember individualFleetMember in ESCache.Instance.DirectEve.FleetMembers.Where(i => i.Name != ESCache.Instance.DirectEve.Me.Name))
                    {
                        if (DebugConfig.DebugFleetMgr) Log("foreach (DirectFleetMember individualFleetMember in ESCache.Instance.DirectEve.FleetMembers.Where(i => i.Name != ESCache.Instance.DirectEve.Me.Name))");
                        if (individualFleetMember.Entity == null || (double)individualFleetMember.Entity.Distance > (double)Distances.OnGridWithMe)
                        {
                            if (DirectEve.Interval(7000)) Log("[" + individualFleetMember.Name + "] is not on grid with us yet! waiting.");
                            return false;
                        }

                        continue;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("[" + ESCache.Instance.DirectEve.FleetMembers.Count + "] fleet members are here on grid! continue");
                    return true;
                }

                return true;
            }
        }

        internal bool IsLeaderMoving
        {
            get
            {
                if (ESCache.Instance.EveAccount.UseFleetMgr)
                {
                    if (DebugConfig.DebugFleetMgr) Log("UseFleetMgr [" + ESCache.Instance.EveAccount.UseFleetMgr + "]");

                    var LeaderAsFleetMember = ESCache.Instance.DirectEve.FleetMembers.FirstOrDefault(i => i.Character.Name == ESCache.Instance.EveAccount.LeaderCharacterName);
                    if (LeaderAsFleetMember != null)
                    {
                        if (LeaderAsFleetMember.Entity != null)
                        {
                            if (LeaderAsFleetMember.Entity.Velocity > 0)
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

        internal bool WeWantToBeMoving
        {
            get
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (ESCache.Instance.EveAccount.UseFleetMgr)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.Velocity > 0))
                        {
                            if (ESCache.Instance.ActiveShip.Entity.Velocity == 0)
                            {
                                //ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.Regroup);
                            }

                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.Velocity > 0))");
                            return true;
                        }

                        if (Time.Instance.SecondsSinceLastSessionChange > 17)
                        {
                            if (DebugConfig.DebugNavigateOnGrid)
                                Log("SecondsSinceLastSessionChange [" + Math.Round(Time.Instance.SecondsSinceLastSessionChange, 0) + "] is more than 17 sec: return [true]");

                            return true;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn) //wait for them to move first!
                        {
                            if (Combat.PotentialCombatTargets.All(i => i.BracketType != BracketType.Large_Collidable_Structure && i.Velocity == 0))
                            {
                                if (DebugConfig.DebugNavigateOnGrid)
                                    Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] wait for them to move first!");

                                if (DirectEve.Interval(2000, 3000)) ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);
                                if (DebugConfig.DebugNavigateOnGrid) Log("WeWantToBeMoving [false]");
                                return false;
                            }

                            //they are moving!
                            return true;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn) //wait for them to move first!
                        {
                            if (Combat.PotentialCombatTargets.All(i => i.BracketType != BracketType.Large_Collidable_Structure && i.Velocity == 0))
                            {
                                if (DebugConfig.DebugNavigateOnGrid)
                                    Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] wait for them to move first!");

                                if (DirectEve.Interval(2000, 3000)) ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);
                                if (DebugConfig.DebugNavigateOnGrid) Log("WeWantToBeMoving [false]");
                                return false;
                            }

                            //they are moving!
                            return true;
                        }


                        //Are fleet members on grid with us yet?
                        if (AreFleetMembersAllHere)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("AreFleetMembersAllHere [true]");
                            if (ESCache.Instance.EveAccount.IsLeader)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.EveAccount.IsLeader): WeWantToBeMoving [true]");
                                return true;
                            }

                            if (IsLeaderMoving)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("IsLeaderMoving [true]");
                                return true;
                            }

                            if (DirectEve.Interval(2000, 3000)) ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);
                            if (DebugConfig.DebugNavigateOnGrid) Log("WeWantToBeMoving [false]");
                            return false;
                        }

                        if (DirectEve.Interval(2000, 3000)) ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);
                        if (DebugConfig.DebugNavigateOnGrid) Log("AreFleetMembersAllHere [false]");
                        return false;
                    }

                    return true;
                }

                if (ESCache.Instance.Stations.Any())
                {
                    if (2000 > ESCache.Instance.ClosestStation.Distance)
                        return false;
                }

                if (ESCache.Instance.Citadels.Any())
                {
                    if (2000 > ESCache.Instance.ClosestCitadel.Distance)
                        return false;
                }

                return true;
            }
        }

        internal bool KeepMeSafelyInsideTheArenaAndMoving()
        {
            if (!DirectEve.HasFrameChanged())
                return false;

            if (1 > Time.Instance.SecondsSinceLastSessionChange)
                return false;

            if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar && 6 > Time.Instance.SecondsSinceLastSessionChange)
                return false;

            if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.VexorNavyIssue && 6 > Time.Instance.SecondsSinceLastSessionChange)
                return false;

            if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer && 6 > Time.Instance.SecondsSinceLastSessionChange)
                return false;

            if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Vagabond && 6 > Time.Instance.SecondsSinceLastSessionChange)
                return false;

            if (ESCache.Instance.ActiveShip.Entity.Velocity == 0)
            {
                if (WeWantToBeMoving)
                {
                    if (DirectEve.Interval(4000, 5000, ESCache.Instance.ActiveShip.Entity.Velocity.ToString()))
                    {
                        if (MoveToUsingBestMethod(ESCache.Instance.AbyssalCenter, 70000, 80000)) return true;
                        return true;
                    }
                }
                else
                {
                    Log("WeWantToBeMoving [ false ] Our Velocity is Zero - we are waiting");
                    return true;
                }
            }

            if (ESCache.Instance.EveAccount.UseFleetMgr && FleetIdealCount >= 2 && !ESCache.Instance.EveAccount.IsLeader && 1700 > ESCache.Instance.ActiveShip.Entity.Velocity) //only orbit fleet leader here if we arent going too fast: otherwise check distance first
            {
                if (HandleNoAggroLootWhileWeCan) return true;  //Looting for non-leaders
                if (DebugConfig.DebugNavigateOnGrid) Log("HandleNoAggroLootWhileWeCan [false]");

                if (ShouldWeOrbitTheFleetLeader) return true;
                if (DebugConfig.DebugNavigateOnGrid) Log("ShouldWeOrbitTheFleetLeader [false]");
            }

                //if (ESCache.Instance.ActiveShip.IsLocatedWithinFilamentCloud)
                //{
                //    if (DebugConfig.DebugNavigateOnGrid)
                //        Log($"[" + AbyssalSpawn.DetectSpawn + "] IsLocatedWithinBioluminescenceCloud [" + ESCache.Instance.ActiveShip.IsLocatedWithinFilamentCloud + "]");
                //    ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(Combat.MaxRange - 5000);
                //    return true;
                //}

            if (ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon.Any(i => 1000 > i.Distance))
            {
                ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon.OrderBy(x => x.Distance).FirstOrDefault().Orbit(3000, true);
                return true;
            }

            if (ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.Any(i => 1000 > i.Distance))
            {
                ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.OrderBy(x => x.Distance).FirstOrDefault().Orbit(3000, true);
                return true;
            }

            if (ESCache.Instance.AbyssalCenter.Distance > ESCache.Instance.SafeDistanceFromAbyssalCenter && !WeAreRetrievingAnMTUThatIsFarAway)
            {
                if (DirectEve.Interval(2000)) Log("AbyssalCenter.Distance [" + Math.Round(ESCache.Instance.AbyssalCenter.Distance, 0) + "] > SafeDistanceFromCenter [" + ESCache.Instance.SafeDistanceFromAbyssalCenter + "]) MyVelocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + " m/s]");

                if (!boolShouldWeBeSpeedTanking)
                {
                    if (DirectEve.Interval(2000)) Log("if (!boolShouldWeBeSpeedTanking)");
                    if (ESCache.Instance.AbyssalCenter.Distance > ESCache.Instance.AbyssalGate.Distance)
                    {
                        if (DirectEve.Interval(2000)) Log("AbyssalGate [" + ESCache.Instance.AbyssalGate.Nearest1KDistance + "] is closer than AbyssalCenter [" + ESCache.Instance.AbyssalCenter.Nearest1KDistance + "]");

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                        {
                            if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                            {
                                if ( ESCache.Instance.ActiveShip.HasAfterburner)
                                {
                                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                                    {
                                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))");
                                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && 31000 > i.Distance))
                                        {
                                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && 31000 > i.Distance))");
                                            if (Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).KeepAtRange(25000))
                                                return true;
                                        }

                                        if (ESCache.Instance.AbyssalGate.KeepAtRange(25000))
                                            return true;

                                        if (ESCache.Instance.AbyssalCenter.Orbit(10000, true))
                                            return true;

                                        return false;
                                    }
                                }
                            }

                            if (_moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(1000, true))
                                return true;

                            if (ESCache.Instance.AbyssalCenter.Orbit(10000, true))
                                return true;

                            return false;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                        {
                            if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                            {
                                if (ESCache.Instance.ActiveShip.HasAfterburner)
                                {
                                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                                    {
                                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))");
                                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && 31000 > i.Distance))
                                        {
                                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && 31000 > i.Distance))");
                                            if (Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).KeepAtRange(25000))
                                                return true;
                                        }

                                        if (ESCache.Instance.AbyssalGate.KeepAtRange(25000))
                                            return true;
                                    }
                                }
                            }

                            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(1000);
                            return true;
                        }

                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            if (_moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(1000))
                                return true;

                            if (ESCache.Instance.AbyssalCenter.Orbit(10000))
                                return true;

                            return false;
                        }

                        if (DirectEve.Interval(2000)) Log("_moveToTarget_MTU_Wreck_Can_or_Gate_Entity [" + _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.TypeName + "] @ [" + _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Nearest1KDistance + "k] Orbit at 1500");

                        if (_moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(15000, true))
                        {
                            if (DirectEve.Interval(2000)) Log("if (_moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(15000, true)) returned true");
                            return true;
                        }

                        if (ESCache.Instance.AbyssalCenter.Orbit(10000, true))
                        {
                            if (DirectEve.Interval(2000)) Log("if (ESCache.Instance.AbyssalCenter.Orbit(10000, true)) returned true");
                            return true;
                        }

                        if (DirectEve.Interval(2000)) Log("Orbiting _moveToTarget_MTU_Wreck_Can_or_Gate_Entity and AbyssalCenter both failed!?");
                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("AbyssalCenter [" + ESCache.Instance.AbyssalCenter.Nearest1KDistance + "] is closer than AbyssalGate [" + ESCache.Instance.AbyssalGate.Nearest1KDistance + "]");

                    if (ESCache.Instance.AbyssalCenter.Orbit(10000, true))
                    {
                        if (DirectEve.Interval(2000)) Log("if (ESCache.Instance.AbyssalCenter.Orbit(10000, true)) returned true");
                        return true;
                    }

                    return false;
                }

                AbyssalSpawn.AbyssalSpawnType AbyssalDetectSpawnResult = AbyssalSpawn.DetectSpawn;

                if (AbyssalDetectSpawnResult != AbyssalSpawn.AbyssalSpawnType.Undecided)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("AbyssalSpawn.DetectSpawn [" + AbyssalDetectSpawnResult + "]!");
                    switch (AbyssalDetectSpawnResult)
                    {
                        case AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn:
                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("AbyssalOvermindDroneBSSpawn: if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                                {
                                    Combat.PotentialCombatTargets.Where(i => i.IsNPCBattleship).OrderBy(x => x.Distance).FirstOrDefault().Orbit(500);
                                    return true;
                                }

                                if (_moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500, true))
                                    return true;

                                if (ESCache.Instance.AbyssalGate.Orbit(10000, true))
                                    return true;

                                return false;
                            }

                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                Log("HasMicroWarpDrive");
                                if (_moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500, true))
                                    return true;

                                if (ESCache.Instance.AbyssalGate.Orbit(10000, true))
                                    return true;

                                return false;
                            }

                            if (_moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500, true))
                                return true;

                            if (ESCache.Instance.AbyssalGate.Orbit(10000, true))
                                return true;

                            return false;

                        case AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn:
                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                Log("HasMicroWarpDrive");
                                ESCache.Instance.AbyssalCenter.Orbit(KeepMeSafelyInsideTheArena_DroneFrigateSpawn_OrbitDistance_MWD, true);
                                return true;
                            }

                            ESCache.Instance.AbyssalCenter.Orbit(KeepMeSafelyInsideTheArena_DroneFrigateSpawn_OrbitDistance_AB, true);
                            return true;

                        case AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn:
                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("HighAngleBattleCruiserSpawn: if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                                {
                                    Combat.PotentialCombatTargets.Where(i => i.IsNPCBattlecruiser).OrderBy(x => x.Distance).FirstOrDefault().Orbit(18000, true);
                                    return true;
                                }

                                if (_moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500, true))
                                    return true;

                                if (ESCache.Instance.AbyssalGate.Orbit(10000, true))
                                    return true;

                                return false;
                            }

                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                Log("HasMicroWarpDrive");
                                ESCache.Instance.AbyssalCenter.Orbit(KeepMeSafelyInsideTheArena_HighAngleDroneBattleCruiserSpawn_OrbitDistance_MWD, true);
                                return true;
                            }

                            ESCache.Instance.AbyssalCenter.Orbit(KeepMeSafelyInsideTheArena_HighAngleDroneBattleCruiserSpawn_OrbitDistance_AB, true);
                            //ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(KeepMeSafelyInsideTheArena_HighAngleDroneBattleCruiserSpawn_OrbitDistance_AB);
                            return true;

                        case AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn:
                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("DrekavacBattleCruiserSpawn: if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                                {
                                    Combat.PotentialCombatTargets.Where(i => i.IsNPCBattlecruiser).OrderBy(x => x.Distance).FirstOrDefault().Orbit(14000, true);
                                    return true;
                                }

                                if (_moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500, true))
                                    return true;

                                if (ESCache.Instance.AbyssalGate.Orbit(10000, true))
                                    return true;

                                return false;
                            }

                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                Log("HasMicroWarpDrive");
                                ESCache.Instance.AbyssalCenter.Orbit(KeepMeSafelyInsideTheArena_DrekavacBattleCruiserSpawn_OrbitDistance_MWD, true);
                                return true;
                            }

                            //ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(KeepMeSafelyInsideTheArena_DrekavacBattleCruiserSpawn_OrbitDistance_AB);
                            ESCache.Instance.AbyssalCenter.Orbit(KeepMeSafelyInsideTheArena_DrekavacBattleCruiserSpawn_OrbitDistance_AB, true);
                            return true;

                        case AbyssalSpawn.AbyssalSpawnType.ConcordSpawn:
                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                Log("HasMicroWarpDrive");
                                ESCache.Instance.AbyssalCenter.Orbit(KeepMeSafelyInsideTheArena_ConcordSpawn_OrbitDistance_MWD, true);
                                return true;
                            }

                            ESCache.Instance.AbyssalCenter.Orbit(KeepMeSafelyInsideTheArena_ConcordSpawn_OrbitDistance_AB, true);
                            return true;

                        case AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn:
                        case AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn:
                        case AbyssalSpawn.AbyssalSpawnType.RodivaSpawn:
                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                Log("HasMicroWarpDrive");
                                ESCache.Instance.AbyssalCenter.Orbit((int)Combat.MaxRange - 9000, true);
                                return true;
                            }

                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                                ESCache.Instance.AbyssalCenter.Orbit(500);
                            }

                            ESCache.Instance.AbyssalCenter.Orbit((int)Combat.MaxRange - 9000, true);
                            return true;

                        case AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn:
                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                Log("HasMicroWarpDrive");
                                ESCache.Instance.AbyssalCenter.Orbit((int)Combat.MaxRange - 10000, true);
                                return true;
                            }

                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                                ESCache.Instance.AbyssalCenter.Orbit(500, true);
                            }

                            ESCache.Instance.AbyssalCenter.Orbit((int)Combat.MaxRange - 10000, true);
                            return true;

                        case AbyssalSpawn.AbyssalSpawnType.DevotedHunter:
                        case AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn:
                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                Log("HasMicroWarpDrive");
                                ESCache.Instance.AbyssalCenter.Orbit((int)Combat.MaxRange - 15000, true);
                                return true;
                            }

                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                                ESCache.Instance.AbyssalCenter.Orbit(500, true);
                            }

                            //ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 20000);
                            ESCache.Instance.AbyssalCenter.Orbit((int)Combat.MaxRange - 15000, true);
                            return true;

                        case AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn:
                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                                {
                                    //will get eventually get us killed? we might be at the very edge of the arena!
                                    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(500, true);
                                    return true;
                                }

                                ESCache.Instance.AbyssalCenter.Orbit(5000, true);
                            }

                            if (DirectEve.Interval(4000))
                            {
                                if (_moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500, true))
                                    return true;

                                if (ESCache.Instance.AbyssalGate.Orbit(10000, true))
                                    return true;

                                return false;
                            }

                            return true;

                        case AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn:
                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                                ESCache.Instance.AbyssalCenter.Orbit(500, true);
                            }

                            if (_moveToTarget_MTU_Wreck_Can_or_Gate_Entity != null)
                            {
                                _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(15000, true);
                                return true;
                            }

                            ESCache.Instance.AbyssalGate.Orbit(15000);
                            return true;

                        case AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn:
                            if (DirectEve.Interval(10000)) _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(1000, true);
                            return true;

                        case AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn:
                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                Log("HasMicroWarpDrive");
                                if (DirectEve.Interval(10000)) ESCache.Instance.AbyssalCenter.Orbit((int)Combat.MaxRange - 10000, true);
                                return true;
                            }

                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                                ESCache.Instance.AbyssalCenter.Orbit(500);
                            }

                            //ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 20000);
                            if (DirectEve.Interval(10000)) ESCache.Instance.AbyssalCenter.Orbit((int)Combat.MaxRange - 10000, true);
                            return true;

                        case AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn:
                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                Log("HasMicroWarpDrive");
                                ESCache.Instance.AbyssalCenter.Orbit((int)Combat.MaxRange - 14000, true);
                                return true;
                            }

                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                                ESCache.Instance.AbyssalCenter.Orbit(500, true);
                            }

                            //ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 14000);
                            if (DirectEve.Interval(4000)) ESCache.Instance.AbyssalCenter.Orbit((int)Combat.MaxRange - 14000, true);
                            return true;

                        case AbyssalSpawn.AbyssalSpawnType.LuciferSpawn:
                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                Log("HasMicroWarpDrive");
                                ESCache.Instance.AbyssalCenter.Orbit(ESCache.Instance.SafeDistanceFromAbyssalCenter - 12000, true);
                                return true;
                            }

                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                                ESCache.Instance.AbyssalCenter.Orbit(500);
                            }

                            ESCache.Instance.AbyssalCenter.Orbit(ESCache.Instance.SafeDistanceFromAbyssalCenter - 7000, true);
                            return true;

                        case AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn:
                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                Log("HasMicroWarpDrive");
                                ESCache.Instance.AbyssalCenter.Orbit((int)Combat.MaxRange - 15000, true);
                                return true;
                            }

                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                                ESCache.Instance.AbyssalCenter.Orbit(500);
                            }

                            ESCache.Instance.AbyssalCenter.Orbit((int)Combat.MaxRange - 15000, true);
                            return true;

                        case AbyssalSpawn.AbyssalSpawnType.VedmakVilaCruiserSwarmerSpawn:
                            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000, true);
                            return true;
                    }
                }

                Log("Unable to detect what spawn this is: Orbiting gate.");
                _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000, true);
                return true;
            }

            if (ESCache.Instance.EveAccount.UseFleetMgr && !ESCache.Instance.EveAccount.IsLeader) //no velocity check
            {
                if (HandleNoAggroLootWhileWeCan) return true;  //Looting for non-leaders
                if (DebugConfig.DebugNavigateOnGrid) Log("HandleNoAggroLootWhileWeCan [false]");

                if (ShouldWeOrbitTheFleetLeader) return true;
                if (DebugConfig.DebugNavigateOnGrid) Log("ShouldWeOrbitTheFleetLeader [false]");
            }

            if (!Combat.PotentialCombatTargets.Any() || Combat.PotentialCombatTargets.All(i => i.BracketType == BracketType.Large_Collidable_Structure))
                return false;

            if (!IsKarenHuggingSuppressor)
            {
                if (boolShouldWeBeSpeedTanking && ESCache.Instance.ActiveShip.Entity.IsTooCloseToSmallDeviantAutomataSuppressor && ESCache.Instance.PlayerSpawnLocation.DistanceTo(ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor) > 20000 && !WeAreRetrievingAnMTUThatIsFarAway)
                {
                    Log("Notification: ActiveShip IsTooCloseToSmallDeviantAutomataSuppressor [true]");
                    if (DebugConfig.DebugNavigateOnGrid) Log("[" + AbyssalSpawn.DetectSpawn + "]!");
                    if (boolShouldWeBeSpeedTanking && ESCache.Instance.Weapons.Any(i => i.IsMissileLauncher && !i._module.IsVortonProjector) && ESCache.Instance.ActiveShip.TypeId != (int)TypeID.Gila && !ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("[" + AbyssalSpawn.DetectSpawn + "] if (ESCache.Instance.Weapons.Any(i => i.IsMissileLauncher) && ESCache.Instance.ActiveShip.TypeId != (int)TypeID.Gila)");
                        if (DirectEve.Interval(3000))
                        {
                            if (PlayNotificationSounds) Util.PlayNoticeSound();
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                            {
                                if (!ESCache.Instance.AbyssalGate._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.MaxRange > ESCache.Instance.AbyssalGate.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship)))
                                {
                                    ESCache.Instance.AbyssalGate.Orbit(5000, true);
                                    return true;
                                }
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && !i._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor))
                            {
                                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && 30000 > i.Distance))
                                {
                                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && 25000 > i.Distance))
                                    {
                                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && 20000 > i.Distance))
                                        {
                                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && 15000 > i.Distance))
                                            {
                                                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && 10000 > i.Distance))
                                                {
                                                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && 5000 > i.Distance))
                                                    {
                                                        Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(1000);
                                                        return true;
                                                    }

                                                    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(5000);
                                                    return true;
                                                }

                                                Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(10000);
                                                return true;
                                            }

                                            Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(15000);
                                            return true;
                                        }

                                        Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(20000);
                                        return true;
                                    }

                                    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(25000);
                                    return true;
                                }
                            }
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                            {
                                if (!ESCache.Instance.AbyssalGate._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.MaxRange > ESCache.Instance.AbyssalGate.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship)))
                                {
                                    ESCache.Instance.AbyssalGate.Orbit(5000, true);
                                    return true;
                                }
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && !i._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor))
                            {
                                Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(5000, true);
                                return true;
                            }
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                            {
                                if (!ESCache.Instance.AbyssalGate._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.MaxRange > ESCache.Instance.AbyssalGate.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship)))
                                {
                                    ESCache.Instance.AbyssalGate.Orbit(5000);
                                    return true;
                                }
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && !i._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor))
                            {
                                Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(5000);
                                return true;
                            }
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                            {
                                if (!ESCache.Instance.AbyssalGate._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.MaxRange > ESCache.Instance.AbyssalGate.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship)))
                                {
                                    ESCache.Instance.AbyssalGate.Orbit(5000);
                                    return true;
                                }
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && !i._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor))
                            {
                                Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(5000);
                                return true;
                            }
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                            {
                                if (!ESCache.Instance.AbyssalGate._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.MaxRange > ESCache.Instance.AbyssalGate.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship)))
                                {
                                    ESCache.Instance.AbyssalGate.Orbit(5000);
                                    return true;
                                }
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && !i._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor))
                            {
                                Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(5000);
                                return true;
                            }
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                            {
                                if (!ESCache.Instance.PlayerSpawnLocation._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                                {
                                    ESCache.Instance.PlayerSpawnLocation.Orbit(15000);
                                    return true;
                                }

                                if (!ESCache.Instance.AbyssalGate._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.MaxRange > ESCache.Instance.AbyssalGate.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCCruiser)))
                                {
                                    ESCache.Instance.AbyssalGate.Orbit(5000);
                                    return true;
                                }
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser && !i._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor))
                            {
                                Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCCruiser).Orbit(5000);
                                return true;
                            }
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                        {
                            if (!ESCache.Instance.PlayerSpawnLocation._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                            {
                                ESCache.Instance.PlayerSpawnLocation.Orbit(15000);
                                return true;
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                            {
                                ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor.Orbit(30000);
                                return true;
                            }

                            if (!ESCache.Instance.AbyssalGate._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.MaxRange > ESCache.Instance.AbyssalGate.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCCruiser)))
                            {
                                ESCache.Instance.AbyssalGate.Orbit(5000);
                                return true;
                            }

                            ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor.Orbit(30000);
                            return true;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                            {
                                if (!ESCache.Instance.AbyssalGate._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.MaxRange > ESCache.Instance.AbyssalGate.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCCruiser)))
                                {
                                    ESCache.Instance.AbyssalGate.Orbit(5000);
                                    return true;
                                }
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser && !i._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor))
                            {
                                Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCCruiser).Orbit(5000);
                                return true;
                            }
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn ||
                            AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedHunter)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => !i.IsNPCCruiser))
                            {
                                if (!ESCache.Instance.PlayerSpawnLocation._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                                {
                                    ESCache.Instance.PlayerSpawnLocation.Orbit(15000);
                                    return true;
                                }

                                if (!ESCache.Instance.AbyssalGate._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.MaxRange > ESCache.Instance.AbyssalGate.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattlecruiser)))
                                {
                                    ESCache.Instance.AbyssalGate.Orbit(5000);
                                    return true;
                                }

                                return false;
                            }

                            return false;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                            {
                                if (!ESCache.Instance.AbyssalCenter._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                                {
                                    ESCache.Instance.AbyssalCenter.Orbit(15000);
                                    return true;
                                }

                                if (!ESCache.Instance.PlayerSpawnLocation._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                                {
                                    ESCache.Instance.PlayerSpawnLocation.Orbit(15000);
                                    return true;
                                }

                                if (!ESCache.Instance.AbyssalGate._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.MaxRange > ESCache.Instance.AbyssalGate.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattlecruiser)))
                                {
                                    ESCache.Instance.AbyssalGate.Orbit(5000);
                                    return true;
                                }
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser && !i._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor))
                            {
                                Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattlecruiser).Orbit(5000);
                                return true;
                            }
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                            {
                                if (ESCache.Instance.ActiveShip.IsFrigate)
                                {
                                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser && 25000 > i.Distance))
                                        return false;
                                }

                                if (!ESCache.Instance.PlayerSpawnLocation._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                                {
                                    ESCache.Instance.PlayerSpawnLocation.Orbit(15000);
                                    return true;
                                }

                                if (!ESCache.Instance.AbyssalGate._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.MaxRange > ESCache.Instance.AbyssalGate.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattlecruiser)))
                                {
                                    ESCache.Instance.AbyssalGate.Orbit(5000);
                                    return true;
                                }
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser && !i._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor))
                            {
                                Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattlecruiser).Orbit(5000);
                                return true;
                            }
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
                            {
                                if (!ESCache.Instance.PlayerSpawnLocation._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                                {
                                    ESCache.Instance.PlayerSpawnLocation.Orbit(15000);
                                    return true;
                                }

                                if (!ESCache.Instance.AbyssalGate._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.MaxRange > ESCache.Instance.AbyssalGate.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCDestroyer)))
                                {
                                    ESCache.Instance.AbyssalGate.Orbit(15000);
                                    return true;
                                }

                                if (Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer) >= 3)
                                {
                                    if (!ESCache.Instance.AbyssalCenter._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.MaxRange > ESCache.Instance.AbyssalCenter.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCDestroyer)))
                                    {
                                        ESCache.Instance.AbyssalCenter.Orbit(15000);
                                        return true;
                                    }
                                }

                            }

                            if (Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer) == 1 && Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer && !i._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor))
                            {
                                //bad idea?
                                Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCDestroyer).Orbit(5000);
                                return true;
                            }
                        }

                        if (!ESCache.Instance.AbyssalGate._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCCruiser) != null && Combat.MaxRange > ESCache.Instance.AbyssalGate.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCCruiser)))
                        {
                            ESCache.Instance.AbyssalGate.Orbit(5000);
                            return true;
                        }

                        Log("[" + AbyssalSpawn.DetectSpawn + "] IsTooCloseToSmallDeviantAutomataSuppressor [" + ESCache.Instance.ActiveShip.Entity.IsTooCloseToSmallDeviantAutomataSuppressor + "][" + ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor.Name + "] @ [" + ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor.Nearest1KDistance + "] k away");

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                        {
                            if (ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor._directEntity.IsWithinAbyssBounds() && ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor.DistanceTo(_nextGate) > 30000)
                            {
                                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship) && Combat.PotentialCombatTargets.Count(i => i.BracketType != BracketType.Large_Collidable_Structure) == 1)
                                {
                                    if (Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).DistanceTo(_nextGate) > 30000)
                                    {
                                        ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor.Orbit(500);
                                        return true;
                                    }
                                }
                            }
                        }

                        ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor.Orbit(35000);
                        return true;
                    }

                    //if (ESCache.Instance.ActiveShip.Entity.IsTooCloseToMediumDeviantAutomataSuppressor)
                    //{
                    //    Log("[" + AbyssalSpawn.DetectSpawn + "] IsTooCloseToMediumDeviantAutomataSuppressor [" + ESCache.Instance.ActiveShip.Entity.IsTooCloseToMediumDeviantAutomataSuppressor + "][" + ESCache.Instance.AbyssalDeadspaceMediumDeviantAutomataSuppressor.Name + "] @ [" + ESCache.Instance.AbyssalDeadspaceMediumDeviantAutomataSuppressor.Nearest1KDistance + "] k away");
                    //    ESCache.Instance.AbyssalDeadspaceMediumDeviantAutomataSuppressor._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(45000);
                    //    return true;
                    //}

                    if (Combat.PotentialCombatTargets.Any(i => i.Velocity > 0 && i._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && i.IsTarget))
                    {
                        if (!ESCache.Instance.AbyssalGate._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && Combat.MaxRange > ESCache.Instance.AbyssalGate.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsCurrentTarget)))
                        {
                            ESCache.Instance.AbyssalGate.Orbit(5000);
                            return true;
                        }

                        EntityCache NPCTooCloseToTower = Combat.PotentialCombatTargets.FirstOrDefault(i => i.Velocity > 0 && i._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor && i.IsTarget);
                        Log("[" + AbyssalSpawn.DetectSpawn + "] [" + NPCTooCloseToTower.TypeName + "] IsTooCloseToSmallDeviantAutomataSuppressor [true] moving further away and hoping it follows us");
                        NPCTooCloseToTower._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(52000, NPCTooCloseToTower._directEntity);
                        return true;
                    }

                    Log("[" + AbyssalSpawn.DetectSpawn + "] IsTooCloseToSmallDeviantAutomataSuppressor [true] moving further away");
                    ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.FirstOrDefault()._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(52000, ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.FirstOrDefault()._directEntity);
                    return true;
                }
            }

            int toofast = 4000;

            if (ESCache.Instance.ActiveShip.Entity.Velocity > toofast && ESCache.Instance.AbyssalGate.Nearest1KDistance > 40)
            {
                if (DirectEve.Interval(4000, 5000, ESCache.Instance.ActiveShip.Entity.Velocity.ToString()))
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && i.Name.ToLower().Contains("lance".ToLower())))
                        {
                            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(35000, true, "Velocity > 3500! - lance");
                            return true;
                        }
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser && i.Name.Contains("Cynabal")))
                        {
                            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(35000, true, "Velocity > 3500! - Cynabal");
                            return true;
                        }
                    }

                    _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(15000, true, "Velocity > 3500!");
                    return true;
                }
            }

            if (ESCache.Instance.ActiveShip.IsLocatedWithinBioluminescenceCloud)
            {
                Log($"[" + AbyssalSpawn.DetectSpawn + "] IsLocatedWithinBioluminescenceCloud [" + ESCache.Instance.ActiveShip.IsLocatedWithinBioluminescenceCloud + "]");
                ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(Combat.MaxRange - 20000, ESCache.Instance.AbyssalCenter._directEntity);
                return true;
            }

            if (ESCache.Instance.ActiveShip.Entity.IsLocatedWithinRangeOfPulsePlatformTrackingPylon && ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon != null && ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon.Any())
            {
                if (boolIsDangerousSituationToHaveTrackingBoost)
                {
                    //if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                    //{
                        //is there anything we can do to influence where Karen goes?!
                        //if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsAbyssalKaren && i.pylo))
                        //{
                        //
                        //}

                    //}

                    //40k for medium
                    //15k for small
                    Log($"[" + AbyssalSpawn.DetectSpawn + "] IsLocatedWithinRangeOfPulsePlatformTrackingPylon [" + ESCache.Instance.ActiveShip.Entity.IsLocatedWithinRangeOfPulsePlatformTrackingPylon + "][" + ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon.OrderBy(i => i.Distance).FirstOrDefault().Name + "] @ [" + ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon.OrderBy(i => i.Distance).FirstOrDefault().Nearest1KDistance + "] k away");
                    ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon.OrderBy(i => i.Distance).FirstOrDefault()._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(25000, ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon.OrderBy(i => i.Distance).FirstOrDefault()._directEntity);
                    return true;
                }
            }

            if (Combat.PotentialCombatTargets.Any())
            {
                if (ESCache.Instance.ActiveShip.IsLocatedWithinSpeedCloud && ESCache.Instance.ActiveShip.TypeId != (int)TypeID.Retribution && AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                {
                    Log("IsLocatedWithinSpeedCloud [True] DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");

                    if (boolShouldWeBeSpeedTanking)
                    {
                        if (HandleNoAggroLootWhileWeCan) return true;  //Looting for non-leaders
                        if (DebugConfig.DebugNavigateOnGrid) Log("HandleNoAggroLootWhileWeCan [false]!");

                        if (ShouldWeOrbitTheFleetLeader) return true;
                        if (DebugConfig.DebugNavigateOnGrid) Log("ShouldWeOrbitTheFleetLeader [false]!");

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                            return false;

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                            return false;

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                            return false;

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
                            return false;

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                            return false;

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                            return false;

                        /**
                        if (Combat.PotentialCombatTargets.Any(i => i.IsWebbingMe || i.IsWarpScramblingMe))
                        {
                            Log("if (Combat.PotentialCombatTargets.Any(i => i.IsWebbingMe || i.IsWarpScramblingMe))");
                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                Log("HasMicroWarpDrive");
                                ESCache.Instance.AbyssalCenter.Orbit(27000);
                                return true;
                            }

                            ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(27000);
                            return true;
                        }
                        **/

                        //if (ESCache.Instance.Modules.Any(i => i.IsOverloaded) && _secondsSinceLastSessionChange > 30)
                        //{
                        //    Log("if (ESCache.Instance.Modules.Any(i => i.IsOverloaded))");
                        //    ESCache.Instance.AbyssalCenter.Orbit(26000);
                        //    return true;
                        //}

                        /**
                        if (ESCache.Instance.ActiveShip.IsShieldTanked && .35 > ESCache.Instance.ActiveShip.Entity.ShieldPct)
                        {
                            Log("if (ESCache.Instance.ActiveShip.IsShieldTanked && .35 > ESCache.Instance.ActiveShip.Entity.ShieldPct)");
                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                Log("HasMicroWarpDrive");
                                ESCache.Instance.AbyssalCenter.Orbit(35000);
                                return true;
                            }

                            ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(35000);
                            return true;
                        }

                        if (Combat.PotentialCombatTargets.Count() == 1 && ESCache.Instance.ActiveShip.Entity.ShieldPct > .85)
                        {
                            Log("if (Combat.PotentialCombatTargets.Count() == 1)");
                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                Log("HasMicroWarpDrive");
                                ESCache.Instance.AbyssalCenter.Orbit(9000);
                                return true;
                            }

                            ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(9000);
                            return true;
                        }
                        **/

                        if (!Combat.PotentialCombatTargets.Any(i => i.IsTargetedBy || i.IsAttacking))
                        {
                            Log("boolShouldWeBeSpeedTanking: if (!Combat.PotentialCombatTargets.Any(i => i.IsTargetedBy || i.IsAttacking))");
                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                Log("HasMicroWarpDrive");
                                ESCache.Instance.AbyssalCenter.Orbit(9000);
                                return true;
                            }

                            ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(9000, ESCache.Instance.AbyssalCenter._directEntity);
                            return true;
                        }

                        if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                        {
                            Log("HasMicroWarpDrive");
                            ESCache.Instance.AbyssalCenter.Orbit(1400);
                            return true;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn ||
                            AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikVilaFrigateSwarmerSpawn ||
                            AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn ||
                            AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn ||
                            Combat.PotentialCombatTargets.Where(i => i.BracketType != BracketType.Large_Collidable_Structure).All(x => x.IsNPCFrigate))
                        {
                            if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(ESCache.Instance.AbyssalGate) > 10000))
                            {
                                Log("boolShouldWeBeSpeedTanking: if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(ESCache.Instance.AbyssalGate) > 10000))");
                                ESCache.Instance.AbyssalGate.Orbit(1400);
                                return true;
                            }

                            if (_getMTUInSpace != null && ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(_getMTUInSpace) > 10000))
                            {
                                Log("boolShouldWeBeSpeedTanking: if (_getMTUInSpace != null && ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(_getMTUInSpace) > 10000))");
                                _getMTUInSpace.Orbit(5000);
                                return true;
                            }

                            if (SafeSpotNearGate != null && ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(SafeSpotNearGate) > 10000))
                            {
                                Log("boolShouldWeBeSpeedTanking: if (SafeSpotNearGate != null && ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(SafeSpotNearGate) > 10000))");
                                SafeSpotNearGate.Orbit(5000);
                                return true;
                            }

                            if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(ESCache.Instance.AbyssalCenter) > 10000))
                            {
                                Log("boolShouldWeBeSpeedTanking: if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(ESCache.Instance.AbyssalCenter) > 10000))");
                                ESCache.Instance.AbyssalCenter.Orbit(5000);
                                return true;
                            }
                        }

                        ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(14000, ESCache.Instance.AbyssalCenter._directEntity);
                        return true;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn ||
                        AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikVilaFrigateSwarmerSpawn ||
                        AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn ||
                        AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn ||
                        Combat.PotentialCombatTargets.Where(i => i.BracketType != BracketType.Large_Collidable_Structure).All(x => x.IsNPCFrigate))
                    {
                        if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(ESCache.Instance.AbyssalGate) > 10000))
                        {
                            Log("if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(ESCache.Instance.AbyssalGate) > 10000))");
                            ESCache.Instance.AbyssalGate.Orbit(1400);
                            return true;
                        }

                        if (_getMTUInSpace != null && ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(_getMTUInSpace) > 10000))
                        {
                            Log("if (_getMTUInSpace != null && ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(_getMTUInSpace) > 10000))");
                            _getMTUInSpace.Orbit(5000);
                            return true;
                        }

                        if (SafeSpotNearGate != null && ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(SafeSpotNearGate) > 10000))
                        {
                            Log("if (SafeSpotNearGate != null && ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(SafeSpotNearGate) > 10000))");
                            SafeSpotNearGate.Orbit(5000);
                            return true;
                        }

                        if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(ESCache.Instance.AbyssalCenter) > 10000))
                        {
                            Log("if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(ESCache.Instance.AbyssalCenter) > 10000))");
                            ESCache.Instance.AbyssalCenter.Orbit(5000);
                            return true;
                        }
                    }
                }

            }

            /**
            if (boolShouldWeBeSpeedTanking)
            {
                if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure && i.IsTarget && i.IsActiveTarget))
                {
                    if (ESCache.Instance.ActiveShip.IsShieldTanked && (overHeatBooster || (1 > ESCache.Instance.ActiveShip.ShieldPercentage && 99 > ESCache.Instance.ActiveShip.ArmorPercentage)))
                    {
                        Log("We are in danger!?! Armor Damage! Trying to put some distance between us and our aggressor");
                        Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsTarget && i.IsActiveTarget)._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(Combat.MaxRange - 5000);
                        return true;
                    }

                    if (ESCache.Instance.ActiveShip.IsArmorTanked && (overHeatBooster || (1 > ESCache.Instance.ActiveShip.ShieldPercentage && 1 > ESCache.Instance.ActiveShip.ArmorPercentage && 99 > ESCache.Instance.ActiveShip.StructurePercentage)))
                    {
                        Log("We are in danger!?! Structure Damage! Trying to put some distance between us and our aggressor");
                        Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsTarget && i.IsActiveTarget)._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(Combat.MaxRange - 5000);
                        return true;
                    }
                }
            }
            **/

            if (DebugConfig.DebugNavigateOnGrid) Log("We are inside the arena and moving! Velocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "] m/s AbyssalCenter [" + ESCache.Instance.AbyssalCenter.Nearest1KDistance + "k] AbyssalGate [" + ESCache.Instance.AbyssalGate.Nearest1KDistance + "]");
            return false;
        }

        internal bool myTargetIsInRange
        {
            get
            {

                if (!Combat.PotentialCombatTargets.Any())
                    return false;

                if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure && i.IsTarget))
                    return false;

                //ignore this for karen spawn!
                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                    return true;

                if (!Combat.PotentialCombatTargets.Where(i => i.BracketType != BracketType.Large_Collidable_Structure && i.IsTarget && i.IsActiveTarget).Any(x => Combat.MaxWeaponRange - 3000 > x.Distance))
                    return false;

                if (DebugConfig.DebugNavigateOnGrid) Log("myTargetIsInRange [true]");
                return true;
            }
        }

        internal bool ShouldWeOrbitTheFleetLeader
        {
            get
            {
                try
                {
                    if (ESCache.Instance.EveAccount.UseFleetMgr && !ESCache.Instance.EveAccount.IsLeader && ESCache.Instance.DirectEve.FleetMembers.Any())
                    {
                        if (DebugConfig.DebugFleetMgr || DebugConfig.DebugNavigateOnGrid) Log("UseFleetMgr [" + ESCache.Instance.EveAccount.UseFleetMgr + "] IsLeader [" + ESCache.Instance.EveAccount.IsLeader + "] FleetMembers [" + ESCache.Instance.DirectEve.FleetMembers.Any() + "] DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");

                        foreach (DirectFleetMember individualFleetMember in ESCache.Instance.DirectEve.FleetMembers.Where(i => i.Name != ESCache.Instance.DirectEve.Me.Name))
                        {
                            if (DebugConfig.DebugFleetMgr || DebugConfig.DebugNavigateOnGrid) Log("individualFleetMember [" + individualFleetMember.Name + "]");
                            if (individualFleetMember.Entity != null)
                            {
                                if (individualFleetMember.Name == ESCache.Instance.EveAccount.LeaderCharacterName)
                                {
                                    if (individualFleetMember.Entity.IsPod)
                                        return false;

                                    if (individualFleetMember.Entity.IsWithinAbyssBounds((int)5000))
                                    {
                                        //always stay together for this spawn: we need to eliminate DPS quickly and staying together should do that?!
                                        //if (AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                                        //{
                                            //if (DirectEve.Interval(6000) && DebugConfig.DebugNavigateOnGrid) Log("if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn) return true");

                                            //if (Combat.PotentialCombatTargets.Any(i => i.IsAttacking && .7 > i.ShieldPct))
                                            //{
                                            //    if (DirectEve.Interval(6000) && DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsAttacking && .7 > i.ShieldPct)) return false");
                                            //    return false;
                                            //}
                                        //}

                                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                                        {
                                            if (DirectEve.Interval(6000) && DebugConfig.DebugNavigateOnGrid) Log("if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn) return false");
                                            return false;
                                        }

                                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn)
                                        {
                                            if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Devoted Knight")) >= 2)
                                            {
                                                if (DirectEve.Interval(6000) && DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(\"Devoted Knight\")) >= 2) return false");
                                                return false;
                                            }
                                        }

                                        if (DirectEve.Interval(6000) && DebugConfig.DebugNavigateOnGrid) Log("individualFleetMember [" + individualFleetMember.Name + "] Distance [" + Math.Round(individualFleetMember.Entity.Distance / 1000, 0) + "] Entity Exists!");
                                        if ((double)individualFleetMember.Entity.Distance > 15000)
                                        {
                                            if (DirectEve.Interval(7000))
                                            {
                                                Log("[" + individualFleetMember.Name + "] Orbiting at 500m");
                                                individualFleetMember.Entity.Orbit(500);
                                                return true;
                                            }

                                            return true;
                                        }

                                        return true;
                                    }
                                    else Log("individualFleetMember [" + individualFleetMember.Name + "] IsWithinAbyssBounds [false]");
                                }

                                continue;
                            }

                            continue;
                        }
                    }
                    else if (DirectEve.Interval(6000) && DebugConfig.DebugNavigateOnGrid) Log("!UseFleetMgr [" + ESCache.Instance.EveAccount.UseFleetMgr + "] IsLeader [" + ESCache.Instance.EveAccount.IsLeader + "] FleetMembers [" + ESCache.Instance.DirectEve.FleetMembers.Any() + "]!");

                    return false;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        internal bool HandleNoTargetsOrOutOfWeaponsRange
        {
            get
            {
                try
                {
                    if (!boolSpawnIsDangerousForSpeedTanksDontWaitForTargetsBeforeMovingOnGrid)
                    {
                        //
                        // No Targets: Orbit closest NPC at WeaponsRange - 4000
                        //
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (!boolSpawnIsDangerousForSpeedTanksDontWaitForTargetsBeforeMovingOnGrid)");
                        if (!ESCache.Instance.Targets.Any(i => i.GroupId != (int)Group.AssaultShip))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (!ESCache.Instance.Targets.Any())");
                            ESCache.Instance.AbyssalGate.Orbit(15000);
                            return true;
                        }

                        if (ESCache.Instance.Targets.Any(i => i.GroupId != (int)Group.AssaultShip))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.Targets.Any())");
                            if (Combat.PotentialCombatTargets.Any(i => i.IsTarget && i.IsActiveTarget))
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsTarget && i.IsActiveTarget))");
                                if (Combat.PotentialCombatTargets.Where(i => i.IsTarget && i.IsActiveTarget).Any(x => Combat.MaxWeaponRange > x.Distance))
                                {
                                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Where(i => i.IsTarget && i.IsActiveTarget).Any(x => Combat.MaxWeaponRange > x.Distance))");
                                    Combat.PotentialCombatTargets.Where(i => i.IsTarget && i.IsActiveTarget).FirstOrDefault(x => Combat.MaxWeaponRange > x.Distance).Orbit((int)Combat.MaxRange - 5000, false, "!!!");
                                    return true;
                                }
                            }
                        }

                        return false;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        internal bool boolThisisASpawnThatGenerallyDoesntSwitchTargetsOften
        {
            get
            {
                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser && i.Name.Contains("Devoted Knight")) >= 2)
                    {
                        return false;
                    }

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 4)
                    {
                        return false;
                    }

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 2)
                    {
                        return false;
                    }

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer) >= 2)
                    {
                        return false;
                    }

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser && i.TypeName.Contains("Lucifer Cynabal")))
                    {
                        return false;
                    }

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser && i.TypeName.Contains("Vedmak")) >= 3)
                    {
                        return false;
                    }

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 3)
                    {
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 3)
                    {
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 7)
                    {
                        return false;
                    }

                    return true;
                }

                return true;
            }
        }

        internal bool IsWreckLootable
        {
            get
            {
                if (!ESCache.Instance.Wrecks.Any())
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("No Wrecks exist yet");
                    return false;
                }

                if (ESCache.Instance.Wrecks.All(i => i.IsWreckEmpty))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.Wrecks.All(i => i.IsWreckEmpty))");
                    return false;
                }

                //fixme
                //what about extraction nodes?
                if (ESCache.Instance.Wrecks.Any(i => i.IsWreckEmpty && i.Name.ToLower().Contains("Cache Wreck".ToLower())))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.Wrecks.Any(i => i.IsWreckEmpty && i.Name.ToLower().Contains(\"Cache Wreck\".ToLower())))");
                    return false;
                }

                if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) && ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty && i.Name.ToLower().Contains("Cache Wreck".ToLower()) && i._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("Wreck is IsTooCloseToSmallDeviantAutomataSuppressor: wait to loot it till everything is dead");
                    return false;
                }

                if (Combat.KillTarget != null)
                {
                    if (!ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty && i.Name.ToLower().Contains("Cache Wreck".ToLower()) && (Combat.MaxRange - 2000) > i.DistanceTo(Combat.KillTarget)))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (!ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty && i.Name.ToLower().Contains(\"Cache Wreck\".ToLower()) && (Combat.MaxRange - 2000) > i.DistanceTo(Combat.KillTarget)))");
                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("return true;");
                    return true;
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("return true;");
                return true;
            }
        }

        internal bool HandleNoAggroLootWhileWeCan
        {
            get
            {
                if (!IsWreckLootable) return false;

                if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) && ESCache.Instance.Modules.Any(i => i.IsRemoteShieldRepairModule && i.IsActive))
                    return false;

                if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains("Lucifer Cynabal".ToLower())))
                    return false;

                if (!boolThisisASpawnThatGenerallyDoesntSwitchTargetsOften)
                {
                    if (Combat.PotentialCombatTargets.Any())
                        return false;
                }

                //
                // maybe a bad idea? If the BioAdaptiveCache is targeted but not in weapons range: get in range
                //
                if (Combat.PotentialCombatTargets.Any() && Combat.PotentialCombatTargets.Count() == 1 && Combat.PotentialCombatTargets.Where(i => i.IsAbyssalBioAdaptiveCache).Any(x => x.IsTarget && x.IsActiveTarget && !x.IsReadyToShoot))
                {
                    Log("Only thing left to kill is the cache and it is out of range: approaching");
                    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache)._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(Combat.MaxRange - 5000, Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache)._directEntity);
                    return true;
                }

                if (ESCache.Instance.EveAccount.UseFleetMgr && ESCache.Instance.EveAccount.IsLeader)
                    return false;

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution) //range is low, dont go wandering around ffs!
                    return false;

                if (2 >= Combat.PotentialCombatTargets.Where(i => i.BracketType != BracketType.Large_Collidable_Structure).Count(x => x._directEntity.IsAttacking))
                {
                    if (MoveIntoRangeOfLoot()) return true;
                    return false;
                }

                return false;
            }
        }

        int SpeedTank_OrbitDistance_Spawn_AbyssalOvermind
        {
            get
            {
                try
                {
                    //frigates and such
                    if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Vagabond)
                        return 5000;

                    if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                        return 22000;

                    if (ESCache.Instance.ActiveShip.IsFrigate)
                        return 5000;

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                    {
                        return 5000;
                    }

                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        return 500;
                    }

                    return (int)Combat.MaxRange - 22000;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return (int)Combat.MaxRange - 22000;
                }
            }
        }
        internal bool SpeedTank_AbyssalOvermindDroneBSSpawn()
        {
            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
            {
                var Battleship = Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCBattleship);
                if (Battleship != null)
                {
                    //if (Battleship._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor)
                    //{
                    //    Battleship.Orbit(500);
                    //    return true;
                    //}

                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        Battleship.Orbit(500);
                        return true;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))");
                    if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                        if (DirectEve.Interval(5000, 7000, Battleship.Id.ToString())) Battleship.Orbit(SpeedTank_OrbitDistance_Spawn_AbyssalOvermind, false, "IsNPCBattleship at [" + Battleship.Nearest1KDistance + "]");
                        return true;
                    }

                    if (DirectEve.Interval(5000, 7000, Battleship.Id.ToString())) Battleship.Orbit(SpeedTank_OrbitDistance_Spawn_AbyssalOvermind);
                    return true;
                }
            }

            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))");
                if (26000 > Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Distance)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (26000 > Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Distance)");
                    if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                        Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Orbit((int)Combat.MaxRange - 22000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                        return true;
                    }

                    Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Orbit((int)Combat.MaxRange - 22000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                    return true;
                }
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("return false");
            return false;
        }

        int SpeedTank_OrbitDistance_Spawn_DroneFrigateSpawn
        {
            get
            {
                try
                {
                    //if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                    //{
                    //    return 5000;
                    //}

                    //frigates and such
                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Vagabond)
                        return 5000;

                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        return 500;
                    }

                    return (int)Combat.MaxRange - 8000;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return (int)Combat.MaxRange - 8000;
                }
            }
        }
        internal bool SpeedTank_DroneFrigateSpawn()
        {
            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))");

                //https://everef.net/type/47859
                if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate && i.Name.ToLower().Contains("lance".ToLower())) >= 4) //blastneedle are 1/2 the damage of a blastlance! //	Maximum Velocity	1,500.00 m/sec
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate && i.Name.ToLower().Contains(\"lance\".ToLower())) >= 2)");

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && i.Name.ToLower().Contains("lance".ToLower()) && i.IsInOptimalRange))
                    {
                        if (DirectEve.Interval(5000))
                        {
                            Log("Notification!: if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && i.Name.ToLower().Contains(\"lance\".ToLower()) && i.IsInOptimalRange))");
                            if (PlayNotificationSounds) Util.PlayNoticeSound();
                        }
                    }

                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCFrigate || i.IsNPCDestroyer).Orbit(500);
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure && i.IsTarget && i.IsReadyToShoot))
                    {
                        //what is nothing is in range? in this case VERY unlikely!
                    }

                    if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.Any())
                    {
                        if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.Any(i => 50000 > i.DistanceTo(ESCache.Instance.AbyssalCenter)))
                        {
                            var speedCloud = ESCache.Instance.AbyssalDeadspaceTachyonClouds.Where(i => 50000 > i.DistanceTo(ESCache.Instance.AbyssalCenter)).OrderBy(i => i.Distance).FirstOrDefault();
                            if (DirectEve.Interval(15000)) Log("Move INTO the speed cloud at [" + speedCloud.Nearest1KDistance + "k] (and repeat!)");
                            speedCloud._directEntity.MoveToViaAStar();
                            return true;
                        }
                    }

                    if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");

                        if (45 > Time.Instance.SecondsSinceLastSessionChange)
                        {
                            if (MoveToUsingBestMethod(ESCache.Instance.AbyssalCenter, 70000, 80000)) return true;
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (MoveToUsingBestMethod(ESCache.Instance.AbyssalCenter, 70000, 80000)) returned false");
                        }

                        if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure && i.IsTarget && i.IsReadyToShoot))
                        {
                            ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(ESCache.Instance.SafeDistanceFromAbyssalCenter - 5000);
                            return true;
                        }

                        ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(ESCache.Instance.SafeDistanceFromAbyssalCenter - 5000);
                        return true;
                    }

                    if (45 > Time.Instance.SecondsSinceLastSessionChange)
                    {
                        if (MoveToUsingBestMethod(ESCache.Instance.AbyssalCenter, 70000, 80000)) return true;
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (MoveToUsingBestMethod(ESCache.Instance.AbyssalCenter, 70000, 80000)) returned false");
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure && i.IsTarget && i.IsReadyToShoot))
                    {
                        ESCache.Instance.AbyssalCenter.Orbit(61000);
                        return true;
                    }

                    ESCache.Instance.AbyssalCenter.Orbit(ESCache.Instance.SafeDistanceFromAbyssalCenter - 5000);
                    return true;
                }

                if (ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor != null)
                {
                    if (50 > ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor.Nearest1KDistance)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && i.IsAttacking) && Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 5)
                        {
                            ESCache.Instance.AbyssalDeadspaceSmallDeviantAutomataSuppressor.Orbit(12000);
                            return true;
                        }
                    }
                }

                if (Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer || i.IsNPCFrigate) >= 8)
                {
                    var closest = Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderByDescending(x => x._directEntity.IsWarpDisruptingEntity).ThenByDescending(y => y._directEntity.IsWebbingEntity).ThenByDescending(i => i._directEntity.IsWarpDisruptingMe).ThenBy(i => i.Distance).FirstOrDefault();
                    if (closest != null)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))");
                        if (32000 > closest.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (26000 > Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Distance)");
                            closest.Orbit(SpeedTank_OrbitDistance_Spawn_DroneFrigateSpawn); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                            return true;
                        }


                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                            closest._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange);
                            return true;
                        }

                        closest.Orbit(SpeedTank_OrbitDistance_Spawn_DroneFrigateSpawn); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                        return true;
                    }
                }

                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    if (Combat.PotentialCombatTargets.Any(i => 14000 > i.Distance))
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsAbyssalBioAdaptiveCache))
                        {
                            Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache).Orbit(500);
                            return true;
                        }

                        if (ESCache.Instance.Wrecks.Any() && ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                        {
                            ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty).Orbit(500);
                            return true;
                        }

                        _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500);
                        return true;
                    }

                    Combat.PotentialCombatTargets.OrderBy(i => i.Distance).FirstOrDefault().Orbit(500);
                    return true;
                }

                if (Combat.PotentialCombatTargets.All(i => i.BracketType != BracketType.Large_Collidable_Structure && !i.IsAttacking) && Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                {
                    Combat.PotentialCombatTargets.FirstOrDefault(i => i.BracketType != BracketType.Large_Collidable_Structure).Orbit(5000);
                    return true;
                }

                _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(1000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                return true;
            }


            if (DebugConfig.DebugNavigateOnGrid) Log("return false");
            return false;
        }

        int SpeedTank_OrbitDistance_Spawn_HighAngleDroneBattleCruiserSpawn
        {
            get
            {
                try
                {
                    //if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                    //{
                    //    return 5000;
                    //}

                    //frigates and such
                    if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Vagabond)
                        return 5000;

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                    {
                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            if (ESCache.Instance.ActiveShip.IsShieldTanked)
                            {
                                if (10 > ESCache.Instance.ActiveShip.ShieldPercentage)
                                {
                                    return 32000;
                                }
                            }
                            else if (ESCache.Instance.ActiveShip.IsArmorTanked)
                            {
                                if (10 > ESCache.Instance.ActiveShip.ArmorPercentage)
                                {
                                    return 32000;
                                }
                            }

                            return 14000;
                        }

                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            return (int)Combat.MaxRange - 3000;
                        }

                        if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                        {
                            return (int)Combat.MaxRange - 8000;
                        }

                        if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses) //for this spawn we use missiles only because drones get chewed up. we whould use missile range here!
                        {
                            //return Math.Max(22000, (int)Drones.MaxDroneRange);
                            return 17000;
                        }

                        return (int)Combat.MaxRange - 3000;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
                    {
                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            return 12000;
                        }

                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            return (int)Combat.MaxRange - 14000;
                        }

                        if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                            return Math.Max(15000, (int)Drones.MaxDroneRange);//was 22k

                        return (int)Combat.MaxRange - 14000;
                    }

                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        return 12000;
                    }

                    if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                        return Math.Min(17000, (int)Drones.MaxDroneRange);

                    return (int)Combat.MaxRange - 3000;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return (int)Combat.MaxRange - 3000;
                }
            }
        }
        internal bool SpeedTank_HighAngleDroneBattleCruiserSpawn()
        {
            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
            {
                var BattleCruiser = Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCBattlecruiser);

                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer) && 30000 > BattleCruiser.Distance)
                {
                    if (35 > Time.Instance.SecondsSinceLastSessionChange)
                    {
                        if (45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (45000 > closest.Distance && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            BattleCruiser.KeepAtRange(50000);
                            return true;
                        }
                    }
                }

                if (BattleCruiser != null)
                {
                    //if (BattleCruiser._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor)
                    //{
                    //    BattleCruiser.Orbit(500);
                    //    return true;
                    //}

                    if (DebugConfig.DebugNavigateOnGrid) Log("var BattleCruiser = Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCBattlecruiser);");
                    if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                            BattleCruiser._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(SpeedTank_OrbitDistance_Spawn_HighAngleDroneBattleCruiserSpawn);
                            return true;
                        }

                        BattleCruiser.Orbit(SpeedTank_OrbitDistance_Spawn_HighAngleDroneBattleCruiserSpawn);
                        return true;
                    }

                    if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud && !ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud && !ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                        if (!Combat.PotentialCombatTargets.Any(i => i.IsTarget))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (!Combat.PotentialCombatTargets.Any(i => i.IsTarget))");
                            BattleCruiser._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(SpeedTank_OrbitDistance_Spawn_HighAngleDroneBattleCruiserSpawn);
                            return true;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                        if (16000 > BattleCruiser.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("WAY TOO CLOSE: Run away a little! if (16000 > BattleCruiser.Distance)");
                            BattleCruiser.Orbit(SpeedTank_OrbitDistance_Spawn_HighAngleDroneBattleCruiserSpawn + 10000);
                            return true;
                        }

                        BattleCruiser.Orbit(SpeedTank_OrbitDistance_Spawn_HighAngleDroneBattleCruiserSpawn);
                        return true;
                    }

                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("HighAngleBattleCruiserSpawn: if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                        {
                            Combat.PotentialCombatTargets.Where(i => i.IsNPCBattlecruiser).OrderBy(x => x.Distance).FirstOrDefault().Orbit(18000);
                            return true;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("_moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500);");
                        _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500);
                        return true;
                    }

                    if (!WeHavePotentialCombatTargetsThatArentReadyToShoot && BattleCruiser.Distance > 19000)
                    {
                        _nextGate.Orbit(15000);
                        return true;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("BattleCruiser.Orbit(SpeedTank_OrbitDistance_Spawn_HighAngleDroneBattleCruiserSpawn);");
                    BattleCruiser.Orbit(SpeedTank_OrbitDistance_Spawn_HighAngleDroneBattleCruiserSpawn);
                    return true;
                }
            }

            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
            {
                //if (IsNPCDestroyerHuggingSuppressor)
                //{
                //    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCDestroyer).Orbit(500);
                //    return true;
                //}

                if (Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer) >= 3)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))");
                    var closestDestroyer = Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer).OrderByDescending(x => x._directEntity.IsWarpDisruptingEntity).ThenByDescending(y => y._directEntity.IsWebbingEntity).ThenByDescending(i => i._directEntity.IsWarpDisruptingMe).ThenBy(i => i.Distance).FirstOrDefault();
                    if (closestDestroyer != null)
                    {
                        if (40 > Time.Instance.SecondsSinceLastSessionChange && ESCache.Instance.ActiveShip.TypeId != (int)TypeID.Vagabond)
                        {
                            if (closestDestroyer._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (closestDestroyer._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closestDestroyer.KeepAtRange(55000);
                                return true;
                            }

                            if (closestDestroyer._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (closestDestroyer._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closestDestroyer.KeepAtRange(55000);
                                return true;
                            }

                            if (DebugConfig.DebugNavigateOnGrid) Log("if (45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closestDestroyer.Orbit(45000);
                            return true;
                        }

                        if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                        {
                            if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                                closestDestroyer._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(SpeedTank_OrbitDistance_Spawn_HighAngleDroneBattleCruiserSpawn);
                                return true;
                            }

                            if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                            closestDestroyer.Orbit(SpeedTank_OrbitDistance_Spawn_HighAngleDroneBattleCruiserSpawn);
                            return true;
                        }

                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                            closestDestroyer._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(SpeedTank_OrbitDistance_Spawn_HighAngleDroneBattleCruiserSpawn);
                            return true;
                        }

                        closestDestroyer.Orbit(SpeedTank_OrbitDistance_Spawn_HighAngleDroneBattleCruiserSpawn); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                        return true;
                    }
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("safe to orbit gate?!");
                _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
            }

            if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) > 3 || Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && !i.IsReadyToShoot))
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))");
                var closest = Combat.PotentialCombatTargets.Where(i => i.IsNPCFrigate).OrderByDescending(x => x._directEntity.IsWarpDisruptingEntity).ThenByDescending(y => y._directEntity.IsWebbingEntity).ThenByDescending(i => i._directEntity.IsWarpDisruptingMe).ThenBy(i => i.Distance).FirstOrDefault();
                if (closest != null)
                {
                    if (25 > Time.Instance.SecondsSinceLastSessionChange)
                    {
                        if (closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.KeepAtRange(55000);
                            return true;
                        }

                        if (closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.KeepAtRange(55000);
                            return true;
                        }

                        if (45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.KeepAtRange(50000);
                            return true;
                        }
                    }

                    if (ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds != null && ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.Any() && Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                    {
                        var BioluminesenceCloud = ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.OrderBy(i => i.Distance).FirstOrDefault();
                        if (DebugConfig.DebugNavigateOnGrid) Log("BioluminesenceCloud [" + BioluminesenceCloud.Nearest1KDistance + "] DistanceToGate [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.AbyssalGate) / 1000, 0) + "] DistanceToAbyssalCenter [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.AbyssalCenter) / 1000, 0) + "] DistanceToPlayerSpawnLocation [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.PlayerSpawnLocation) / 1000, 0) + "]");
                        if (ESCache.Instance.ActiveShip.IsLocatedWithinBioluminescenceCloud)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                            {
                                if (ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.All(i => i.DistanceTo(_nextGate) > 10000))
                                {
                                    Log("if (ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.All(i => i.DistanceTo(_nextGate) > 10000))");
                                    _nextGate.Orbit(5000);
                                    return true;
                                }

                                if (_getMTUInSpace != null && ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.All(i => i.DistanceTo(_getMTUInSpace) > 10000))
                                {
                                    Log("if (_getMTUInSpace != null && ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(_getMTUInSpace) > 10000))");
                                    _getMTUInSpace.Orbit(5000);
                                    return true;
                                }

                                if (SafeSpotNearGate != null && ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.All(i => i.DistanceTo(SafeSpotNearGate) > 10000))
                                {
                                    Log("if (SafeSpotNearGate != null && ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(SafeSpotNearGate) > 10000))");
                                    SafeSpotNearGate.Orbit(5000);
                                    return true;
                                }

                                if (ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.All(i => i.DistanceTo(ESCache.Instance.AbyssalCenter) > 10000))
                                {
                                    Log("if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(ESCache.Instance.AbyssalCenter) > 10000))");
                                    ESCache.Instance.AbyssalCenter.Orbit(5000);
                                    return true;
                                }

                                return true;
                            }
                        }
                    }

                    if (Combat.PotentialCombatTargets.Any(i => !i.IsReadyToShoot))
                    {
                        if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                            closest.Orbit((int)Combat.MaxRange - 21000);
                            return true;
                        }

                        closest.Orbit((int)Combat.MaxRange - 21000);
                        return true;
                    }

                    if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                        _moveToTarget_MTU_Wreck_Can_or_Gate_Entity._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 15000);
                        return true;
                    }

                    _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit((int)Combat.MaxRange - 21000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                    return true;
                }
            }

            if (ESCache.Instance.AbyssalMobileTractor != null)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                return true;
            }

            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
            {
                if (Combat.PotentialCombatTargets.Any(i => 14000 > i.Distance))
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsAbyssalBioAdaptiveCache))
                    {
                        Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache).Orbit(500);
                        return true;
                    }

                    if (ESCache.Instance.Wrecks.Any() && ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                    {
                        ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty).Orbit(500);
                        return true;
                    }

                    _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500);
                    return true;
                }

                Combat.PotentialCombatTargets.OrderBy(i => i.IsNPCBattlecruiser).ThenBy(x => x.Distance).FirstOrDefault().Orbit(500);
                return true;
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("return true");
            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
            return true;
        }

        internal bool SpeedTank_DrekavacBattleCruiserSpawn()
        {
            if (Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer) >= 2 || Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer && !i.IsReadyToShoot))
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))");
                var closestDestroyer = Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer).OrderByDescending(x => x._directEntity.IsWarpDisruptingEntity).ThenByDescending(y => y._directEntity.IsWebbingEntity).ThenByDescending(i => i._directEntity.IsWarpDisruptingMe).ThenBy(i => i.Distance).FirstOrDefault();
                if (closestDestroyer != null)
                {
                    if (25 > Time.Instance.SecondsSinceLastSessionChange)
                    {
                        if (closestDestroyer._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closestDestroyer.KeepAtRange(55000);
                            return true;
                        }

                        if (closestDestroyer._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closestDestroyer.KeepAtRange(55000);
                            return true;
                        }

                        if (45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (35000 > closest.Distance && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closestDestroyer.KeepAtRange(50000);
                            return true;
                        }
                    }

                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        closestDestroyer.Orbit(500);
                        return true;
                    }

                    if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                    {
                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                            closestDestroyer._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 14000);
                            return true;
                        }

                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            closestDestroyer.Orbit(500);
                            return true;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                        closestDestroyer.Orbit((int)Combat.MaxRange - 14000);
                        return true;
                    }

                    if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                        closestDestroyer._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 10000);
                        return true;
                    }

                    closestDestroyer.Orbit((int)Combat.MaxRange - 10000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                    return true;
                }

                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    if (Combat.PotentialCombatTargets.Any(i => 14000 > i.Distance))
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsAbyssalBioAdaptiveCache))
                        {
                            Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache).Orbit(500);
                            return true;
                        }

                        if (ESCache.Instance.Wrecks.Any() && ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                        {
                            ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty).Orbit(500);
                            return true;
                        }

                        _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500);
                        return true;
                    }

                    Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault().Orbit(500);
                    return true;
                }
            }

            if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 4 || Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && !i.IsReadyToShoot))
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.OrderBy(i => i.IsNPCFrigate).ThenBy(i => i.Distance).FirstOrDefault().Orbit(500);
                    return true;
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate))");
                Combat.PotentialCombatTargets.OrderBy(i => i.IsNPCFrigate).ThenBy(i => i.Distance).FirstOrDefault().Orbit((int)Combat.MaxRange - 21000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                return true;
            }

            if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) >= 2 || Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser && !i.IsReadyToShoot))
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))");

                //if (IsKarenHuggingSuppressor)
                //{
                //    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattlecruiser).Orbit(500);
                //    return true;
                //}

                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    if (Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCBattlecruiser).Distance > 30000)
                    {
                        Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCBattlecruiser).ApproachUsingMoveToAStar();
                        return true;
                    }

                    Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCBattlecruiser).Orbit(14000);
                    return true;
                }

                if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                    Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCBattlecruiser).Orbit((int)Combat.MaxRange - 18000, false, "IsNPCBattlecruiser is too close: they are inside 27k");
                    return true;
                }

                Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCBattlecruiser)._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 18000);
                return true;

            }

            if (ESCache.Instance.AbyssalMobileTractor != null)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                return true;
            }

            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
            {
                Combat.PotentialCombatTargets.OrderBy(i => i.IsNPCBattlecruiser).ThenBy(x => x.Distance).FirstOrDefault().Orbit(13000);
                return true;
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("Only 1 BC left");
            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
            return true;
        }

        private bool OrbitWithinLightningRange = false;

        internal bool SpeedTank_ConcordSpawn()
        {
            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCBattleship).Orbit(500);
                    return true;
                }

                if (45 > Time.Instance.SecondsSinceLastSessionChange && !ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                {
                    if (MoveToUsingBestMethod(ESCache.Instance.AbyssalCenter, 70000, 80000)) return true;
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (MoveToUsingBestMethod(ESCache.Instance.AbyssalCenter, 70000, 80000)) returned false");
                }

                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                {

                    if (ESCache.Instance.Entities.Any(i => i.IsAbyssalDeadspaceTriglavianExtractionNode && 30000 > i.DistanceTo(Combat.PotentialCombatTargets.Where(x => x.IsNPCBattleship && x.Distance > 30000).FirstOrDefault())))
                    {
                        var extractionNodeToGetCloserTo = ESCache.Instance.Entities.FirstOrDefault(i => i.IsAbyssalDeadspaceTriglavianExtractionNode && 30000 > i.DistanceTo(Combat.PotentialCombatTargets.Where(x => x.IsNPCBattleship && x.Distance > 30000).FirstOrDefault()));
                        if (extractionNodeToGetCloserTo != null && extractionNodeToGetCloserTo.Distance > 10000)
                        {
                            extractionNodeToGetCloserTo.Orbit(1000);
                            return true;
                        }
                    }

                    EntityCache NPCBS = Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship);
                    //we need to get close because Karen is within range of a suppressor: if we dont we will never apply enough DPS
                    if (NPCBS._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor)
                    {
                        NPCBS.Orbit(500);
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                    {
                        if (!ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses || (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses && Drones.AllDronesInSpace.Any(i => !i.IsAttacking)))
                        {
                            if (35000 > ESCache.Instance.AbyssalCenter.Distance || 40000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Distance) //if we arent this close to the abyssalcenter wait until we are before getting closer to karen
                            {
                                if (30000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Distance)
                                {
                                    if (20000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Distance)
                                    {
                                        if (10000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Distance)
                                        {
                                            Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(5000);
                                            return true;
                                        }

                                        Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(5000);
                                        return true;
                                    }

                                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                                    {
                                        Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(14000);
                                        return true;
                                    }

                                    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(15000);
                                    return true;
                                }

                                if (DebugConfig.DebugNavigateOnGrid) Log("!if (30000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Distance): OrbitWithHighTransversal(25000)");
                                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                                {
                                    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(15000);
                                    return true;
                                }

                                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                                {
                                    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(20000);
                                    return true;
                                }

                                Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(25000);
                                return true;
                            }
                        }
                    }

                    if (Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Distance > Combat.MaxRange - 6000 || !Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).IsTarget)
                    {
                        if (30000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).DistanceTo(_nextGate))
                        {
                            Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship)._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(5000);
                            return true;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Distance [" + Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Nearest1KDistance + "] > Combat.MaxRange - 6000 [" + (Combat.MaxRange - 6000) + "] || !Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).IsTarget): AbyssalCenter OrbitWithHighTransversal @ 15000;: Karen is [" + Math.Round(Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).DistanceTo(ESCache.Instance.AbyssalGate) / 1000, 0) + "]k from gate");
                        Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(500);
                        return true;
                    }

                    if (ESCache.Instance.AbyssalGate.Distance > 55000)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (AbyssalGate Distance [" + ESCache.Instance.AbyssalGate.Nearest1KDistance + "] > 45000) AbyssalCenter OrbitWithHighTransversal @ 15000;: Karen is [" + Math.Round(Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).DistanceTo(ESCache.Instance.AbyssalGate) / 1000, 0) + "]k from gate");
                        ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000);
                        return true;
                    }

                    if (ESCache.Instance.AbyssalMobileTractor != null)
                    {
                        ESCache.Instance.AbyssalMobileTractor.Orbit(7000);
                        return true;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("AbyssalGate OrbitWithHighTransversal @ 10000: Karen is [" + Math.Round(Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).DistanceTo(ESCache.Instance.AbyssalGate) / 1000, 0) + "]k from gate");
                    _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(7000);
                    return true;
                }
            }

            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate))
            {
                if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 3 || Combat.PotentialCombatTargets.Any(i => (i.IsNPCFrigate) && !i.IsReadyToShoot))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate))");
                    if (51000 > Combat.PotentialCombatTargets.Where(i => i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Distance)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (31000 > Combat.PotentialCombatTargets.Where(i => i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Distance)");
                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            Combat.PotentialCombatTargets.Where(i => i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Orbit(9000);
                            return true;
                        }

                        var orbitDistanceToUse = (int)Combat.MaxRange - 12000;
                        if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                            orbitDistanceToUse = Math.Min(22000, (int)Drones.MaxDroneRange);

                        if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses && OrbitWithinLightningRange && Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Skybreaker")))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses && OrbitWithinLightningRange && Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 2)");
                            Log("OrbitWithinLightningRange [" + OrbitWithinLightningRange + "] - orbiting Skybreaker at very close range so that his lightning damage kills the other NPCs");
                            orbitDistanceToUse = 500;
                            return true;
                        }

                        Combat.PotentialCombatTargets.Where(i => i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Orbit(orbitDistanceToUse); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                        return true;
                    }
                }

                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Skybreaker")))
                    {
                        Combat.PotentialCombatTargets.FirstOrDefault(i => i.Name.Contains("Skybreaker")).Orbit(500);
                        return true;
                    }

                    Combat.PotentialCombatTargets.Where(i => i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Orbit(9000);
                    return true;
                }

                if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Skybreaker")))
                    {
                        var skyBreakerOrbitDistanceToUse = 15000; //outside skybreaker lighting damage. if within 10k if your ship your drones can get hit by the chain lightning!
                        Combat.PotentialCombatTargets.FirstOrDefault(i => i.Name.Contains("Skybreaker")).Orbit(skyBreakerOrbitDistanceToUse);
                        return true;
                    }

                    var orbitDistanceToUse = 15000; //what range should we orbit the other frigs?
                    Combat.PotentialCombatTargets.Where(i => i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Orbit(orbitDistanceToUse);
                    return true;
                }
            }

            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
            {
                if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 2 || Combat.PotentialCombatTargets.Any(i => !i.IsReadyToShoot))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))");
                    EntityCache NPCCruiser = Combat.PotentialCombatTargets.Where(i => i.IsNPCCruiser).OrderBy(i => i._directEntity.NPCHasVortonProjectorGuns).ThenBy(i => i.Distance).FirstOrDefault();
                    if (NPCCruiser != null)
                    {
                        //if (NPCCruiser._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor)
                        //{
                        //    NPCCruiser.Orbit(500);
                        //    return true;
                        //}

                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            NPCCruiser.Orbit(500);
                            return true;
                        }

                        if (45000 > NPCCruiser.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (29000 > CruiserThatIsTooClose.Distance)");
                            if (25000 > NPCCruiser.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (25000 > CruiserThatIsTooClose.Distance)");
                                NPCCruiser.Orbit((int)Combat.MaxRange - 12000);
                                return true;
                            }

                            if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                                NPCCruiser._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 8000);
                                return true;
                            }

                            if (DebugConfig.DebugNavigateOnGrid) Log("CruiserThatIsTooClose.Orbit((int)Combat.MaxRange - 4000, true, \"IsNPCCruiser is too close: they are inside 27k\");");
                            NPCCruiser.Orbit((int)Combat.MaxRange - 13000, true, "IsNPCCruiser is too close: they are inside 27k");
                            return true;
                        }
                    }
                }


                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCCruiser).Orbit(500);
                    return true;
                }
            }

            if (Combat.PotentialCombatTargets.Any())
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.FirstOrDefault().Orbit(500);
                    return true;
                }
            }

            if (ESCache.Instance.AbyssalMobileTractor != null)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                return true;
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("return true");
            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
            return true;
        }

        internal bool SpeedTank_DamavikFrigateSpawn()
        {
            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))");
                if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate || i.IsNPCDestroyer) > 5 || !Combat.PotentialCombatTargets.All(i => i.IsReadyToTarget))
                {
                    var closest = Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderByDescending(x => x._directEntity.IsWarpDisruptingEntity).ThenByDescending(y => y._directEntity.IsWebbingEntity).ThenByDescending(i => i._directEntity.IsWarpDisruptingMe).ThenBy(i => i.Distance).FirstOrDefault();
                    if (closest != null)
                    {
                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            if (Combat.PotentialCombatTargets.All(i => 14000 > i.Distance))
                            {
                                if (Combat.PotentialCombatTargets.Any(i => i.IsAbyssalBioAdaptiveCache))
                                {
                                    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache).Orbit(500);
                                    return true;
                                }

                                if (ESCache.Instance.Wrecks.Any() && ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                                {
                                    ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty).Orbit(500);
                                    return true;
                                }

                                _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500);
                                return true;
                            }

                            closest.Orbit(500);
                            return true;
                        }

                        if (25 > Time.Instance.SecondsSinceLastSessionChange)
                        {
                            if (closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closest.KeepAtRange(55000);
                                return true;
                            }

                            if (closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closest.KeepAtRange(55000);
                                return true;
                            }

                            if (45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closest.KeepAtRange(50000);
                                return true;
                            }
                        }

                        if ((ESCache.Instance.ActiveShip.IsScrambled && ESCache.Instance.ActiveShip.HasMicroWarpDrive) || ESCache.Instance.ActiveShip.IsWebbed)
                        {
                            if (closest.Velocity > ESCache.Instance.ActiveShip.Entity.Velocity)
                            {
                                if (_nextGate.Distance > 10000)
                                {
                                    _nextGate.Orbit(5000);
                                    return true;
                                }

                                closest.KeepAtRange(18000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                                return true;
                            }
                        }

                        if (ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds != null && ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.Any() && Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                        {
                            var BioluminesenceCloud = ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.OrderBy(i => i.Distance).FirstOrDefault();
                            if (DebugConfig.DebugNavigateOnGrid) Log("BioluminesenceCloud [" + BioluminesenceCloud.Nearest1KDistance + "] DistanceToGate [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.AbyssalGate) / 1000, 0) + "] DistanceToAbyssalCenter [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.AbyssalCenter) / 1000, 0) + "] DistanceToPlayerSpawnLocation [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.PlayerSpawnLocation) / 1000, 0) + "]");
                            if (50000 > BioluminesenceCloud.Distance && ESCache.Instance.SafeDistanceFromAbyssalCenter > BioluminesenceCloud._directEntity.DistanceTo(ESCache.Instance.AbyssalCenter._directEntity))
                            {
                                if (Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                                {
                                    if (DirectEve.Interval(7000, 7000, BioluminesenceCloud.Id.ToString())) BioluminesenceCloud.Orbit(5000);
                                    return true;
                                }
                            }
                        }

                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                            closest._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 20000);
                            return true;
                        }

                        if (Combat.PotentialCombatTargets.All(i => 10000 > i.Distance))
                        {
                            if (ESCache.Instance.AbyssalMobileTractor != null)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                                ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                                return true;
                            }

                            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(10000);
                            return true;
                        }

                        var orbitDistanceToUse = (int)Combat.MaxRange - 5000;
                        if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                            orbitDistanceToUse = Math.Min(22000, (int)Drones.MaxDroneRange);

                        if (ESCache.Instance.ActiveShip.IsShieldTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage > ESCache.Instance.ActiveShip.ShieldPercentage && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsShieldTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage [" + _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage + "] > ESCache.Instance.ActiveShip.ShieldPercentage [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "]) Run Away till shield recovers!");
                            orbitDistanceToUse = 45000;//our targeting range is only 32k, our missile range is?
                        }

                        if (ESCache.Instance.ActiveShip.IsArmorTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage > ESCache.Instance.ActiveShip.ArmorPercentage && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsShieldTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage [" + _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage + "] > ESCache.Instance.ActiveShip.ShieldPercentage [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "]) Run Away till shield recovers!");
                            orbitDistanceToUse = 45000;//our targeting range is only 32k, our missile range is?
                        }

                        closest.Orbit(orbitDistanceToUse); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                        return true;
                    }
                }

                if (ESCache.Instance.ActiveShip.IsShieldTanked && 60 > ESCache.Instance.ActiveShip.ShieldPercentage)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsShieldTanked && 60 > ESCache.Instance.ActiveShip.ShieldPercentage [" + ESCache.Instance.ActiveShip.ShieldPercentage + "])");
                    var closest2 = Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault();
                    if (closest2 != null)
                    {
                        var orbitDistanceToUse = (int)Combat.MaxRange - 5000;
                        if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                            orbitDistanceToUse = Math.Min(22000, (int)Drones.MaxDroneRange);

                        closest2.Orbit(orbitDistanceToUse);
                        return true;
                    }
                }

                if (ESCache.Instance.AbyssalMobileTractor != null)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                    ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                    return true;
                }

                _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
                return true;
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("return false");
            return false;
        }

        internal bool SpeedTank_EphialtesCruiserSpawn()
        {
            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
            {
                if (ESCache.Instance.ActiveShip.IsShieldTanked && 60 > ESCache.Instance.ActiveShip.ShieldPercentage)
                {
                    var closest2 = Combat.PotentialCombatTargets.OrderBy(i => i.Distance).FirstOrDefault();
                    if (closest2 != null)
                    {
                        var orbitDistanceToUse = (int)Combat.MaxRange - 5000;
                        if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                            orbitDistanceToUse = Math.Min(22000, (int)Drones.MaxDroneRange);

                        closest2.Orbit(orbitDistanceToUse);
                        return true;
                    }
                }

                EntityCache Cruiser = null;
                if (Combat.PotentialCombatTargets.Count(x => x.IsNPCCruiser) >= 3 || !Combat.PotentialCombatTargets.All(i => i.IsReadyToTarget))
                {
                    Cruiser = Combat.PotentialCombatTargets.Where(x => x.IsNPCCruiser).OrderBy(y => y.Distance).FirstOrDefault();
                    if (Cruiser != null)
                    {
                        if (3 > Combat.PotentialCombatTargets.Count(x => x.IsNPCCruiser && x.IsReadyToShoot))
                        {
                            Cruiser = Combat.PotentialCombatTargets.Where(x => x.IsNPCCruiser).OrderBy(y => y.Distance).Skip(1).FirstOrDefault();
                            if (Cruiser != null)
                            {
                                if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                                    Cruiser._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 7000);
                                    return true;
                                }

                                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                                {
                                    Cruiser.Orbit(500);
                                    return true;
                                }

                                Cruiser.Orbit((int)Combat.MaxRange - 9000);
                                return true;
                            }
                        }

                        if (Cruiser != null)
                        {
                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                if (Combat.PotentialCombatTargets.Any(i => 14000 > i.Distance))
                                {
                                    if (Combat.PotentialCombatTargets.Any(i => i.IsAbyssalBioAdaptiveCache))
                                    {
                                        Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache).Orbit(500);
                                        return true;
                                    }

                                    if (ESCache.Instance.Wrecks.Any() && ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                                    {
                                        ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty).Orbit(500);
                                        return true;
                                    }

                                    _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500);
                                    return true;
                                }

                                if (Cruiser != null)
                                {
                                    Cruiser.Orbit(3000);
                                    return true;
                                }
                            }
                        }
                    }
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))");
                //if (DebugConfig.DebugNavigateOnGrid) Log("[" + Cruiser.TypeName + "] Orbit at [" + (Combat.MaxRange - 6000) + "];");
                if (ESCache.Instance.AbyssalMobileTractor != null)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                    ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                    return true;
                }

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault().Orbit(500);
                        return true;
                    }
                }

                _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
                return true;
            }

            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
            {
                if (Combat.PotentialCombatTargets.Any(i => 14000 > i.Distance))
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsAbyssalBioAdaptiveCache))
                    {
                        Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache).Orbit(500);
                        return true;
                    }

                    if (ESCache.Instance.Wrecks.Any() && ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                    {
                        ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty).Orbit(500);
                        return true;
                    }

                    _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500);
                    return true;
                }

                Combat.PotentialCombatTargets.OrderBy(i => i.Distance).FirstOrDefault().Orbit(500);
                return true;
            }

            if (ESCache.Instance.AbyssalMobileTractor != null)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                return true;
            }

            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
            return true;
        }

        internal bool SpeedTank_DevotedCruiserSpawn()
        {
            if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains("Devoted Hunter".ToLower())))
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains(Devoted Hunter.ToLower())))");
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                    Combat.PotentialCombatTargets.Where(x => x.Name.ToLower().Contains("Devoted Hunter".ToLower())).OrderBy(i => i.Distance).FirstOrDefault().Orbit(18000);
                    return true;
                }

                if (27000 > Combat.PotentialCombatTargets.Where(i => i.Name.ToLower().Contains("Devoted Hunter".ToLower())).OrderBy(i => i.Distance).FirstOrDefault().Distance)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (27000 > Combat.PotentialCombatTargets.Where(i => i.Name.ToLower().Contains(Devoted Hunter.ToLower())).OrderBy(i => i.Distance).FirstOrDefault().Distance)");
                    var orbitDistanceToUse = (int)Combat.MaxRange - 5000;
                    if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                        orbitDistanceToUse = Math.Min(28000, (int)Drones.MaxDroneRange);

                    if (ESCache.Instance.ActiveShip.IsShieldTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage > ESCache.Instance.ActiveShip.ShieldPercentage && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsShieldTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage [" + _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage + "] > ESCache.Instance.ActiveShip.ShieldPercentage [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "]) Run Away till shield recovers!");
                        orbitDistanceToUse = 45000;//our targeting range is only 32k, our missile range is?
                    }

                    if (ESCache.Instance.ActiveShip.IsArmorTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage > ESCache.Instance.ActiveShip.ArmorPercentage && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsArmorTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage [" + _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage + "] > ESCache.Instance.ActiveShip.ArmorPercentage [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "]) Run Away till armor recovers!");
                        orbitDistanceToUse = 45000;//our targeting range is only 32k, our missile range is?
                    }

                    Combat.PotentialCombatTargets.Where(i => i.Name.ToLower().Contains("Devoted Hunter".ToLower())).OrderBy(i => i.Distance).FirstOrDefault().Orbit(orbitDistanceToUse); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                    return true;
                }
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("No Devoted Hunters?");

            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))");
                var closest = Combat.PotentialCombatTargets.Where(i => i.IsNPCCruiser && i.Name.ToLower().Contains("Devoted Knight".ToLower())).OrderBy(i => i.Distance).FirstOrDefault();
                if (closest != null)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (closest != null)");
                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)");
                        closest.Orbit(10000); //anything closer and we will probably die
                        return true;
                    }

                    if (25 > Time.Instance.SecondsSinceLastSessionChange)
                    {
                        if (closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.KeepAtRange(55000);
                            return true;
                        }

                        if (closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.KeepAtRange(55000);
                            return true;
                        }

                        if (45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.KeepAtRange(50000);
                            return true;
                        }
                    }
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))");
                if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains("Devoted Knight".ToLower())))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains(\"Devoted Knight\".ToLower())))");

                    if (37000 > Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.Name.ToLower().Contains("Devoted Knight".ToLower())).Distance)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (37000 > Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.Name.ToLower().Contains(Devoted Knight.ToLower())).Distance)");
                        var orbitDistanceToUse = (int)Math.Max((double)Combat.MaxRange, 32000);
                        if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                            orbitDistanceToUse = Math.Min(32500, (int)Drones.MaxDroneRange);//30k was working but worm got into 1/2 shields
                        if (ESCache.Instance.ActiveShip.IsAssaultShip)
                        {
                            orbitDistanceToUse = Math.Max((int)Combat.MaxRange, 30000);//our targeting range is only 32k, our missile range is?
                            if (ESCache.Instance.EveAccount.UseFleetMgr && Combat.PotentialCombatTargets.Count(i => i.Name.ToLower().Contains("Devoted Knight".ToLower()) && i.IsAttacking) >= 2)
                            {
                                if (ESCache.Instance.ActiveShip.IsShieldTanked && .70 > ESCache.Instance.ActiveShip.ShieldPercentage && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                                {
                                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsShieldTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage [" + _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage + "] > ESCache.Instance.ActiveShip.ShieldPercentage [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "]) Run Away till shield recovers!");
                                    orbitDistanceToUse = 45000;//our targeting range is only 32k, our missile range is?
                                }

                                if (ESCache.Instance.ActiveShip.IsArmorTanked && .70 > ESCache.Instance.ActiveShip.ArmorPercentage && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                                {
                                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsArmorTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage [" + _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage + "] > ESCache.Instance.ActiveShip.ArmorPercentage [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "]) Run Away till armor recovers!");
                                    orbitDistanceToUse = 45000;//our targeting range is only 32k, our missile range is?
                                }
                            }
                        }

                        if (ESCache.Instance.ActiveShip.IsShieldTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage > ESCache.Instance.ActiveShip.ShieldPercentage && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsShieldTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage [" + _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage + "] > ESCache.Instance.ActiveShip.ShieldPercentage [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "]) Run Away till shield recovers!");
                            orbitDistanceToUse = 45000;//our targeting range is only 32k, our missile range is?
                        }

                        if (ESCache.Instance.ActiveShip.IsArmorTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage > ESCache.Instance.ActiveShip.ArmorPercentage && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsArmorTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage [" + _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage + "] > ESCache.Instance.ActiveShip.ArmorPercentage [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "]) Run Away till armor recovers!");
                            orbitDistanceToUse = 45000;//our targeting range is only 32k, our missile range is?
                        }

                        Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.Name.ToLower().Contains("Devoted Knight".ToLower())).Orbit(orbitDistanceToUse); //out of the devoted knits range, 20k is too close!
                        return true;
                    }
                }

                if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCCruiser)._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 13000);
                    return true;
                }

                closest = Combat.PotentialCombatTargets.Where(i => i.IsNPCCruiser).OrderBy(i => i.Distance).FirstOrDefault();
                if (closest != null)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (closest != null)!!");
                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)!!");
                        closest.Orbit(500);
                        return true;
                    }
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCCruiser).Orbit((int)Combat.MaxRange - 4000);");
                Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCCruiser).Orbit((int)Combat.MaxRange - 13000);
                return true;
            }

            if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) > 3 || Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && !i.IsReadyToShoot))
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))");
                var closest = Combat.PotentialCombatTargets.Where(i => i.IsNPCFrigate).OrderByDescending(x => x._directEntity.IsWarpDisruptingEntity).ThenByDescending(y => y._directEntity.IsWebbingEntity).ThenByDescending(i => i._directEntity.IsWarpDisruptingMe).ThenBy(i => i.Distance).FirstOrDefault();
                if (closest != null)
                {
                    if (ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds != null && ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.Any() && Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                    {
                        var BioluminesenceCloud = ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.OrderBy(i => i.Distance).FirstOrDefault();
                        if (DebugConfig.DebugNavigateOnGrid) Log("BioluminesenceCloud [" + BioluminesenceCloud.Nearest1KDistance + "] DistanceToGate [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.AbyssalGate) / 1000, 0) + "] DistanceToAbyssalCenter [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.AbyssalCenter) / 1000, 0) + "] DistanceToPlayerSpawnLocation [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.PlayerSpawnLocation) / 1000, 0) + "]");
                        if (50000 > BioluminesenceCloud.Distance && ESCache.Instance.SafeDistanceFromAbyssalCenter > BioluminesenceCloud._directEntity.DistanceTo(ESCache.Instance.AbyssalCenter._directEntity))
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                            {
                                if (DirectEve.Interval(7000, 7000, BioluminesenceCloud.Id.ToString())) BioluminesenceCloud.Orbit(5000);
                                return true;
                            }
                        }
                    }

                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        closest.Orbit(500);
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => !i.IsReadyToShoot))
                    {
                        if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                            closest.Orbit((int)Combat.MaxRange - 21000);
                            return true;
                        }

                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            closest.Orbit(500);
                            return true;
                        }

                        closest.Orbit((int)Combat.MaxRange - 21000);
                        return true;
                    }

                    if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                        _moveToTarget_MTU_Wreck_Can_or_Gate_Entity._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 15000);
                        return true;
                    }

                    if (ESCache.Instance.AbyssalMobileTractor != null)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                        ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                        return true;
                    }

                    _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit((int)Combat.MaxRange - 21000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                    return true;
                }
            }

            if (ESCache.Instance.AbyssalMobileTractor != null)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                return true;
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("return true");
            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
            return true;
        }

        internal bool SpeedTank_KarybdisTyrannosSpawn()
        {
            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser) && (!ESCache.Instance.ActiveShip.IsFrigate))
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500);
                    return true;
                }

                var closest = Combat.PotentialCombatTargets.Where(i => i.IsNPCCruiser).OrderByDescending(x => x._directEntity.IsWarpDisruptingEntity).ThenByDescending(y => y._directEntity.IsWebbingEntity).ThenByDescending(i => i._directEntity.IsWarpDisruptingMe).ThenBy(i => i.Distance).FirstOrDefault();
                if (closest != null)
                {
                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        closest.Orbit(500);
                        return true;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))");
                    if (55 > Time.Instance.SecondsSinceLastSessionChange)
                    {
                        if (closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.Orbit(65000);
                            return true;
                        }

                        if (closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.Orbit(65000);
                            return true;
                        }

                        if (45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.Orbit(60000);
                            return true;
                        }
                    }

                    if (23000 > closest.Distance)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (26000 > closest.Distance)");
                        if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                            closest.Orbit((int)Combat.MaxRange - 16000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                            return true;
                        }

                        closest._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 16000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                        return true;
                    }
                }
            }

            if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsAbyssalKaren))
            {
                EntityCache Karen = Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren);
                //we need to get close because Karen is within range of a suppressor: if we dont we will never apply enough DPS
                if (Karen._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor)
                {
                    if (DirectEve.Interval(15000))
                    {
                        string msg = "Notification: Karen IsTooCloseToSmallDeviantAutomataSuppressor";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);

                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }

                if (Karen._directEntity.IsInSpeedCloud)
                {
                    if (DirectEve.Interval(15000))
                    {
                        string msg = "Notification: Karen is in a speed cloud!";
                        DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                        Log(msg);

                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                        if (PlayNotificationSounds) Util.PlayNoticeSound();
                    }
                }

                if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsAbyssalKaren))
                {
                    if (!ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses || (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses && Drones.AllDronesInSpace.Any(i => !i.IsAttacking)))
                    {
                        KarenSpeedTank();
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Distance > Combat.MaxRange - 6000 || !Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).IsTarget)
                    {
                        if (30000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).DistanceTo(_nextGate))
                        {
                            Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren)._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(5000);
                            return true;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Distance [" + Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Nearest1KDistance + "] > Combat.MaxRange - 6000 [" + (Combat.MaxRange - 6000) + "] || !Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).IsTarget): AbyssalCenter OrbitWithHighTransversal @ 15000;: Karen is [" + Math.Round(Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).DistanceTo(ESCache.Instance.AbyssalGate) / 1000, 0) + "]k from gate");
                        KarenSpeedTank();
                        return true;
                    }
                }

                if (ESCache.Instance.AbyssalGate.Distance > 55000)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (AbyssalGate Distance [" + ESCache.Instance.AbyssalGate.Nearest1KDistance + "] > 45000) AbyssalCenter OrbitWithHighTransversal @ 15000;: Karen is [" + Math.Round(Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).DistanceTo(ESCache.Instance.AbyssalGate) / 1000, 0) + "]k from gate");
                    ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000);
                    return true;
                }

                if (ESCache.Instance.AbyssalMobileTractor != null)
                {
                    ESCache.Instance.AbyssalMobileTractor.Orbit(7000);
                    return true;
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("AbyssalGate OrbitWithHighTransversal @ 10000: Karen is [" + Math.Round(Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).DistanceTo(ESCache.Instance.AbyssalGate) / 1000, 0) + "]k from gate");
                _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(7000);
                return true;
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("return false;");
            return false;
        }


        private void KarenSpeedTank()
        {
            if (35000 > ESCache.Instance.AbyssalCenter.Distance || 40000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Distance) //if we arent this close to the abyssalcenter wait until we are before getting closer to karen
            {
                if (30000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Distance)
                {
                    if (20000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Distance)
                    {
                        if (10000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Distance)
                        {
                            Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Orbit(500);
                            return;
                        }

                        Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Orbit(5000);
                        return;
                    }

                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Orbit(14000);
                        return;
                    }

                    Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Orbit(15000);
                    return;
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("!if (30000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Distance): OrbitWithHighTransversal(25000)");
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                {
                    Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Orbit(15000);
                    return;
                }

                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Orbit(20000);
                    return;
                }

                Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Orbit(25000);
                return;
            }

            if (ESCache.Instance.AbyssalGate.Distance > 55000)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (AbyssalGate Distance [" + ESCache.Instance.AbyssalGate.Nearest1KDistance + "] > 45000) AbyssalCenter OrbitWithHighTransversal @ 15000;: Karen is [" + Math.Round(Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).DistanceTo(ESCache.Instance.AbyssalGate) / 1000, 0) + "]k from gate");
                ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000);
                return;
            }

            if (ESCache.Instance.AbyssalMobileTractor != null)
            {
                ESCache.Instance.AbyssalMobileTractor.Orbit(7000);
                return;
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("AbyssalGate OrbitWithHighTransversal @ 10000: Karen is [" + Math.Round(Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).DistanceTo(ESCache.Instance.AbyssalGate) / 1000, 0) + "]k from gate");
            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(7000);
            return;
        }


        internal bool SpeedTank_KikimoraDestroyerSpawn()
        {
            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
            {
                //if (IsNPCDestroyerHuggingSuppressor)
                //{
                //    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCDestroyer).Orbit(500);
                //    return true;
                //}

                var closestDestroyer = Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer).OrderByDescending(x => ESCache.Instance.ActiveShip.HasMicroWarpDrive && x._directEntity.IsWarpDisruptingEntity).ThenByDescending(y => y._directEntity.IsWebbingEntity).ThenByDescending(i => ESCache.Instance.ActiveShip.HasMicroWarpDrive && i._directEntity.IsWarpDisruptingMe).ThenBy(i => i.Distance).FirstOrDefault();

                if (Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer) >= 2)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))");
                    if (closestDestroyer != null)
                    {
                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            //Note: kiki optimal range is 25k!
                            closestDestroyer.Orbit(12000);
                            return true;
                        }

                        if (40 > Time.Instance.SecondsSinceLastSessionChange)
                        {
                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive && closestDestroyer._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (closestDestroyer._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closestDestroyer.KeepAtRange(55000);
                                return true;
                            }

                            if (closestDestroyer._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (closestDestroyer._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closestDestroyer.KeepAtRange(55000);
                                return true;
                            }

                            if (DebugConfig.DebugNavigateOnGrid) Log("if (45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closestDestroyer.Orbit(45000);
                            return true;
                        }

                        if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                        {
                            if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                                closestDestroyer._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 14000);
                                return true;
                            }

                            var orbitDistanceToUseMwd = (int)Combat.MaxRange - 14000;

                            if (ESCache.Instance.ActiveShip.IsShieldTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage > ESCache.Instance.ActiveShip.ShieldPercentage && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsShieldTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage [" + _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage + "] > ESCache.Instance.ActiveShip.ShieldPercentage [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "]) Run Away till shield recovers!");
                                orbitDistanceToUseMwd = 45000;//our targeting range is only 32k, our missile range is?
                            }

                            if (ESCache.Instance.ActiveShip.IsArmorTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage > ESCache.Instance.ActiveShip.ArmorPercentage && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsArmorTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage [" + _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage + "] > ESCache.Instance.ActiveShip.ArmorPercentage [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "]) Run Away till armor recovers!");
                                orbitDistanceToUseMwd = 45000;//our targeting range is only 32k, our missile range is?
                            }

                            if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                            closestDestroyer.Orbit(orbitDistanceToUseMwd);
                            return true;
                        }

                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                            closestDestroyer._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 10000);
                            return true;
                        }

                        var orbitDistanceToUse = (int)Combat.MaxRange - 10000;

                        if (ESCache.Instance.ActiveShip.IsShieldTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage > ESCache.Instance.ActiveShip.ShieldPercentage && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsShieldTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage [" + _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage + "] > ESCache.Instance.ActiveShip.ShieldPercentage [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "]) Run Away till shield recovers!");
                            orbitDistanceToUse = 45000;//our targeting range is only 32k, our missile range is?
                        }

                        if (ESCache.Instance.ActiveShip.IsArmorTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage > ESCache.Instance.ActiveShip.ArmorPercentage && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsArmorTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage [" + _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage + "] > ESCache.Instance.ActiveShip.ArmorPercentage [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "]) Run Away till armor recovers!");
                            orbitDistanceToUse = 45000;//our targeting range is only 32k, our missile range is?
                        }

                        closestDestroyer.Orbit(orbitDistanceToUse); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                        return true;
                    }
                }

                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    if (closestDestroyer != null)
                    {
                        closestDestroyer.Orbit(500);
                        return true;
                    }
                }

                if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                {
                    //Note: kiki optimal range is 25k!
                    var orbitDistanceToUse = 27000;
                    Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer).OrderBy(i => i.Distance).FirstOrDefault().Orbit(orbitDistanceToUse);
                    return true;
                }

                if (ESCache.Instance.AbyssalMobileTractor != null)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                    ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                    return true;
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("safe to orbit gate?!");
                _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
            }


            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate))
            {
                var closest = Combat.PotentialCombatTargets.Where(i => i.IsNPCFrigate).OrderByDescending(x => x._directEntity.IsWarpDisruptingEntity).ThenByDescending(y => y._directEntity.IsWebbingEntity).ThenByDescending(i => i._directEntity.IsWarpDisruptingMe).ThenBy(i => i.Distance).FirstOrDefault();
                if (closest != null)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) > 3 || Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && !i.IsReadyToShoot) || ESCache.Instance.ActiveShip.IsFrigate)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))");

                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            closest.Orbit(500);
                            return true;
                        }

                        if (25 > Time.Instance.SecondsSinceLastSessionChange)
                        {
                            if (closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closest.KeepAtRange(55000);
                                return true;
                            }

                            if (closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closest.KeepAtRange(55000);
                                return true;
                            }

                            if (45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closest.KeepAtRange(50000);
                                return true;
                            }
                        }

                        if (ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds != null && ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.Any() && Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                        {
                            var BioluminesenceCloud = ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.OrderBy(i => i.Distance).FirstOrDefault();
                            if (DebugConfig.DebugNavigateOnGrid) Log("BioluminesenceCloud [" + BioluminesenceCloud.Nearest1KDistance + "] DistanceToGate [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.AbyssalGate) / 1000, 0) + "] DistanceToAbyssalCenter [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.AbyssalCenter) / 1000, 0) + "] DistanceToPlayerSpawnLocation [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.PlayerSpawnLocation) / 1000, 0) + "]");
                            if (50000 > BioluminesenceCloud.Distance && ESCache.Instance.SafeDistanceFromAbyssalCenter > BioluminesenceCloud._directEntity.DistanceTo(ESCache.Instance.AbyssalCenter._directEntity))
                            {
                                if (Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                                {
                                    if (DirectEve.Interval(7000, 7000, BioluminesenceCloud.Id.ToString())) BioluminesenceCloud.Orbit(5000);
                                    return true;
                                }
                            }
                        }

                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            closest.Orbit(500);
                            return true;
                        }

                        if (Combat.PotentialCombatTargets.Any(i => !i.IsReadyToShoot))
                        {
                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                                closest.Orbit((int)Combat.MaxRange - 21000);
                                return true;
                            }

                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                closest.Orbit(500);
                                return true;
                            }

                            closest.Orbit((int)Combat.MaxRange - 21000);
                            return true;
                        }

                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 15000);
                            return true;
                        }

                        if (ESCache.Instance.AbyssalMobileTractor != null)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                            ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                            return true;
                        }

                        _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit((int)Combat.MaxRange - 21000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                        return true;
                    }
                }
            }

            if (ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds != null && ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.Any() && Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
            {
                var BioluminesenceCloud = ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.OrderBy(i => i.Distance).FirstOrDefault();
                if (50000 > BioluminesenceCloud.Distance && ESCache.Instance.SafeDistanceFromAbyssalCenter > BioluminesenceCloud._directEntity.DistanceTo(ESCache.Instance.AbyssalCenter._directEntity))
                {
                    if (DirectEve.Interval(7000, 7000, BioluminesenceCloud.Id.ToString())) BioluminesenceCloud.Orbit(5000);
                    return true;
                }
            }

            if (Combat.PotentialCombatTargets.Any())
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.FirstOrDefault().Orbit(500);
                    return true;
                }
            }

            if (ESCache.Instance.AbyssalMobileTractor != null)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                return true;
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("safe to orbit gate?");
            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
            return true;
        }

        internal bool SpeedTank_RodivaSpawn()
        {
            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
            {
                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser && i.Name.Contains("Rodiva")))
                {
                    //if (IsNPCCruiserOrDestroyerHuggingSuppressor)
                    //{
                    //    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCCruiser && i.Name.Contains("Rodiva")).Orbit(500);
                    //    return true;
                    //}

                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        Combat.PotentialCombatTargets.Where(i => i.IsNPCCruiser && i.Name.Contains("Rodiva")).OrderBy(i => i.Distance).FirstOrDefault().Orbit(500);
                        return true;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))");
                    Combat.PotentialCombatTargets.Where(i => i.IsNPCCruiser && i.Name.Contains("Rodiva")).OrderBy(i => i.Distance).FirstOrDefault().Orbit((int)Combat.MaxRange - 15000, false, "IsNPCFrigate too close: they are inside 52k");
                    return true;
                }

                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.Where(i => i.IsNPCCruiser).OrderBy(i => i.Distance).FirstOrDefault().Orbit(500);
                    return true;
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))");
                Combat.PotentialCombatTargets.Where(i => i.IsNPCCruiser).OrderBy(i => i.Distance).FirstOrDefault().Orbit((int)Combat.MaxRange - 12000, false, "IsNPCFrigate too close: they are inside 52k");
                return true;
            }

            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
            {
                //if (IsNPCDestroyerHuggingSuppressor)
                //{
                //    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCDestroyer).Orbit(500);
                //    return true;
                //}
                var closestDestroyer = Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer).OrderByDescending(x => x._directEntity.IsWarpDisruptingEntity).ThenByDescending(y => y._directEntity.IsWebbingEntity).ThenByDescending(i => i._directEntity.IsWarpDisruptingMe).ThenBy(i => i.Distance).FirstOrDefault();
                if (Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer) >= 3)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))");
                    if (closestDestroyer != null)
                    {
                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            closestDestroyer.Orbit(500);
                            return true;
                        }

                        if (40 > Time.Instance.SecondsSinceLastSessionChange)
                        {
                            if (closestDestroyer._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (closestDestroyer._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closestDestroyer.KeepAtRange(55000);
                                return true;
                            }

                            if (closestDestroyer._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (closestDestroyer._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closestDestroyer.KeepAtRange(55000);
                                return true;
                            }

                            if (DebugConfig.DebugNavigateOnGrid) Log("if (45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closestDestroyer.Orbit(45000);
                            return true;
                        }

                        if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                        {
                            if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                                closestDestroyer._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 14000);
                                return true;
                            }

                            if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                            closestDestroyer.Orbit((int)Combat.MaxRange - 14000);
                            return true;
                        }

                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            closestDestroyer.Orbit(500);
                            return true;
                        }

                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                            closestDestroyer._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 10000);
                            return true;
                        }

                        closestDestroyer.Orbit((int)Combat.MaxRange - 10000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                        return true;
                    }
                }

                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    closestDestroyer.Orbit(500);
                    return true;
                }

                if (ESCache.Instance.AbyssalMobileTractor != null)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                    ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                    return true;
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("safe to orbit gate?!");
                _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
            }

            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate))
            {
                var closest = Combat.PotentialCombatTargets.Where(i => i.IsNPCFrigate).OrderByDescending(x => x._directEntity.IsWarpDisruptingEntity).ThenByDescending(y => y._directEntity.IsWebbingEntity).ThenByDescending(i => i._directEntity.IsWarpDisruptingMe).ThenBy(i => i.Distance).FirstOrDefault();
                if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) > 3 || Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && !i.IsReadyToShoot))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))");
                    if (closest != null)
                    {
                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            closest.Orbit(500);
                            return true;
                        }

                        if (25 > Time.Instance.SecondsSinceLastSessionChange)
                        {
                            if (closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closest.KeepAtRange(55000);
                                return true;
                            }

                            if (closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closest.KeepAtRange(55000);
                                return true;
                            }

                            if (45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closest.KeepAtRange(50000);
                                return true;
                            }
                        }

                        if (ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds != null && ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.Any() && Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                        {
                            var BioluminesenceCloud = ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.OrderBy(i => i.Distance).FirstOrDefault();
                            if (DebugConfig.DebugNavigateOnGrid) Log("BioluminesenceCloud [" + BioluminesenceCloud.Nearest1KDistance + "] DistanceToGate [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.AbyssalGate) / 1000, 0) + "] DistanceToAbyssalCenter [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.AbyssalCenter) / 1000, 0) + "] DistanceToPlayerSpawnLocation [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.PlayerSpawnLocation) / 1000, 0) + "]");
                            if (50000 > BioluminesenceCloud.Distance && ESCache.Instance.SafeDistanceFromAbyssalCenter > BioluminesenceCloud._directEntity.DistanceTo(ESCache.Instance.AbyssalCenter._directEntity))
                            {
                                if (Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                                {
                                    if (DirectEve.Interval(7000, 7000, BioluminesenceCloud.Id.ToString())) BioluminesenceCloud.Orbit(5000);
                                    return true;
                                }
                            }
                        }

                        if (Combat.PotentialCombatTargets.Any(i => !i.IsReadyToShoot))
                        {
                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                                closest.Orbit((int)Combat.MaxRange - 21000);
                                return true;
                            }

                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                closest.Orbit(500);
                                return true;
                            }

                            closest.Orbit((int)Combat.MaxRange - 21000);
                            return true;
                        }

                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            closest.Orbit(500);
                            return true;
                        }

                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                            ESCache.Instance.AbyssalGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 15000);
                            return true;
                        }

                        if (ESCache.Instance.AbyssalMobileTractor != null)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                            ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                            return true;
                        }

                        _moveToTargetEntity.Orbit((int)Combat.MaxRange - 21000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                        return true;
                    }
                }

            }


            if (Combat.PotentialCombatTargets.Any())
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.FirstOrDefault().Orbit(500);
                    return true;
                }
            }

            if (ESCache.Instance.AbyssalMobileTractor != null)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                return true;
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("return true");
            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
            return true;
        }

        internal bool SpeedTank_LeshakBSSpawn()
        {
            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))");
                if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                    Combat.PotentialCombatTargets.Where(i => i.IsNPCBattleship).OrderBy(x => x.Distance).FirstOrDefault().Orbit(15000);
                    return true;
                }

                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.Where(i => i.IsNPCBattleship).OrderBy(x => x.Distance).FirstOrDefault().Orbit(500);
                    return true;
                }

                if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                    Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCBattleship)._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 5000);
                    return true;
                }

                Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCBattleship)._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000);
                return true;
            }

            if (Combat.PotentialCombatTargets.Any(i => !i.IsReadyToShoot))
            {
                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))");
                    if (29000 > Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Distance)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (29000 > Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Distance)");
                        Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Orbit((int)Combat.MaxRange - 20000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");return true;
                        return true;
                    }
                }
            }

            if (Combat.PotentialCombatTargets.Any())
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.Where(i => i.IsNPCBattleship).OrderBy(x => x.Distance).FirstOrDefault().Orbit(500);
                    return true;
                }
            }

            if (ESCache.Instance.AbyssalMobileTractor != null)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                return true;
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("Only frigates left and they are all in range: orbiting gate");
            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
            return true;
        }

        internal bool SpeedTank_LucidDeepwatcherBSSpawn()
        {
            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.Where(i => i.IsNPCFrigate || i.IsNPCDestroyer).OrderBy(x => x.Distance).FirstOrDefault().Orbit(500);
                    return true;
                }

                if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate || i.IsNPCDestroyer) >= 4)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))
                    {
                        if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))");
                            Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Orbit(15000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k"); return true;
                            return true;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))");
                        Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Orbit((int)Combat.MaxRange - 10000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k"); return true;
                        return true;
                    }
                }
            }

            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
            {
                if (ESCache.Instance.ActiveShip.IsFrigate && !ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses || (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses && Drones.AllDronesInSpace.Any(i => !i.IsAttacking)))
                {
                    if (35000 > ESCache.Instance.AbyssalCenter.Distance || 40000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Distance) //if we arent this close to the abyssalcenter wait until we are before getting closer to karen
                    {
                        if (ESCache.Instance.Entities.Any(i => i.IsAbyssalDeadspaceTriglavianExtractionNode && 30000 > i.DistanceTo(Combat.PotentialCombatTargets.Where(x => x.IsNPCBattleship && x.Distance > 30000).FirstOrDefault())))
                        {
                            var extractionNodeToGetCloserTo = ESCache.Instance.Entities.FirstOrDefault(i => i.IsAbyssalDeadspaceTriglavianExtractionNode && 30000 > i.DistanceTo(Combat.PotentialCombatTargets.Where(x => x.IsNPCBattleship && x.Distance > 30000).FirstOrDefault()));
                            if (extractionNodeToGetCloserTo != null && extractionNodeToGetCloserTo.Distance > 10000)
                            {
                                extractionNodeToGetCloserTo.Orbit(1000);
                                return true;
                            }
                        }

                        if (30000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Distance)
                        {
                            if (20000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Distance)
                            {
                                if (10000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Distance)
                                {
                                    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(500);
                                    return true;
                                }

                                Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(5000);
                                return true;
                            }

                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(14000);
                                return true;
                            }

                            Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(15000);
                            return true;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("!if (30000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Distance): OrbitWithHighTransversal(25000)");
                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                        {
                            Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(15000);
                            return true;
                        }

                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(20000);
                            return true;
                        }



                        Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCBattleship).Orbit(25000);
                        return true;
                    }
                }

                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.IsCurrentTarget && i.IsReadyToShoot && i.IsAttacking && Combat.MaxRange > i.DistanceTo(ESCache.Instance.AbyssalGate)))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.IsReadyToShoot))");
                    if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                    {
                        if (ESCache.Instance.AbyssalMobileTractor != null)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                            ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                            return true;
                        }

                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            Combat.PotentialCombatTargets.Where(i => i.IsNPCBattleship).OrderBy(x => x.Distance).FirstOrDefault().Orbit(500);
                            return true;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                        _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
                        return true;
                    }

                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        Combat.PotentialCombatTargets.Where(i => i.IsNPCBattleship).OrderBy(x => x.Distance).FirstOrDefault().Orbit(500);
                        return true;
                    }

                    if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                    {
                        var orbitDistanceToUse = Math.Min(22000, (int)Drones.MaxDroneRange);
                        Combat.PotentialCombatTargets.Where(i => i.IsNPCBattleship).OrderBy(x => x.Distance).FirstOrDefault().Orbit(orbitDistanceToUse);
                        return true;
                    }

                    if (ESCache.Instance.AbyssalMobileTractor != null)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                        ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                        return true;
                    }

                    _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
                    return true;
                }

                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.Where(i => i.IsNPCBattleship).OrderBy(x => x.Distance).FirstOrDefault().Orbit(500);
                    return true;
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))");
                if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                    Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCBattleship).Orbit(2000);
                    return true;
                }

                Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCBattleship).Orbit(2000);
                return true;
            }




            if (Combat.PotentialCombatTargets.Any())
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault().Orbit(500);
                    return true;
                }
            }

            if (ESCache.Instance.AbyssalMobileTractor != null)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                ESCache.Instance.AbyssalMobileTractor.Orbit(500);
                return true;
            }

            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
            return true;
        }

        internal bool SpeedTank_LucidCruiserSpawn()
        {
            if (Combat.PotentialCombatTargets.Any())
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))");
                var closest = Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderByDescending(x => x._directEntity.IsWarpDisruptingEntity).ThenByDescending(y => y._directEntity.IsWebbingEntity).ThenByDescending(i => i._directEntity.IsWarpDisruptingMe).ThenBy(i => i.Distance).FirstOrDefault();
                if (closest != null)
                {
                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => 14000 > i.Distance))
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsAbyssalBioAdaptiveCache))
                            {
                                Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache).Orbit(500);
                                return true;
                            }

                            if (ESCache.Instance.Wrecks.Any() && ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                            {
                                ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty).Orbit(500);
                                return true;
                            }

                            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500);
                            return true;
                        }

                        closest.Orbit(500);
                        return true;
                    }

                    if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))");
                        Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Orbit(15000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k"); return true;
                        return true;
                    }

                    if (25 > Time.Instance.SecondsSinceLastSessionChange)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (25 > _secondsSinceLastSessionChange)");

                        if (closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.KeepAtRange(55000);
                            return true;
                        }

                        if (closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.KeepAtRange(55000);
                            return true;
                        }

                        if (45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.KeepAtRange(50000);
                            return true;
                        }
                    }

                    if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)");
                        _nextGate.Orbit(5000);
                        return true;
                    }


                    if (25000 > closest.Distance)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (25000 > closest.Distance)");
                        if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                            closest.Orbit((int)Combat.MaxRange - 5000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                            return true;
                        }

                        var orbitDistanceToUse = (int)Combat.MaxRange - 5000;

                        if (ESCache.Instance.ActiveShip.IsShieldTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage > ESCache.Instance.ActiveShip.ShieldPercentage && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsShieldTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage [" + _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage + "] > ESCache.Instance.ActiveShip.ShieldPercentage [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "]) Run Away till shield recovers!");
                            orbitDistanceToUse = 45000;//our targeting range is only 32k, our missile range is?
                        }

                        if (ESCache.Instance.ActiveShip.IsArmorTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage > ESCache.Instance.ActiveShip.ArmorPercentage && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsArmorTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage [" + _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage + "] > ESCache.Instance.ActiveShip.ArmorPercentage [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "]) Run Away till armor recovers!");
                            orbitDistanceToUse = 45000;//our targeting range is only 32k, our missile range is?
                        }

                        closest._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(orbitDistanceToUse); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                        return true;
                    }
                }

                if (Combat.PotentialCombatTargets.Any(x => x.IsNPCCruiser))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))");
                    EntityCache NpcCruiser = null;
                    if (Combat.PotentialCombatTargets.Count(x => x.IsNPCCruiser) >= 2)
                    {
                        if (2 > Combat.PotentialCombatTargets.Count(x => x.IsNPCCruiser && x.IsReadyToShoot))
                        {
                            NpcCruiser = Combat.PotentialCombatTargets.Where(x => x.IsNPCCruiser).OrderBy(y => y.Distance).Skip(1).FirstOrDefault();
                        }
                    }

                    if (NpcCruiser == null)
                        NpcCruiser = Combat.PotentialCombatTargets.Where(x => x.IsNPCCruiser).OrderBy(y => y.Distance).FirstOrDefault();

                    if (NpcCruiser != null)
                    {
                        if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                            NpcCruiser.Orbit((int)Combat.MaxRange - 7000);
                            return true;
                        }

                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => 14000 > i.Distance))
                            {
                                if (Combat.PotentialCombatTargets.Any(i => i.IsAbyssalBioAdaptiveCache))
                                {
                                    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache).Orbit(500);
                                    return true;
                                }

                                if (ESCache.Instance.Wrecks.Any() && ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                                {
                                    ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty).Orbit(500);
                                    return true;
                                }

                                _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500);
                                return true;
                            }

                            NpcCruiser.Orbit(500);
                            return true;
                        }

                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                            NpcCruiser._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 7000);
                            return true;
                        }

                        var orbitDistanceToUse = (int)Combat.MaxRange - 7000;

                        if (ESCache.Instance.ActiveShip.IsShieldTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage > ESCache.Instance.ActiveShip.ShieldPercentage && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsShieldTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage [" + _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage + "] > ESCache.Instance.ActiveShip.ShieldPercentage [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "]) Run Away till shield recovers!");
                            orbitDistanceToUse = 45000;//our targeting range is only 32k, our missile range is?
                        }

                        if (ESCache.Instance.ActiveShip.IsArmorTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage > ESCache.Instance.ActiveShip.ArmorPercentage && (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.ActiveShip.IsArmorTanked && _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage [" + _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage + "] > ESCache.Instance.ActiveShip.ArmorPercentage [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "]) Run Away till armor recovers!");
                            orbitDistanceToUse = 45000;//our targeting range is only 32k, our missile range is?
                        }

                        //Cruiser._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 20000);
                        NpcCruiser.Orbit(orbitDistanceToUse);
                        return true;
                    }

                    return true;
                }
            }

            if (Combat.PotentialCombatTargets.Any())
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.OrderBy(i => i.Distance).FirstOrDefault().Orbit(500);
                    return true;
                }
            }

            if (ESCache.Instance.AbyssalMobileTractor != null)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                return true;
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("return true");
            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
            return true;
        }

        internal bool SpeedTank_LucidFrigateSpawn()
        {

            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))
            {
                var closest = Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderByDescending(x => x._directEntity.IsWarpDisruptingEntity).ThenByDescending(y => y._directEntity.IsWebbingEntity).ThenByDescending(i => i._directEntity.IsWarpDisruptingMe).ThenBy(i => i.Distance).FirstOrDefault();
                if (closest != null)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))");
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate || i.IsNPCDestroyer) > 6 || !Combat.PotentialCombatTargets.All(i => i.IsReadyToTarget))
                    {
                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => 14000 > i.Distance))
                            {
                                if (Combat.PotentialCombatTargets.Any(i => i.IsAbyssalBioAdaptiveCache))
                                {
                                    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache).Orbit(500);
                                    return true;
                                }

                                if (ESCache.Instance.Wrecks.Any() && ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                                {
                                    ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty).Orbit(500);
                                    return true;
                                }

                                _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500);
                                return true;
                            }

                            closest.Orbit(500);
                            return true;
                        }

                        if (25 > Time.Instance.SecondsSinceLastSessionChange)
                        {
                            if (closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closest.KeepAtRange(55000);
                                return true;
                            }

                            if (closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closest.KeepAtRange(55000);
                                return true;
                            }

                            if (45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closest.KeepAtRange(50000);
                                return true;
                            }
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("if (26000 > Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderBy(i => i.Distance).FirstOrDefault().Distance)");
                        if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                            closest.Orbit((int)Combat.MaxRange - 28000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                            return true;
                        }

                        if (ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds != null && ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.Any() && Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                        {
                            var BioluminesenceCloud = ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.OrderBy(i => i.Distance).FirstOrDefault();
                            if (DebugConfig.DebugNavigateOnGrid) Log("BioluminesenceCloud [" + BioluminesenceCloud.Nearest1KDistance + "] DistanceToGate [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.AbyssalGate) / 1000, 0) + "] DistanceToAbyssalCenter [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.AbyssalCenter) / 1000, 0) + "] DistanceToPlayerSpawnLocation [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.PlayerSpawnLocation) / 1000, 0) + "]");
                            if (50000 > BioluminesenceCloud.Distance && ESCache.Instance.SafeDistanceFromAbyssalCenter > BioluminesenceCloud._directEntity.DistanceTo(ESCache.Instance.AbyssalCenter._directEntity))
                            {
                                if (DirectEve.Interval(7000, 7000, BioluminesenceCloud.Id.ToString())) BioluminesenceCloud.Orbit(5000);
                                return true;
                            }
                        }

                        closest._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 25000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                        return true;

                    }

                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => 14000 > i.Distance))
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsAbyssalBioAdaptiveCache))
                            {
                                Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache).Orbit(500);
                                return true;
                            }

                            if (ESCache.Instance.Wrecks.Any() && ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                            {
                                ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty).Orbit(500);
                                return true;
                            }

                            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(500);
                            return true;
                        }

                        closest.Orbit(500);
                        return true;
                    }

                    if (ESCache.Instance.AbyssalMobileTractor != null)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                        ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.All(i => i.BracketType != BracketType.Large_Collidable_Structure && !i.IsAttacking) && Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                    {
                        Combat.PotentialCombatTargets.FirstOrDefault(i => i.BracketType != BracketType.Large_Collidable_Structure).Orbit(5000);
                        return true;
                    }

                    _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
                    return true;
                }
            }

            if (Combat.PotentialCombatTargets.Any())
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.OrderBy(i => i.Distance).FirstOrDefault().Orbit(500);
                    return true;
                }
            }

            if (ESCache.Instance.AbyssalMobileTractor != null)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                return true;
            }

            if (Combat.PotentialCombatTargets.All(i => i.BracketType != BracketType.Large_Collidable_Structure && !i.IsAttacking) && Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
            {
                Combat.PotentialCombatTargets.FirstOrDefault(i => i.BracketType != BracketType.Large_Collidable_Structure).Orbit(5000);
                return true;
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("return true");
            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
            return true;
        }

        internal bool SpeedTank_LuciferSpawn()
        {
            if (!WeWantToBeMoving && ESCache.Instance.ActiveShip.Entity.Velocity == 0)
                return false;

            //Cynabals
            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))");

                if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains("Lucifer Cynabal".ToLower())))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains(\"Lucifer Cynabal\".ToLower())))");
                    var closest = Combat.PotentialCombatTargets.Where(i => i.Name.ToLower().Contains("Elite Lucifer Cynabal".ToLower())).OrderBy(i => i.Distance).FirstOrDefault();
                    if (closest == null)
                    {
                        closest = Combat.PotentialCombatTargets.Where(i => i.Name.ToLower().Contains("Lucifer Cynabal".ToLower()) && !i.Name.ToLower().Contains("Elite".ToLower())).OrderBy(i => i.Distance).FirstOrDefault();
                    }

                    /***
                    if (30 > _secondsSinceLastSessionChange && 50000 > closest.Distance)
                    {
                        if (closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.KeepAtRange(55000);
                            return true;
                        }

                        if (closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.KeepAtRange(55000);
                            return true;
                        }

                        if (45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (35000 > closest.Distance && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.KeepAtRange(50000);
                            return true;
                        }
                    }
                    **/

                    /**
                    if (ESCache.Instance.ActiveShip.IsScrambled || ESCache.Instance.ActiveShip.IsWebbed)
                    {
                        if (closest.Velocity > ESCache.Instance.ActiveShip.Entity.Velocity)
                        {
                            closest.KeepAtRange(18000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                            return true;
                        }
                    }
                    **/

                    if (closest.IsInOptimalRange)
                    {
                        if (DirectEve.Interval(5000))
                        {
                            Log("Notification!: if (closest.IsInOptimalRange))");
                            if (PlayNotificationSounds) Util.PlayNoticeSound();
                        }
                    }

                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        closest.Orbit(12000);
                        return true;
                    }

                    if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");


                        if (MoveToUsingBestMethod(ESCache.Instance.AbyssalCenter, 70000, 80000)) return true;
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (MoveToUsingBestMethod(ESCache.Instance.AbyssalCenter, 70000, 80000)) returned false");

                        ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(57000);
                        return true;
                    }

                    if (MoveToUsingBestMethod(ESCache.Instance.AbyssalCenter, 70000, 80000)) return true;
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (MoveToUsingBestMethod(ESCache.Instance.AbyssalCenter, 70000, 80000)) returned false");

                    ESCache.Instance.AbyssalCenter.Orbit(57000);
                    return true;
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("no cynabals");
            }

            //Frigs and Destroyers
            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCDestroyer))");
                var closest = Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer || i.IsNPCFrigate).OrderByDescending(x => x._directEntity.IsWarpDisruptingEntity).ThenByDescending(y => y._directEntity.IsWebbingEntity).ThenByDescending(i => i._directEntity.IsWarpDisruptingMe).ThenBy(i => i.Distance).FirstOrDefault();
                if (closest != null)
                {
                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        closest.Orbit(12000);
                        return true;
                    }

                    if (25 > Time.Instance.SecondsSinceLastSessionChange)
                    {
                        if (closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.KeepAtRange(55000);
                            return true;
                        }

                        if (closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.KeepAtRange(55000);
                            return true;
                        }

                        if (45000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closest.KeepAtRange(50000);
                            return true;
                        }
                    }

                    if (ESCache.Instance.ActiveShip.IsScrambled || ESCache.Instance.ActiveShip.IsWebbed)
                    {
                        if (closest.Velocity > ESCache.Instance.ActiveShip.Entity.Velocity)
                        {
                            closest.KeepAtRange(18000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                            return true;
                        }
                    }

                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        closest.Orbit(500);
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Lucifer Dramiel")))
                    {
                        closest.Orbit((int)Combat.MaxRange - 5000); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                        return true;
                    }

                    if (ESCache.Instance.AbyssalMobileTractor != null)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                        ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                        return true;
                    }

                    _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(10000);
                    return true;
                }
            }

            //Any other cruisers
            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCCruiser).Orbit(500);
                    return true;
                }

                if (ESCache.Instance.AbyssalMobileTractor != null)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                    ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                    return true;
                }

                _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
                return true;
            }

            //any other ships
            if (Combat.PotentialCombatTargets.Any())
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault().Orbit(500);
                    return true;
                }
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("return false");
            return false;
        }

        internal bool SpeedTank_VedmakCruiserSpawn()
        {
            var orbitDistanceToUse = (int)Combat.MaxRange - 5000;

            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))");

                if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains("Vedmak".ToLower())))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains(\"Vedmak\")))");
                    var vedmak = Combat.PotentialCombatTargets.Where(i => i.Name.ToLower().Contains("Vedmak".ToLower())).OrderBy(x => x.Distance).FirstOrDefault();
                    if (vedmak != null)
                    {
                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            vedmak.Orbit(500);
                            return true;
                        }

                        if (45 > Time.Instance.SecondsSinceLastSessionChange)
                        {
                            if (MoveToUsingBestMethod(ESCache.Instance.AbyssalCenter, 60000, 70000)) return true;
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (MoveToUsingBestMethod(ESCache.Instance.AbyssalCenter, 70000, 80000)) returned false");
                        }

                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            vedmak.Orbit(500);
                            return true;
                        }

                        if (27000 > vedmak.Distance && 40000 > ESCache.Instance.AbyssalCenter.Distance)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (27000 > Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.Name.ToLower().Contains(\"Vedmak\")).Distance)");
                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                                vedmak.KeepAtRange(40000);
                                return true;
                            }

                            vedmak.KeepAtRange(40000);
                            return true;
                        }

                        if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                            vedmak.Orbit((int)Combat.MaxRange - 10000);
                            return true;
                        }

                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                            vedmak._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 22000);
                            return true;
                        }

                        orbitDistanceToUse = (int)Combat.MaxRange - 5000;
                        if (ESCache.Instance.ActiveShip.IsFrigate && ESCache.Instance.ActiveShip.IsShieldTanked && 60 > ESCache.Instance.ActiveShip.ShieldPercentage)
                        {
                            var closest2 = Combat.PotentialCombatTargets.OrderBy(i => i.Distance).FirstOrDefault();
                            if (closest2 != null)
                            {
                                if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                                    orbitDistanceToUse = Math.Min(23000, (int)Drones.MaxDroneRange);//at least 22k+ away from vedmaks or they will ramp up damage!

                                closest2.Orbit(orbitDistanceToUse);
                                return true;
                            }
                        }

                        orbitDistanceToUse = (int)Combat.MaxRange - 10000;
                        if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                            orbitDistanceToUse = Math.Min(22000, (int)Drones.MaxDroneRange);

                        vedmak.Orbit(orbitDistanceToUse);
                        return true;
                    }
                }

                EntityCache Cruiser = null;
                if (Combat.PotentialCombatTargets.Count(x => x.IsNPCCruiser) >= 2)
                {
                    if (2 > Combat.PotentialCombatTargets.Count(x => x.IsNPCCruiser && x.IsReadyToShoot))
                    {
                        Cruiser = Combat.PotentialCombatTargets.Where(x => x.IsNPCCruiser).OrderBy(y => y.Distance).Skip(1).FirstOrDefault();
                        if (Cruiser != null)
                        {
                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                Cruiser.Orbit(500);
                                return true;
                            }

                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                                Cruiser.Orbit((int)Combat.MaxRange - 14000);
                                return true;
                            }

                            Cruiser._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 14000);
                            return true;
                        }
                    }
                }

                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCCruiser).Orbit(500);
                    return true;
                }

                if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                    Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCCruiser).Orbit((int)Combat.MaxRange - 14000);
                    return true;
                }

                orbitDistanceToUse = (int)Combat.MaxRange - 5000;
                if (ESCache.Instance.ActiveShip.IsFrigate && ESCache.Instance.ActiveShip.IsShieldTanked && 60 > ESCache.Instance.ActiveShip.ShieldPercentage)
                {
                    var closest2 = Combat.PotentialCombatTargets.OrderBy(i => i.Distance).FirstOrDefault();
                    if (closest2 != null)
                    {
                        if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                            orbitDistanceToUse = Math.Min(22000, (int)Drones.MaxDroneRange);

                        closest2.Orbit(orbitDistanceToUse);
                        return true;
                    }
                }

                orbitDistanceToUse = (int)Combat.MaxRange - 14000;
                if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                    orbitDistanceToUse = Math.Min(22000, (int)Drones.MaxDroneRange);

                Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCCruiser)._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(orbitDistanceToUse);
                return true;
            }


            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
            {
                //if (IsNPCDestroyerHuggingSuppressor)
                //{
                //    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCDestroyer).Orbit(500);
                //    return true;
                //}
                var closestDestroyer = Combat.PotentialCombatTargets.Where(i => i.IsNPCDestroyer).OrderByDescending(x => x._directEntity.IsWarpDisruptingEntity).ThenByDescending(y => y._directEntity.IsWebbingEntity).ThenByDescending(i => i._directEntity.IsWarpDisruptingMe).ThenBy(i => i.Distance).FirstOrDefault();
                if (closestDestroyer != null)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer) >= 3)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))");

                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            closestDestroyer.Orbit(500);
                            return true;
                        }

                        if (40 > Time.Instance.SecondsSinceLastSessionChange)
                        {
                            if (closestDestroyer._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (closestDestroyer._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closestDestroyer.KeepAtRange(55000);
                                return true;
                            }

                            if (closestDestroyer._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (closestDestroyer._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closestDestroyer.KeepAtRange(55000);
                                return true;
                            }

                            if (DebugConfig.DebugNavigateOnGrid) Log("if (45000 > ESCache.Instance.AbyssalCenter.Distance)");
                            closestDestroyer.Orbit(45000);
                            return true;
                        }

                        if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                        {
                            if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                                closestDestroyer._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 14000);
                                return true;
                            }

                            if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                            closestDestroyer.Orbit((int)Combat.MaxRange - 14000);
                            return true;
                        }

                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                            closestDestroyer._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 10000);
                            return true;
                        }

                        orbitDistanceToUse = (int)Combat.MaxRange - 5000;
                        if (ESCache.Instance.ActiveShip.IsFrigate && ESCache.Instance.ActiveShip.IsShieldTanked && 60 > ESCache.Instance.ActiveShip.ShieldPercentage)
                        {
                            var closest2 = Combat.PotentialCombatTargets.OrderBy(i => i.Distance).FirstOrDefault();
                            if (closest2 != null)
                            {
                                if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                                    orbitDistanceToUse = Math.Min(22000, (int)Drones.MaxDroneRange);

                                closest2.Orbit(orbitDistanceToUse);
                                return true;
                            }
                        }

                        orbitDistanceToUse = (int)Combat.MaxRange - 10000;
                        if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                            orbitDistanceToUse = Math.Min(22000, (int)Drones.MaxDroneRange);

                        closestDestroyer.Orbit(orbitDistanceToUse); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                        return true;
                    }
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("safe to orbit gate?!");
                _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
            }


            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate))
            {
                if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) > 3 || Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && !i.IsReadyToShoot))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) > 3 || Combat.PotentialCombatTargets.Any(i => !i.IsReadyToShoot))");
                    var closest = Combat.PotentialCombatTargets.Where(i => i.IsNPCFrigate).OrderByDescending(x => x._directEntity.IsWarpDisruptingEntity).ThenByDescending(y => y._directEntity.IsWebbingEntity).ThenByDescending(i => i._directEntity.IsWarpDisruptingMe).ThenBy(i => i.Distance).FirstOrDefault();
                    if (closest != null)
                    {
                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            closest.Orbit(500);
                            return true;
                        }

                        if (25 > Time.Instance.SecondsSinceLastSessionChange)
                        {
                            if (closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWarpDisruptingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closest.KeepAtRange(55000);
                                return true;
                            }

                            if (closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (42000 > closest.Distance && closest._directEntity.IsWebbingEntity && 45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closest.KeepAtRange(55000);
                                return true;
                            }

                            if (45000 > ESCache.Instance.AbyssalCenter.Distance)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("if (45000 > ESCache.Instance.AbyssalCenter.Distance)");
                                closest.KeepAtRange(50000);
                                return true;
                            }
                        }

                        if (ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds != null && ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.Any() && Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                        {
                            var BioluminesenceCloud = ESCache.Instance.ListAbyssalDeadspaceBioluminesenceClouds.OrderBy(i => i.Distance).FirstOrDefault();
                            if (DebugConfig.DebugNavigateOnGrid) Log("BioluminesenceCloud [" + BioluminesenceCloud.Nearest1KDistance + "] DistanceToGate [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.AbyssalGate) / 1000, 0) + "] DistanceToAbyssalCenter [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.AbyssalCenter) / 1000, 0) + "] DistanceToPlayerSpawnLocation [" + Math.Round(BioluminesenceCloud.DistanceTo(ESCache.Instance.PlayerSpawnLocation) / 1000, 0) + "]");
                            if (50000 > BioluminesenceCloud.Distance && ESCache.Instance.SafeDistanceFromAbyssalCenter > BioluminesenceCloud._directEntity.DistanceTo(ESCache.Instance.AbyssalCenter._directEntity))
                            {
                                if (Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                                {
                                    if (DirectEve.Interval(7000, 7000, BioluminesenceCloud.Id.ToString())) BioluminesenceCloud.Orbit(5000);
                                    return true;
                                }
                            }
                        }

                        if (Combat.PotentialCombatTargets.Any(i => !i.IsReadyToShoot))
                        {
                            if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                            {
                                closest.Orbit(500);
                                return true;
                            }

                            if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                            {
                                if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                                closest.Orbit((int)Combat.MaxRange - 21000);
                                return true;
                            }

                            closest.Orbit((int)Combat.MaxRange - 21000);
                            return true;
                        }

                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            closest.Orbit(500);
                            return true;
                        }

                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("AsteroidDungeonCloud");
                            ESCache.Instance.AbyssalGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 15000);
                            return true;
                        }

                        if (ESCache.Instance.AbyssalMobileTractor != null)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                            ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                            return true;
                        }

                        orbitDistanceToUse = (int)Combat.MaxRange - 5000;
                        if (ESCache.Instance.ActiveShip.IsFrigate && ESCache.Instance.ActiveShip.IsShieldTanked && 60 > ESCache.Instance.ActiveShip.ShieldPercentage)
                        {
                            var closest2 = Combat.PotentialCombatTargets.OrderBy(i => i.Distance).FirstOrDefault();
                            if (closest2 != null)
                            {
                                if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                                    orbitDistanceToUse = Math.Min(22000, (int)Drones.MaxDroneRange);

                                closest2.Orbit(orbitDistanceToUse);
                                return true;
                            }
                        }

                        orbitDistanceToUse = (int)Combat.MaxRange - 21000;
                        if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                            orbitDistanceToUse = Math.Min(22000, (int)Drones.MaxDroneRange);

                        _moveToTargetEntity.Orbit(orbitDistanceToUse); //, false, "IsNPCFrigate x 4 are too close: they are inside 24k");
                        return true;
                    }
                }
            }

            if (Combat.PotentialCombatTargets.Any())
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault().Orbit(500);
                    return true;
                }
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("return false");
            return false;
        }

        internal bool SpeedTank_VedmakVilaCruiserSwarmerSpawn()
        {
            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
            {
                if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 4 || Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser && !i.IsReadyToShoot))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 4 || Combat.PotentialCombatTargets.Any(i => !i.IsReadyToShoot))");
                    if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains("Vedmak".ToLower())))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i.Name.ToLower().Contains(\"Vedmak\")))");
                        if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                            Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.Name.ToLower().Contains("Vedmak".ToLower())).Orbit((int)Combat.MaxWeaponRange - 15000);
                            return true;
                        }

                        if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                        {
                            Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.Name.ToLower().Contains("Vedmak".ToLower())).Orbit(500);
                            return true;
                        }

                        Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.Name.ToLower().Contains("Vedmak".ToLower()))._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxWeaponRange - 15000);
                        return true;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCCruiser).Orbit(32000, false, \"IsNPCCruiser is too close: they are inside 27k\");");
                    if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("HasMicroWarpDrive");
                        Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCCruiser).Orbit((int)Combat.MaxRange - 25000, false, "IsNPCCruiser is too close: they are inside 27k");
                        return true;
                    }

                    if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                    {
                        Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCCruiser).Orbit(500);
                        return true;
                    }

                    Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCCruiser)._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal((int)Combat.MaxRange - 25000);
                    return true;
                }

                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault(i => i.IsNPCCruiser).Orbit(500);
                    return true;
                }

                if (ESCache.Instance.AbyssalMobileTractor != null)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                    ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                    return true;
                }

                if (DebugConfig.DebugNavigateOnGrid) Log("safe to orbit gate?");
                _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
                return true;
            }

            if (Combat.PotentialCombatTargets.Any())
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault().Orbit(500);
                    return true;
                }
            }

            if (Combat.PotentialCombatTargets.Any(i => 30000 > i.Distance))
            {
                if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                {
                    Combat.PotentialCombatTargets.OrderBy(x => x.Distance).FirstOrDefault().Orbit(10000);
                    return true;
                }
            }

            if (ESCache.Instance.AbyssalMobileTractor != null)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                ESCache.Instance.AbyssalMobileTractor.Orbit(5000);
                return true;
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("safe to orbit gate?");
            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(5000);
            return true;
        }

        internal bool boolSpawnIsDangerousForSpeedTanksDontWaitForTargetsBeforeMovingOnGrid
        {
            get
            {
                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedHunter ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.RodivaSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                    return true;

                return false;
            }
        }

        internal void LogMyFleetMembers()
        {
            if (ESCache.Instance.EveAccount.UseFleetMgr)
            {
                Log("Fleet contains [" + ESCache.Instance.DirectEve.FleetMembers.Count() + "] members");
                foreach (DirectFleetMember individualFleetMember in ESCache.Instance.DirectEve.FleetMembers.Where(i => i.Name != ESCache.Instance.DirectEve.Me.Name))
                {
                    if (individualFleetMember.Entity != null)
                    {
                        if (individualFleetMember.Entity.IsPod)
                            continue;

                        Log("[" + individualFleetMember.Name + "] Distance [" + Math.Round(individualFleetMember.Entity.Distance / 1000, 0) + "k] ShipType [" + individualFleetMember.Entity.TypeName + "] Velocity [" + Math.Round(individualFleetMember.Entity.Velocity, 0) + " m/s] FollowEntity [" + individualFleetMember.Entity.FollowEntityName + "]");
                        continue;
                    }

                    continue;
                }
            }

            return;
        }

        internal void LogMyHealth()
        {
            try
            {
                Log("S[" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) + "%] A[" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0) + "%] H[" + Math.Round(ESCache.Instance.ActiveShip.StructurePercentage, 0) + "%] C[" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%] MyVelocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "m/s] FollowEntityName [" + ESCache.Instance.ActiveShip.Entity.FollowEntityName + "]");
                return;
            }
            catch (Exception ex)
            {
                Log("Exception[" + ex + "]");
            }
        }

        internal void LogMyPositionInSpace()
        {
            try
            {
                Log("MyVelocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "m/s] DistanceToGate [" + ESCache.Instance.AbyssalGate.Nearest1KDistance + "k] DistanceToAbyssalCenter [" + ESCache.Instance.AbyssalCenter.Nearest1KDistance + "k] SafeDistanceFromAbyssalCenter [" + ESCache.Instance.SafeDistanceFromAbyssalCenter + "m] SecondsSinceLastSessionChange [" + Math.Round(Time.Instance.SecondsSinceLastSessionChange, 0) + "] IsOurShipWithintheAbyssBounds [" + IsOurShipWithintheAbyssBounds() + "]");
                return;
            }
            catch (Exception ex)
            {
                Log("Exception[" + ex + "]");
            }
        }

        internal void LogMyWeapons()
        {
            try
            {
                if (ESCache.Instance.Weapons.Any(i => i._module != null && i.IsOnline && i._module.IsMaster))
                {
                    var thisWeapon = ESCache.Instance.Weapons.FirstOrDefault(i => i._module.IsMaster);
                    Log("MyWeapons [" + thisWeapon.TypeName + "] IsActive [" + thisWeapon.IsActive + "] TargetEntityName [" + thisWeapon.TargetEntityName + "] IsInLimboState [" + thisWeapon.IsInLimboState + " ] IsReloadingAmmo [" + thisWeapon.IsReloadingAmmo + " ] IsOverloaded [" + thisWeapon.IsOverloaded + "] Damage% [" + thisWeapon.DamagePercent + "] ChargeName [" + thisWeapon.ChargeName + "][" + thisWeapon.ChargeQty + "] units: of MaxCharges [" + thisWeapon.MaxCharges + "]");
                }
                else
                {
                    int intWeapon = 0;
                    foreach (var thisWeapon in ESCache.Instance.Weapons.Where(i => i.IsOnline))
                    {
                        intWeapon++;
                        Log("Weapon [" + intWeapon + "][" + thisWeapon.TypeName + "] IsActive [" + thisWeapon.IsActive + "] TargetEntityName [" + thisWeapon.TargetEntityName + "] IsInLimboState [" + thisWeapon.IsInLimboState + " ]  IsReloadingAmmo [" + thisWeapon.IsReloadingAmmo + " ] IsOverloaded [" + thisWeapon.IsOverloaded + "] Damage% [" + thisWeapon.DamagePercent + "] ChargeName [" + thisWeapon.ChargeName + "][" + thisWeapon.ChargeQty + "]units: of MaxCharges [" + thisWeapon.MaxCharges + "] MaxRange [" + Math.Round(thisWeapon.MaxRange, 0) + "]");
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        internal void LogMyDrones()
        {
            try
            {
                int intDrone = 0;
                if (allDronesInSpace.Any())
                {
                    foreach (var thisDrone in allDronesInSpace)
                    {
                        try
                        {
                            var thisAbyssalDrone = new AbyssalDrone(thisDrone._directEntity);
                            intDrone++;
                            Log("Drone [" + intDrone + "][" + thisDrone.TypeName + "] DroneState [" + thisDrone._directEntity.DroneState + "] S[" + thisDrone.ShieldPct + "] A[" + thisDrone.ArmorPct + "] H[" + thisDrone.StructurePct + "] Distance [" + thisDrone.Nearest1KDistance + "k] FollowEntityName [" + thisDrone._directEntity.FollowEntityName + "] DistanceToFollowEntity [" + thisDrone._directEntity.FollowEntityDistanceNearest1k + " ][" + Math.Round(thisDrone.Velocity, 0) + "m/s] IsAbyssalDeadspaceTachyonCloud [" + thisDrone.IsAbyssalDeadspaceTachyonCloud + "]");
                        }
                        catch (Exception ex)
                        {
                            Log("Exception [" + ex + "]");
                        }
                    }
                }

                return;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        internal void LogMyDamagedModules()
        {
            try
            {
                int intNum = 0;
                foreach (var thisDamagedModule in ESCache.Instance.Modules.Where(i => i.IsOnline && i.DamagePercent > 0))
                {
                    intNum++;
                    Log("Damaged Module[" + intNum + "][" + thisDamagedModule.TypeName + "] Damage% [" + thisDamagedModule.DamagePercent + "] IsActive [" + thisDamagedModule.IsActive + "] IsInLimboState [" + thisDamagedModule.IsInLimboState + " ] IsOverloaded [" + thisDamagedModule.IsOverloaded + "] IsBeingRepaired [" + thisDamagedModule._module.IsBeingRepaired + "]");
                }

                return;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        internal void LogCurrentTargetHealth()
        {
            try
            {
                if (Combat.CurrentWeaponTarget() != null)
                {
                    Log("[" + Combat.CurrentWeaponTarget().Name + "]@[" + Combat.CurrentWeaponTarget().Nearest1KDistance + "k] S[" + Math.Round(Combat.CurrentWeaponTarget().ShieldPct * 100, 0) + "%] A[" + Math.Round(Combat.CurrentWeaponTarget().ArmorPct * 100, 0) + "%] H[" + Math.Round(Combat.CurrentWeaponTarget().StructurePct * 100, 0) + "%] Velocity [" + Math.Round(Combat.CurrentWeaponTarget().Velocity, 0) + "m/s] FollowEntityName [" + Combat.CurrentWeaponTarget()._directEntity.FollowEntityName + "]");
                    return;
                }

                return;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        /**
        internal void LogNodes()
        {
            string WreckInfo = string.Empty;
            if (ESCache.Instance.Wrecks.Any())
            {
                if (ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty && !i.Name.ToLower().Contains("Extraction".ToLower()) && i.Name.ToLower().Contains("Cache Wreck".ToLower())))
                {

                }
            }
            Log("My Position: DistanceToGate [" + ESCache.Instance.AbyssalGate.Nearest1KDistance + "k] DistanceToAbyssalCenter [" + ESCache.Instance.AbyssalCenter.Nearest1KDistance + "k]" + WreckInfo + ");
            return;
        }

        internal void LogWrecks()
        {
            string WreckInfo = string.Empty;
            if (ESCache.Instance.Wrecks.Any())
            {
                if (ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty && !i.Name.ToLower().Contains("Extraction".ToLower()) && i.Name.ToLower().Contains("Cache Wreck".ToLower())))
                {

                }
            }
            Log("My Position: DistanceToGate [" + ESCache.Instance.AbyssalGate.Nearest1KDistance + "k] DistanceToAbyssalCenter [" + ESCache.Instance.AbyssalCenter.Nearest1KDistance + "k]" + WreckInfo + ");
        }
        **/

        internal bool AbyssalSpeedTank()
        {
            try
            {
                if (!DirectEve.HasFrameChanged())
                    return true;

                if (DebugConfig.DebugNavigateOnGrid) LogMyHealth();
                if (DebugConfig.DebugNavigateOnGrid) LogMyPositionInSpace();
                if (DebugConfig.DebugNavigateOnGrid) LogCurrentTargetHealth();
                if (DebugConfig.DebugNavigateOnGrid) LogMyWeapons();
                if (DebugConfig.DebugNavigateOnGrid) LogMyFleetMembers();

                if (KeepMeSafelyInsideTheArenaAndMoving()) return true;
                if (DebugConfig.DebugNavigateOnGrid) Log("KeepMeSafelyInsideTheArenaAndMoving [false]");

                if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                {
                    //if (HandleNoTargetsOrOutOfWeaponsRange) return true;
                    //if (DebugConfig.DebugNavigateOnGrid) Log("HandleNoTargetsOrOutOfWeaponsRange [false]");

                    if (true)//myTargetIsInRange || boolSpawnIsDangerousForSpeedTanksDontWaitForTargetsBeforeMovingOnGrid)
                    {

                        if (SpeedTank_IsItSafeToMoveToMTUOrLootWrecks) return true; //Looting for Leader
                        if (DebugConfig.DebugNavigateOnGrid) Log("SpeedTank_IsItSafeToMoveToMTUOrLootWrecks returned [false]");
                        AbyssalSpawn.AbyssalSpawnType AbyssalDetectSpawnResult = AbyssalSpawn.DetectSpawn;

                        if (AbyssalDetectSpawnResult != AbyssalSpawn.AbyssalSpawnType.Undecided)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("AbyssalSpawn.DetectSpawn [" + AbyssalDetectSpawnResult + "]! My Ships Speed [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "m/s]");
                            switch (AbyssalDetectSpawnResult)
                            {
                                case AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn:
                                    if (SpeedTank_AbyssalOvermindDroneBSSpawn()) return true;
                                    break;

                                case AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn:
                                    if (SpeedTank_DroneFrigateSpawn()) return true;
                                    break;

                                case AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn:
                                    if (SpeedTank_HighAngleDroneBattleCruiserSpawn()) return true;
                                    break;

                                case AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn:
                                    if (SpeedTank_DrekavacBattleCruiserSpawn()) return true;
                                    break;

                                case AbyssalSpawn.AbyssalSpawnType.ConcordSpawn:
                                    if (SpeedTank_ConcordSpawn()) return true;
                                    break;

                                case AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn:
                                    if (SpeedTank_DamavikFrigateSpawn()) return true;
                                    break;

                                case AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn:
                                    if (SpeedTank_EphialtesCruiserSpawn()) return true;
                                    break;

                                case AbyssalSpawn.AbyssalSpawnType.DevotedHunter:
                                case AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn:
                                    if (SpeedTank_DevotedCruiserSpawn()) return true;
                                    break;

                                case AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn:
                                    if (SpeedTank_KarybdisTyrannosSpawn()) return true;
                                    break;

                                case AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn:
                                    if (SpeedTank_KikimoraDestroyerSpawn()) return true;
                                    break;

                                case AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn:
                                case AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn:
                                    if (SpeedTank_LeshakBSSpawn()) return true;
                                    break;

                                case AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn:
                                    if (SpeedTank_LucidDeepwatcherBSSpawn()) return true;
                                    break;

                                case AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn:
                                    if (SpeedTank_LucidCruiserSpawn()) return true;
                                    break;

                                case AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn:
                                    if (SpeedTank_LucidFrigateSpawn()) return true;
                                    break;

                                case AbyssalSpawn.AbyssalSpawnType.LuciferSpawn:
                                    if (SpeedTank_LuciferSpawn()) return true;
                                    break;

                                case AbyssalSpawn.AbyssalSpawnType.RodivaSpawn:
                                    if (SpeedTank_RodivaSpawn()) return true;
                                    break;

                                case AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn:
                                    if (SpeedTank_VedmakCruiserSpawn()) return true;
                                    break;

                                case AbyssalSpawn.AbyssalSpawnType.VedmakVilaCruiserSwarmerSpawn:
                                case AbyssalSpawn.AbyssalSpawnType.DamavikVilaFrigateSwarmerSpawn:
                                    if (SpeedTank_VedmakVilaCruiserSwarmerSpawn()) return true;
                                    break;
                            }
                        }


                        if (ESCache.Instance.AbyssalSpeedTankSpot != null)
                        {
                            Log("AbyssalSpeedTankSpot exists: finding NextSpotToMoveTo");
                            DirectWorldPosition NextSpotToMoveTo = DirectWorldPosition.GetNextStepInPathToSpeedTankSpotOnImaginarySphere(ESCache.Instance.AbyssalSpeedTankSpot);
                            if (NextSpotToMoveTo != null)
                            {
                                double cachedDistanceToNextSpotToMoveTo = NextSpotToMoveTo.GetDistance(Combat.PotentialCombatTargets.OrderByDescending(i => i.IsTarget && i.IsActiveTarget).OrderBy(x => x.Distance).FirstOrDefault()._directEntity.DirectAbsolutePosition);
                                if (DebugConfig.DebugNavigateOnGrid) Log("Combat.MaxWeaponRange [" + Combat.MaxWeaponRange + "] NextSpotToMoveTo Distance [" + cachedDistanceToNextSpotToMoveTo + "]");
                                if (Combat.MaxWeaponRange - 2000 > cachedDistanceToNextSpotToMoveTo)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid) Log("NextSpotToMoveTo is [" + Math.Round(NextSpotToMoveTo.GetDistance(ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition) / 1000, 0) + "]k away");
                                    ESCache.Instance.ActiveShip.MoveTo(NextSpotToMoveTo);
                                    return true;
                                }
                            }
                            else
                            {
                                Log("NextSpotToMoveTo == null: why?");
                                if (DirectEve.Interval(10000)) ESCache.Instance.AbyssalGate.Orbit(5000);
                            }

                        }
                        else if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalSpeedTankSpot == null)!.!");

                        return true;
                    }
                }

                if (ESCache.Instance.AbyssalMobileTractor != null)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (ESCache.Instance.AbyssalMobileTractor != null) ESCache.Instance.AbyssalMobileTractor.Orbit(5000);");
                    ESCache.Instance.AbyssalMobileTractor.Orbit(500);
                    return true;
                }

                Log("ESCache.Instance.AbyssalGate.Orbit(500)!_!-!");
                ESCache.Instance.AbyssalGate.Orbit(500);
                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return true;
            }
        }

        internal bool boolIsItSafeForLeaderToOrbitGateAfterLooting
        {
            get
            {
                if (!ESCache.Instance.EveAccount.UseFleetMgr && !ESCache.Instance.ActiveShip.IsFrigate)
                    return false;

                if (!ESCache.Instance.EveAccount.IsLeader && !ESCache.Instance.ActiveShip.IsFrigate)
                    return false;

                if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsWebbingEntity))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsWebbingEntity)) return false");
                    return false;
                }

                if (!ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses || (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses && Drones.AllDronesInSpace.Any(i => !i.IsAttacking)))
                {
                    if (Combat.PotentialCombatTargets.Any(i => !i.WillTryToStayWithin40kOfItsTarget && ESCache.Instance.AbyssalGate.DistanceTo(i) > Combat.MaxRange))
                    {
                        if (Combat.PotentialCombatTargets.Count(i => i.BracketType != BracketType.Large_Collidable_Structure && i.IsAttacking) == 1 && AreAllDronesAttacking)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.BracketType != BracketType.Large_Collidable_Structure) == 1 && AreAllDronesAttacking)");
                            return true;
                        }

                        if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Any(i => !i.WillTryToStayWithin40kOfItsTarget && ESCache.Instance.AbyssalGate.DistanceTo(i) > Combat.MaxRange)) return false");
                        return false;
                    }
                }

                if (ESCache.Instance.ActiveShip.IsFrigate)
                {
                    if (50 > ESCache.Instance.ActiveShip.ShieldPercentage)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (80 > ESCache.Instance.ActiveShip.ShieldPercentage) return false");
                        return false;
                    }
                }
                else if (80 > ESCache.Instance.ActiveShip.ShieldPercentage)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (80 > ESCache.Instance.ActiveShip.ShieldPercentage) return false");
                    return false;
                }

                if (40 > ESCache.Instance.ActiveShip.Capacitor && ESCache.Instance.ActiveShip.IsActiveTanked)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (40 > ESCache.Instance.ActiveShip.Capacitor) return false");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("[" + AbyssalSpawn.DetectSpawn + "] if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser)) return false");
                        return false;
                    }

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Lucifer Cynabal")))
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("[" + AbyssalSpawn.DetectSpawn + "] if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains(\"Lucifer Cynabal\"))) return false");
                            return false;
                        }

                        return true;
                    }

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                    {
                        if (Combat.PotentialCombatTargets.Count(i => i.Name.Contains("Vedmak")) >= 2)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("[" + AbyssalSpawn.DetectSpawn + "] if (Combat.PotentialCombatTargets.Count(i => i.Name.Contains(\"Vedmak\")) >= 2) return false");
                            return false;
                        }

                        return true;
                    }

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                    {
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >=2)
                    {
                        return false;
                    }

                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.RodivaSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn ||
                    AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("[" + AbyssalSpawn.DetectSpawn + "] if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser)) return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer) >= 2)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("[" + AbyssalSpawn.DetectSpawn + "] if (Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer) >= 2) return false");
                        return false;
                    }

                    return true;
                }

                return true;
            }
        }

        internal bool MoveIntoRangeOfLoot()
        {
            if (_getMTUInSpace != null)
            {
                if (boolShouldWeBeSpeedTanking)
                {
                    if (_getMTUInSpace.Distance > 20000)
                    {
                        if (DebugConfig.DebugNavigateOnGrid)
                            Log("[" + AbyssalSpawn.DetectSpawn + "]: _getMTUInSpace exists Orbit at 15k");
                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            _getMTUInSpace._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000, _getMTUInSpace._directEntity);
                            return true;
                        }

                        _getMTUInSpace._directEntity.Orbit(15000);
                        return true;
                    }

                    if (_getMTUInSpace.Distance > 12000)
                    {
                        if (DebugConfig.DebugNavigateOnGrid)
                            Log("[" + AbyssalSpawn.DetectSpawn + "]: _getMTUInSpace exists Orbit at 7k");
                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            _getMTUInSpace._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(7000, _getMTUInSpace._directEntity);
                            return true;
                        }

                        _getMTUInSpace._directEntity.Orbit(7000);
                        return true;
                    }

                    if (_getMTUInSpace.Distance > 8000)
                    {
                        if (DebugConfig.DebugNavigateOnGrid)
                            Log("[" + AbyssalSpawn.DetectSpawn + "]: _getMTUInSpace exists Orbit at 500m");
                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            _getMTUInSpace._directEntity.MoveToViaAStar();
                            return true;
                        }

                        _getMTUInSpace._directEntity.Orbit(500);
                        return true;
                    }
                }

                if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                {
                    if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                    {
                        _getMTUInSpace._directEntity.MoveToViaAStar();
                        return true;
                    }

                    _getMTUInSpace._directEntity.Orbit(250);
                    return true;
                }

                if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                {
                    _getMTUInSpace._directEntity.MoveToViaAStar();
                    return true;
                }

                _getMTUInSpace._directEntity.Orbit(250);
                return true;
            }

            if (_getMTUInSpace == null && ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty && !i.Name.ToLower().Contains("Extraction".ToLower()) && i.Name.ToLower().Contains("Cache Wreck".ToLower())))
            {
                EntityCache unlootedWreck = ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty && !i.Name.ToLower().Contains("Extraction".ToLower()) && i.Name.ToLower().Contains("Cache Wreck".ToLower()));
                if (unlootedWreck != null)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (unlootedWreck != null)");
                    if (unlootedWreck.Distance > 20000)
                    {
                        if (DebugConfig.DebugNavigateOnGrid)
                            Log("[" + AbyssalSpawn.DetectSpawn + "]: unlootedWreck [" + unlootedWreck.Nearest1KDistance + "k away ] Orbit at 15k");
                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            unlootedWreck._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000, unlootedWreck._directEntity);
                            return true;
                        }

                        unlootedWreck._directEntity.Orbit(15000);
                        return true;
                    }

                    if (unlootedWreck.Distance > 12000)
                    {
                        if (DebugConfig.DebugNavigateOnGrid)
                            Log("[" + AbyssalSpawn.DetectSpawn + "]: unlootedWreck [" + unlootedWreck.Nearest1KDistance + "k away ] Orbit at 7k");
                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            unlootedWreck._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(7000, unlootedWreck._directEntity);
                            return true;
                        }

                        unlootedWreck._directEntity.Orbit(7000);
                        return true;
                    }

                    if (unlootedWreck.Distance > 8000)
                    {
                        if (DebugConfig.DebugNavigateOnGrid)
                            Log("[" + AbyssalSpawn.DetectSpawn + "]: unlootedWreck [" + unlootedWreck.Nearest1KDistance + "k away ] Orbit at 250m");
                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            unlootedWreck.Orbit(250);
                            return true;
                        }

                        unlootedWreck.Orbit(250);
                        return true;
                    }

                    if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                    {
                        unlootedWreck.Orbit(250);
                        return true;
                    }

                    unlootedWreck._directEntity.Orbit(250);
                    return true;
                }
            }

            try
            {
                if (boolIsItSafeForLeaderToOrbitGateAfterLooting)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (boolIsItSafeForLeaderToOrbitGateAfterLooting)");
                    if (ESCache.Instance.AbyssalGate.Distance > 20000)
                    {
                        if (DebugConfig.DebugNavigateOnGrid)
                            Log("[" + AbyssalSpawn.DetectSpawn + "]: AbyssalGate [" + ESCache.Instance.AbyssalGate.Nearest1KDistance + "k away ] Orbit at 15k");
                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            ESCache.Instance.AbyssalGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000, ESCache.Instance.AbyssalGate._directEntity);
                            return true;
                        }

                        ESCache.Instance.AbyssalGate._directEntity.Orbit(15000);
                        return true;
                    }

                    if (ESCache.Instance.AbyssalGate.Distance > 12000)
                    {
                        if (DebugConfig.DebugNavigateOnGrid)
                            Log("[" + AbyssalSpawn.DetectSpawn + "]: AbyssalGate [" + ESCache.Instance.AbyssalGate.Nearest1KDistance + "k away ] Orbit at 7k");
                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            ESCache.Instance.AbyssalGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(7000, ESCache.Instance.AbyssalGate._directEntity);
                            return true;
                        }

                        ESCache.Instance.AbyssalGate._directEntity.Orbit(7000);
                        return true;
                    }

                    if (ESCache.Instance.AbyssalGate.Distance > 8000)
                    {
                        if (DebugConfig.DebugNavigateOnGrid)
                            Log("[" + AbyssalSpawn.DetectSpawn + "]: AbyssalGate [" + ESCache.Instance.AbyssalGate.Nearest1KDistance + "k away ] Orbit at 250m");
                        if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                        {
                            ESCache.Instance.AbyssalGate.Orbit(250);
                            return true;
                        }

                        ESCache.Instance.AbyssalGate.Orbit(250);
                        return true;
                    }

                    if (AbyssalSpawn.DetectDungeon == AbyssalSpawn.AbyssalDungeonType.AsteroidDungeonCloud)
                    {
                        ESCache.Instance.AbyssalGate.Orbit(250);
                        return true;
                    }

                    ESCache.Instance.AbyssalGate.Orbit(250);
                    return true;
                }

            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }

            return false;
        }

        internal bool SpeedTank_IsItSafeToMoveToMTUOrLootWrecks
        {
            get
            {
                try
                {
                    if (boolSpeedTank_IsItSafeToMoveToMTUOrLootWrecks)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (boolSpeedTank_IsItSafeToMoveToMTUOrLootWrecks)");
                        if (MoveIntoRangeOfLoot()) return true;
                        if (DebugConfig.DebugNavigateOnGrid) Log("MoveIntoRangeOfLoot() returned [ false ]");
                        return false;
                    }

                    if (DebugConfig.DebugNavigateOnGrid) Log("boolSpeedTank_IsItSafeToMoveToMTUOrLootWrecks [ false ]");
                    return false;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        internal bool boolShouldWeMoveIntoRange
        {
            get
            {
                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)
                    return false;

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila && AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) > _marshalTankThreshold)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate || i.IsNPCCruiser))
                        {
                            //dont go running into the concord spawn if there is more than one BS and other stuff. let the other stuff come to us FFS. Stay away from the BSs
                            return false;
                        }
                    }
                }

                if (_singleRoomAbyssal)
                    return false;

                if (boolShouldWeBeSpeedTanking)
                    return false;

                if (allDronesInSpace.Any())
                {
                    if (allDronesInSpace.Any(e => e._directEntity.DroneState == (int)Drones.DroneState.Attacking))
                        return false;
                }

                if (Drones.UseDrones && alldronesInBay.Any() && !allDronesInSpace.Any())
                    return false;

                if (!Combat.PotentialCombatTargets.Any())
                    return false;

                if (Combat.MaxRange > Combat.PotentialCombatTargets.OrderBy(i => i.Distance).FirstOrDefault().Distance)
                    return false;

                return true;
            }
        }

        internal bool WeNeedToGetIntoRange
        {
            get
            {
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar ||
                    ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                        {
                            return false;
                        }
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && i.IsEntityDronesAreShooting))
                            {
                                return false;
                            }

                            if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) > 3)
                            {
                                return false;
                            }

                            if (AreAllDronesAttacking)
                                return false;

                            return true;
                        }
                    }

                    if (AreAllDronesAttacking)
                        return false;
                }

                return true;
            }
        }

        /// <summary>
        /// This is an override for any movement position, if this function returns anything but null, we are forced to go there, no matter what. (except single room abyssals)
        /// </summary>
        internal bool MoveToOverride
        {
            get
            {
                try
                {
                    if (IsSituationThatNecessitatesWeUseOurUniqueOrbitRoutine)
                    {
                        if (DirectEve.Interval(5000))
                            Log($"Keep Transversal Up! IsSituationThatNecessitatesWeUseOurUniqueOrbitRoutine: [true], IsItSafeToMoveToMTUOrLootWrecks [" + IsItSafeToMoveToMTUOrLootWrecks + "]");

                        if (DirectEve.HasFrameChanged())
                        {
                            if (ESCache.Instance.ActiveShip.IsLocatedWithinBioluminescenceCloud && ESCache.Instance.ActiveShip.TypeId != (int)TypeID.StormBringer)
                            {
                                if (DebugConfig.DebugNavigateOnGrid)
                                    Log($"[" + AbyssalSpawn.DetectSpawn + "] IsLocatedWithinBioluminescenceCloud [" + ESCache.Instance.ActiveShip.IsLocatedWithinBioluminescenceCloud + "]");
                                ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(60000, ESCache.Instance.AbyssalCenter._directEntity);
                                return true;
                            }

                            if (!IsOurShipWithintheAbyssBounds())
                            {
                                if (DirectEve.Interval(5000))
                                    Log("!IsOurShipWithintheAbyssBounds");
                                ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(60000, ESCache.Instance.AbyssalCenter._directEntity);
                                return true;
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && i.BracketType != BracketType.Large_Collidable_Structure) && AreAllOurDronesInASpeedCloud && _tooManyEnemiesAreStillInASpeedCloudCount > 4)
                            {
                                Log($"KarybdisTyrannosSpawn: ThisIsBad: if (Combat.PotentialCombatTargets.Any(i => i.IsNPCFrigate && i.BracketType != BracketType.Large_Collidable_Structure) && AreAllOurDronesInASpeedCloud && _tooManyEnemiesAreStillInASpeedCloudCount > 4)");
                                _nextGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(30000, _nextGate._directEntity);
                                return true;
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                            {
                                if (IsItSafeToMoveToMTUOrLootWrecks)
                                {
                                    if (MoveIntoRangeOfLoot()) return true;

                                    if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck)
                                    {
                                        if (DebugConfig.DebugNavigateOnGrid)
                                            Log($"[" + AbyssalSpawn.DetectSpawn + "]: if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck && IsItSafeToMoveToMTUOrLootWrecks): Orbit Gate with High Transversal @ 15000");
                                        _nextGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000, _nextGate._directEntity);
                                        return true;
                                    }
                                }

                                if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsAbyssalKaren && DoWeNeedToGetIntoRangeOfKaren))
                                {
                                    double rangeNeededForKaren = Combat.MaxRange;
                                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                                        rangeNeededForKaren = 20000;

                                    if (Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Distance > rangeNeededForKaren)
                                    {
                                        if (35000 > ESCache.Instance.AbyssalCenter.Distance || 40000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Distance) //if we arent this close to the abyssalcenter wait until we are before getting closer to karen
                                        {
                                            if (30000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Distance)
                                            {
                                                if (20000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Distance)
                                                {
                                                    if (10000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Distance)
                                                    {
                                                        Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Orbit(500);
                                                        return true;
                                                    }

                                                    Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Orbit(5000);
                                                    return true;
                                                }

                                                Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Orbit(15000);
                                                return true;
                                            }

                                            if (DebugConfig.DebugNavigateOnGrid) Log("!if (30000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Distance): OrbitWithHighTransversal(25000)");
                                            if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                                            {
                                                Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Orbit(15000);
                                                return true;
                                            }

                                            Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalKaren).Orbit(25000);
                                            return true;
                                        }
                                    }
                                }

                                _nextGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000, _nextGate._directEntity);
                                return true;
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                            {
                                if (IsItSafeToMoveToMTUOrLootWrecks)
                                {
                                    if (MoveIntoRangeOfLoot()) return true;

                                    if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck)
                                    {
                                        if (DebugConfig.DebugNavigateOnGrid)
                                            Log($"[" + AbyssalSpawn.DetectSpawn + "]: if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck && IsItSafeToMoveToMTUOrLootWrecks): Orbit Gate with High Transversal @ 15000");
                                        _nextGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000, _nextGate._directEntity);
                                        return true;
                                    }
                                }

                                if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsAbyssalDroneBattleship))
                                {
                                    double rangeNeededForKaren = Combat.MaxRange;
                                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                                        rangeNeededForKaren = 20000;

                                    if (Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalDroneBattleship).Distance > rangeNeededForKaren)
                                    {
                                        if (35000 > ESCache.Instance.AbyssalCenter.Distance || 40000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalDroneBattleship).Distance) //if we arent this close to the abyssalcenter wait until we are before getting closer
                                        {
                                            if (30000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalDroneBattleship).Distance)
                                            {
                                                if (20000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalDroneBattleship).Distance)
                                                {
                                                    if (10000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalDroneBattleship).Distance)
                                                    {
                                                        Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalDroneBattleship).Orbit(500);
                                                        return true;
                                                    }

                                                    Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalDroneBattleship).Orbit(5000);
                                                    return true;
                                                }

                                                Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalDroneBattleship).Orbit(7000);
                                                return true;
                                            }

                                            if (DebugConfig.DebugNavigateOnGrid) Log("!if (30000 > Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalDroneBattleship).Distance): OrbitWithHighTransversal(25000)");
                                            Combat.PotentialCombatTargets.FirstOrDefault(i => i._directEntity.IsAbyssalDroneBattleship).Orbit(22000);
                                            return true;
                                        }
                                    }
                                }

                                ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(21000, ESCache.Instance.AbyssalCenter._directEntity);
                                return true;
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
                            {
                                if (IsItSafeToMoveToMTUOrLootWrecks)
                                {
                                    if (MoveIntoRangeOfLoot()) return true;

                                    if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck && IsItSafeToMoveToMTUOrLootWrecks)
                                    {
                                        if (DebugConfig.DebugNavigateOnGrid)
                                            Log($"[" + AbyssalSpawn.DetectSpawn + "]: if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck && IsItSafeToMoveToMTUOrLootWrecks): Orbit Gate with High Transversal @ 15000");
                                        _nextGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000, _nextGate._directEntity);
                                        return true;
                                    }
                                }

                                ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(25000, ESCache.Instance.AbyssalCenter._directEntity);
                                return true;
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                            {
                                if (IsItSafeToMoveToMTUOrLootWrecks)
                                {
                                    if (MoveIntoRangeOfLoot()) return true;

                                    if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck)
                                    {
                                        if (DebugConfig.DebugNavigateOnGrid)
                                            Log($"[" + AbyssalSpawn.DetectSpawn + "]: if No MTU and Wrecks are empty: Orbit Gate with High Transversal @ 15000");
                                        _nextGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000, _nextGate._directEntity);
                                        return true;
                                    }
                                }

                                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser && 26000 > i.Distance) && 95 > ESCache.Instance.ActiveShip.ArmorPercentage)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid)
                                        Log($"[" + AbyssalSpawn.DetectSpawn + "]: if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser && 26000 > i.Distance))");

                                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser && 26000 > i.Distance) >= 6)
                                    {
                                        Log("Notification!: We have [" + Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser && 26000 > i.Distance) + " ] BCs within 26k!");
                                        Combat.PotentialCombatTargets.OrderBy(x => x.IsAttacking).ThenBy(y => y.Distance).FirstOrDefault(i => i.IsNPCBattlecruiser)._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(30000, Combat.PotentialCombatTargets.OrderBy(x => x.IsAttacking).ThenBy(y => y.Distance).FirstOrDefault(i => i.IsNPCBattlecruiser)._directEntity);
                                        return true;
                                    }

                                    return true;
                                }

                                if (DebugConfig.DebugNavigateOnGrid)
                                    Log($"[" + AbyssalSpawn.DetectSpawn + "]: ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(30000);");
                                ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(30000, ESCache.Instance.AbyssalCenter._directEntity);
                                return true;
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                            {
                                if (IsItSafeToMoveToMTUOrLootWrecks)
                                {
                                    if (MoveIntoRangeOfLoot()) return true;

                                    if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck && IsItSafeToMoveToMTUOrLootWrecks)
                                    {
                                        if (DebugConfig.DebugNavigateOnGrid)
                                            Log($"[" + AbyssalSpawn.DetectSpawn + "]: if No MTU and Wrecks are empty: Orbit Gate with High Transversal @ 15000");
                                        _nextGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000, _nextGate._directEntity);
                                        return true;
                                    }
                                }

                                if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                                {
                                    if (ESCache.Instance.EntityWithHighestNeutingPotential != null)
                                    {
                                        ESCache.Instance.EntityWithHighestNeutingPotential.Orbit(30000);
                                        return true;
                                    }

                                    ESCache.Instance.AbyssalGate.Orbit(45000);
                                    return true;
                                }

                                ESCache.Instance.AbyssalGate.Orbit(30000);
                                return true;
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                            {
                                if (IsItSafeToMoveToMTUOrLootWrecks)
                                {
                                    if (MoveIntoRangeOfLoot()) return true;
                                }

                                if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck && IsItSafeToMoveToMTUOrLootWrecks)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid)
                                        Log($"[" + AbyssalSpawn.DetectSpawn + "]: if No MTU and Wrecks are empty: Orbit Gate with High Transversal");
                                    _nextGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000, _nextGate._directEntity);
                                    return true;
                                }

                                if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                                {
                                    if (ESCache.Instance.AbyssalSpeedTankSpot != null)
                                    {
                                        Log("if (AbyssalSpeedTankSpot != null): this may need better pathing: test!?!");
                                        DirectWorldPosition NextSpotToMoveTo = DirectWorldPosition.GetNextStepInPathToSpeedTankSpotOnImaginarySphere(ESCache.Instance.AbyssalSpeedTankSpot);
                                        if (NextSpotToMoveTo != null)
                                        {
                                            ESCache.Instance.ActiveShip.MoveTo(NextSpotToMoveTo);
                                            return true;
                                        }

                                        Log("if (NextSpotToMoveTo != null)!");
                                        ESCache.Instance.AbyssalGate.Orbit(42000);
                                        return true;
                                    }

                                    Log("if (AbyssalSpeedTankSpot == null) !");
                                    ESCache.Instance.AbyssalGate.Orbit(42000);
                                    return true;
                                }

                                ESCache.Instance.AbyssalGate.Orbit(30000);
                                return true;
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn || AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedHunter)
                            {
                                if (IsItSafeToMoveToMTUOrLootWrecks)
                                {
                                    if (MoveIntoRangeOfLoot()) return true;
                                }

                                if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck && IsItSafeToMoveToMTUOrLootWrecks)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid)
                                        Log($"[" + AbyssalSpawn.DetectSpawn + "]: if No MTU and Wrecks are empty: Orbit Gate with High Transversal");
                                    _nextGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000, _nextGate._directEntity);
                                    return true;
                                }

                                if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                                {
                                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                                    {
                                        if (ESCache.Instance.EntityWithHighestNeutingPotential != null)
                                        {
                                            ESCache.Instance.EntityWithHighestNeutingPotential.Orbit((int)Combat.MaxRange - 3000);
                                            return true;
                                        }

                                        Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCCruiser).Orbit((int)Combat.MaxRange - 3000); //worm
                                        return true;
                                    }

                                    return true;
                                }

                                ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(30000, ESCache.Instance.AbyssalCenter._directEntity);
                                return true;
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                            {
                                if (IsItSafeToMoveToMTUOrLootWrecks)
                                {
                                    if (MoveIntoRangeOfLoot()) return true;
                                }

                                if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid)
                                        Log($"[" + AbyssalSpawn.DetectSpawn + "]: if No MTU and Wrecks are empty: Orbit Gate with High Transversal");
                                    _nextGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000, _nextGate._directEntity);
                                    return true;
                                }

                                ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(60000, ESCache.Instance.AbyssalCenter._directEntity);
                                return true;
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                            {
                                if (IsItSafeToMoveToMTUOrLootWrecks)
                                {
                                    if (MoveIntoRangeOfLoot()) return true;
                                }

                                if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid)
                                        Log($"[" + AbyssalSpawn.DetectSpawn + "]: if No MTU and Wrecks are empty: Orbit Gate with High Transversal");
                                    _nextGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000, _nextGate._directEntity);
                                    return true;
                                }

                                if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                                {
                                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                                    {
                                        if (ESCache.Instance.EntityWithHighestNeutingPotential != null)
                                        {
                                            ESCache.Instance.EntityWithHighestNeutingPotential.Orbit(30000);
                                            return true;
                                        }

                                        Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCCruiser).Orbit(28000); //worm
                                        return true;
                                    }

                                    Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsNPCCruiser).Orbit(28000);
                                    return true;
                                }

                                ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(35000, ESCache.Instance.AbyssalCenter._directEntity);
                                return true;
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakVilaCruiserSwarmerSpawn)
                            {
                                if (IsItSafeToMoveToMTUOrLootWrecks)
                                {
                                    if (MoveIntoRangeOfLoot()) return true;
                                }

                                if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid)
                                        Log($"[" + AbyssalSpawn.DetectSpawn + "]: if No MTU and Wrecks are empty: Orbit Gate with High Transversal");
                                    _nextGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000, _nextGate._directEntity);
                                    return true;
                                }

                                ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(35000, ESCache.Instance.AbyssalCenter._directEntity);
                                return true;
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.RodivaSpawn)
                            {
                                if (IsItSafeToMoveToMTUOrLootWrecks)
                                {
                                    if (MoveIntoRangeOfLoot()) return true;
                                }

                                if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid)
                                        Log($"[" + AbyssalSpawn.DetectSpawn + "]: if No MTU and Wrecks are empty: Orbit Gate with High Transversal");
                                    _nextGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000, _nextGate._directEntity);
                                    return true;
                                }

                                ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(35000, ESCache.Instance.AbyssalCenter._directEntity);
                                return true;
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                            {
                                if (IsItSafeToMoveToMTUOrLootWrecks)
                                {
                                    if (MoveIntoRangeOfLoot()) return true;
                                }

                                if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck)
                                {
                                    if (DebugConfig.DebugNavigateOnGrid)
                                        Log($"[" + AbyssalSpawn.DetectSpawn + "]: if No MTU and Wrecks are empty: Orbit Gate with High Transversal");
                                    _nextGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000, _nextGate._directEntity);
                                    return true;
                                }

                                if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                                {
                                    if (Combat.PotentialCombatTargets.Any())
                                    {
                                        try
                                        {
                                            Combat.PotentialCombatTargets
                                                .OrderByDescending(x => x.IsAttacking)
                                                .ThenBy(y => y.Distance)
                                                .FirstOrDefault(i =>
                                                i.TypeName.ToLower().Contains("kikimora".ToLower())
                                                )
                                                .Orbit((int)Combat.MaxWeaponRange - 4000);
                                        }
                                        catch (Exception ex)
                                        {
                                            Log("Exception [" + ex + "]");
                                        }

                                        return true;
                                    }
                                }

                                if (Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer) > _kikimoraTankThreshold)
                                {
                                    if (DirectEve.Interval(10000)) Log("Notification!: We have [" + Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer) + " ] Destroyers > [" + _kikimoraTankThreshold + "]");
                                    if (ESCache.Instance.ActiveShip.Entity.IsTooCloseToSmallDeviantAutomataSuppressor)
                                    {
                                        if (DirectEve.Interval(10000)) Log("Notification!: We are IsTooCloseToSmallDeviantAutomataSuppressor [" + ESCache.Instance.ActiveShip.Entity.IsTooCloseToSmallDeviantAutomataSuppressor + "] - they kill drones and make us pull drones alot: get away");
                                        if (!ESCache.Instance.AbyssalCenter._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor)
                                        {
                                            if (40000 > ESCache.Instance.AbyssalCenter.Distance)
                                            {
                                                ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(25000, ESCache.Instance.AbyssalCenter._directEntity);
                                                return true;
                                            }
                                        }

                                        if (!ESCache.Instance.PlayerSpawnLocation._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor)
                                        {
                                            if (30000 > ESCache.Instance.PlayerSpawnLocation.Distance)
                                            {
                                                ESCache.Instance.PlayerSpawnLocation._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(25000, ESCache.Instance.PlayerSpawnLocation._directEntity);
                                                return true;
                                            }
                                        }
                                    }
                                }

                                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer && i.Distance > 24000))
                                {
                                    if (DirectEve.Interval(10000)) Log("Notification!: We have [" + Combat.PotentialCombatTargets.Count(i => i.IsNPCDestroyer && i.Distance > 24000) + " ] Kikimoras further than 24k!");
                                    if (GetCurrentStageStageSeconds > 90)
                                    {
                                        //if (PlayNotificationSounds) Util.PlayNoticeSound();
                                        //if (PlayNotificationSounds) Util.PlayNoticeSound();
                                    }

                                    Combat.PotentialCombatTargets.OrderBy(y => y.Distance).FirstOrDefault(i => i.IsNPCDestroyer)._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(20000, Combat.PotentialCombatTargets.OrderBy(y => y.Distance).FirstOrDefault(i => i.IsNPCDestroyer)._directEntity);
                                    return true;
                                }

                                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
                                {
                                    Combat.PotentialCombatTargets.OrderBy(y => y.Distance).FirstOrDefault(i => i.IsNPCDestroyer)._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(20000, Combat.PotentialCombatTargets.OrderBy(y => y.Distance).FirstOrDefault(i => i.IsNPCDestroyer)._directEntity);
                                    return true;
                                }

                                ESCache.Instance.AbyssalGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(5000, ESCache.Instance.AbyssalGate._directEntity);
                                return true;
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn)
                            {
                                try
                                {
                                    if (IsItSafeToMoveToMTUOrLootWrecks)
                                    {
                                        if (MoveIntoRangeOfLoot()) return true;
                                    }

                                    if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck)
                                    {
                                        if (DebugConfig.DebugNavigateOnGrid)
                                            Log($"[" + AbyssalSpawn.DetectSpawn + "]: if No MTU and Wrecks are empty: Orbit Gate with High Transversal");
                                        _nextGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(15000, _nextGate._directEntity);
                                        return true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log("Exception [" + ex + "]");
                                    ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(35000, ESCache.Instance.AbyssalCenter._directEntity);
                                    return true;
                                }

                                if (DebugConfig.DebugNavigateOnGrid)
                                    Log($"[" + AbyssalSpawn.DetectSpawn + "]: ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(35000);!!!");
                                ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(35000, ESCache.Instance.AbyssalCenter._directEntity);
                                return true;
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn)
                            {
                                try
                                {
                                    if (IsItSafeToMoveToMTUOrLootWrecks)
                                    {
                                        if (MoveIntoRangeOfLoot()) return true;
                                    }

                                    if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck)
                                    {
                                        if (DebugConfig.DebugNavigateOnGrid)
                                            Log($"[" + AbyssalSpawn.DetectSpawn + "]: if No MTU and Wrecks are empty: Orbit Gate with High Transversal");
                                        _nextGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(10000, _nextGate._directEntity);
                                        return true;
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Log("Exception [" + ex + "]");
                                    ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(35000, ESCache.Instance.AbyssalCenter._directEntity);
                                    return true;
                                }

                                if (DebugConfig.DebugNavigateOnGrid)
                                    Log($"[" + AbyssalSpawn.DetectSpawn + "]: ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(35000);!!!");
                                ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(35000, ESCache.Instance.AbyssalCenter._directEntity);
                                return true;
                            }

                            //if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.RogueDroneFrigateSpawnWithMediumAtomitaSupressor)
                            //{
                            //    ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.FirstOrDefault()._directEntity.DirectAbsolutePosition.Orbit(10000);
                            //    return;
                            //}

                            if (ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate)
                            {
                                if (Combat.PotentialCombatTargets.All(i => !i.IsTarget))
                                {
                                    Combat.PotentialCombatTargets.OrderBy(i => i.Distance).FirstOrDefault().Orbit(28000);
                                    return true;
                                }
                            }

                            if (IsItSafeToMoveToMTUOrLootWrecks)
                            {
                                if (MoveIntoRangeOfLoot()) return true;
                            }

                            if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck)
                            {
                                if (DebugConfig.DebugNavigateOnGrid)
                                    Log($"[" + AbyssalSpawn.DetectSpawn + "]: if No MTU and Wrecks are empty: Orbit Gate with High Transversal");
                                _nextGate._directEntity.DirectAbsolutePosition.OrbitWithHighTransversal(10000, _nextGate._directEntity);
                                return true;
                            }

                            Log("default orbit: 3k around moveToTargetEntity [" + moveToTargetEntity.TypeName + "]");
                            _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(3000);
                        }

                        return true;
                    }
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex  +"]");
                    _moveToTarget_MTU_Wreck_Can_or_Gate_Entity.Orbit(10000);
                    return true;
                }


                // Always ensure we are in range to deal damage
                var targetsOrderedByDistance = Combat.PotentialCombatTargets.Where(i => i.BracketType != BracketType.Large_Collidable_Structure).OrderBy(x => x.Distance);
                var sumInBayInSpaceDrones = alldronesInBay.Count + allDronesInSpace.Count;
                var range = sumInBayInSpaceDrones == 0 ? (Combat.MaxWeaponRange - 1000 < 0 ? Combat.MaxWeaponRange : Combat.MaxWeaponRange - 1000) : _maxRange;

                if (boolShouldWeMoveIntoRange)
                {
                    if (DirectEve.Interval(10000))
                        Log("The closest entity is outside of our range [" + range + "], moving towards the closest enemy. Distance to closest enemy [{targetsOrderedByDistance.First().Distance}] Typename [{targetsOrderedByDistance.First().TypeName}]");
                    if (MoveToThisDirectWorldPosition(targetsOrderedByDistance.First()._directEntity.DirectAbsolutePosition))
                        return true;

                    return true;
                }

                /**
                if (!_shipWasCloseToTheBoundaryAndEnemiesInOptimalDuringThisStage && AreWeCloseToTheAbyssBoundary &&
                    AreTheMajorityOfNPCsInOptimalOnGrid() && !IsItSafeToMoveToMTU)
                {
                    if (DirectEve.Interval(10000))
                        Log($"We are close to the abyss bounds and the majority of the enemies are in optimal, skipping overriding the move-to destination for this stage.");
                    _shipWasCloseToTheBoundaryAndEnemiesInOptimalDuringThisStage = true;
                }

                if (_shipWasCloseToTheBoundaryAndEnemiesInOptimalDuringThisStage)
                    return false;
                **/

                // Force us to go back into the boundary if there are less than 2 geckos left.
                if (!IsOurShipWithintheAbyssBounds() && ESCache.Instance.ActiveShip.GetRemainingDroneBandwidth() > 20)
                {
                    if (DirectEve.Interval(5000))
                        Log("!IsOurShipWithintheAbyssBounds && ESCache.Instance.ActiveShip.GetRemainingDroneBandwidth() > 25");

                    return false;
                }

                bool _doWeNeedtoMoveToTheGate = DoWeNeedToMoveToTheGate();
                if (CurrentStageRemainingSecondsWithoutPreviousStages < 320 && _doWeNeedtoMoveToTheGate)
                {
                    if (DirectEve.Interval(5000))
                    {
                        Log($"We need to move to the gate: CurrentStageRemainingSecondsWithoutPreviousStages [" + Math.Round(CurrentStageRemainingSecondsWithoutPreviousStages, 0) + "] DoWeNeedtoMoveToTheGate [" + _doWeNeedtoMoveToTheGate + "]");
                    }

                    return false;
                }

                if (_lastMoveToOverride.AddMilliseconds(250) > DateTime.UtcNow && _moveToOverride != null)
                    return true;

                //if (!_maxDroneRange.HasValue)
                //{
                //    Log($"Warning: MaxDroneRange has no value!");
                //    _maxDroneRange = 80000;
                //}

                _moveToOverride = null;

                if (_nextGate == null)
                    return false;

                if (!_moveBackwardsDirection.HasValue)
                {
                    var direction = _nextGate._directEntity.DirectAbsolutePosition.GetDirectionalVectorTo(_activeShipPos)
                        .Normalize(); // go backwards!
                    _moveBackwardsDirection = DirectSceneManager.RotateVector(direction, 5);
                }

                double _minMoveToRange = ESCache.Instance.ActiveShip.MaxTargetRange - 1000;
                double _maxMoveToRange = ESCache.Instance.ActiveShip.MaxTargetRange - 500;

                //if (Combat.PotentialCombatTargets.Count(e => e.TypeName.ToLower().Contains("kikimora")) > _kikimoraTankThreshold)
                //{
                //    var nearestKiki = Combat.PotentialCombatTargets
                //        .Where(e => e.TypeName.ToLower().Contains("kikimora"))
                //        .OrderBy(e => e.Distance).FirstOrDefault(); // pick the nearest kiki
                //
                //    if (MoveToUsingBestMethod(nearestKiki, _minMoveToRange, _maxMoveToRange))
                //        return true;
                //
                //    return false;
                //}
                //else

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn
                    && Combat.PotentialCombatTargets.Count(e => e.IsNPCBattlecruiser) > _bcTankthreshold)
                {
                    if (DirectEve.Interval(15000)) Log("DetectSpawn [HighAngleDroneBattleCruiserSpawn] IsNPCBattlecruiser [" + Combat.PotentialCombatTargets.Count(e => e.IsNPCBattlecruiser) + "] > _bcTankthreshold [" + _bcTankthreshold + "]");
                    var nearestRogueBc = Combat.PotentialCombatTargets.Where(e => e.IsNPCBattlecruiser)
                        .OrderBy(e => e.Distance).FirstOrDefault(); // pick the nearest rogue drone bc

                    if (nearestRogueBc == null)
                    {
                        Log("if (nearestRogueBc == null)");
                    }
                    else if (MoveToUsingBestMethod(nearestRogueBc, _minMoveToRange, _maxMoveToRange))
                        return true;

                    return false;
                }
                else if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn
                    && ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.Any()  &&
                    Combat.PotentialCombatTargets.Count(e => e.IsNPCFrigate || e.IsNPCCruiser) >= 13)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn && ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.Any())");
                    EntityCache deviantAutomataSuppressor = ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.Where(x => 50000 > x.Distance).OrderBy(x => x.Distance).FirstOrDefault(i => i.Name.Contains("Long-Range"));
                    if (deviantAutomataSuppressor == null) deviantAutomataSuppressor = ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.Where(x => 50000 > x.Distance).OrderBy(x => x.Distance).FirstOrDefault(i => i.Name.Contains("Medium-Range"));
                    if (deviantAutomataSuppressor == null) deviantAutomataSuppressor = ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.Where(x => 50000 > x.Distance).OrderBy(x => x.Distance).FirstOrDefault(i => i.Name.Contains("Short-Range"));

                    if (deviantAutomataSuppressor != null)
                    {
                        if (DirectEve.Interval(15000) && deviantAutomataSuppressor.Orbit(5000))
                            return true;
                    }

                    return false;
                }
                //else if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn
                //    && ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon.Any() &&
                //    Combat.PotentialCombatTargets.Count(e => e.IsNPCFrigate || e.IsNPCCruiser) >= 10)
                //{
                //    EntityCache trackingPylon = ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon.OrderBy(x => x.Distance).FirstOrDefault();
                //    if (trackingPylon != null)
                //    {
                //        if (trackingPylon.Orbit(5000))
                //            return true;
                //    }
                //
                //    return false;
                //}
                else if (Combat.PotentialCombatTargets.Count(e => e.TypeName.Contains("Lucifer Cynabal")) >= 3 && ESCache.Instance.ActiveShip.TypeId != (int)TypeID.Ishtar)
                {
                    if (DirectEve.Interval(15000)) Log("if (Combat.PotentialCombatTargets.Count(e => e.TypeName.Contains(\"Lucifer Cynabal\")) >= 3)");
                    var nearestCynabal = Combat.PotentialCombatTargets.Where(e => e.TypeName.Contains("Lucifer Cynabal"))
                        .OrderBy(e => e.Distance).FirstOrDefault();

                    _minMoveToRange = 15000;
                    _maxMoveToRange = 20000;

                    if (MoveToUsingBestMethod(nearestCynabal, _minMoveToRange, _maxMoveToRange))
                        return true;

                    return false;
                }
                // handle marshals
                // here we should dive into a deviata supressor (which kills missles?) or go straight backwards keep at range. missile range of marshals is: 2.70 * 6 * 4300 = ~70k, we have 80k drone range.
                else if (_marshalsOnGridCount > _marshalTankThreshold)
                {
                    if (DirectEve.Interval(15000)) Log("if (_marshalsOnGridCount [" + _marshalsOnGridCount + "] >= _marshalTankThreshold [" + _marshalTankThreshold + "])");
                    var nearestMarhsal = Combat.PotentialCombatTargets.Where(e => e._directEntity.IsAbyssalMarshal).OrderBy(e => e.Distance)
                        .FirstOrDefault(); // pick the nearest

                    _minMoveToRange = 73000;
                    _maxMoveToRange = 74000;

                    if (nearestMarhsal == null)
                    {
                        Log("if (nearestMarhsal == null)");
                    }
                    else if (MoveToUsingBestMethod(nearestMarhsal, _minMoveToRange, _maxMoveToRange))
                        return true;

                    return false;
                }
                else if (Combat.PotentialCombatTargets.Any(e => e.TypeName.ToLower().Contains("Stormbringer".ToLower()) && 60000 > e.Distance))
                {
                    if (DirectEve.Interval(15000)) Log("if (Combat.PotentialCombatTargets.Any(e => e.TypeName.ToLower().Contains(\"Stormbringer\".ToLower()) && 60000 > e.Distance))");
                    EntityCache deviantAutomataSuppressor = ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.Where(x => 50000 > x.Distance).OrderBy(x => x.Distance).FirstOrDefault(i => i.Name.Contains("Long-Range"));
                    if (deviantAutomataSuppressor == null) deviantAutomataSuppressor = ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.Where(x => 50000 > x.Distance).OrderBy(x => x.Distance).FirstOrDefault(i => i.Name.Contains("Medium-Range"));
                    if (deviantAutomataSuppressor == null) deviantAutomataSuppressor = ESCache.Instance.AbyssalDeadspaceDeviantAutomataSuppressor.Where(x => 50000 > x.Distance).OrderBy(x => x.Distance).FirstOrDefault(i => i.Name.Contains("Short-Range"));

                    if (deviantAutomataSuppressor != null)
                    {
                        if (DirectEve.Interval(15000) && deviantAutomataSuppressor.Orbit(5000))
                            return true;
                    }

                    return false;
                }
                //else if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn
                //    && ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon.Any() &&
                //    Combat.PotentialCombatTargets.Count(e => e.IsNPCFrigate || e.IsNPCCruiser) >= 10)
                //{
                //    EntityCache trackingPylon = ESCache.Instance.AbyssalDeadspaceMultibodyTrackingPylon.OrderBy(x => x.Distance).FirstOrDefault();
                //    if (trackingPylon != null)
                //    {
                //        if (trackingPylon.Orbit(5000))
                //            return true;
                //    }
                //
                //    return false;
                //}
                // handle marshals
                // here we should dive into a deviata supressor (which kills missles?) or go straight backwards keep at range. missile range of marshals is: 2.70 * 6 * 4300 = ~70k, we have 80k drone range.

                else if (Combat.PotentialCombatTargets.Any(e => e.TypeName.ToLower().Contains("Thunderchild".ToLower()) && 60000 > e.Distance))
                {
                    if (DirectEve.Interval(15000)) Log("if (Combat.PotentialCombatTargets.Any(e => e.TypeName.ToLower().Contains(\"Thunderchild\".ToLower()) && 60000 > e.Distance))");
                    var nearestThunderchild = Combat.PotentialCombatTargets.Where(e => e.TypeName.ToLower().Contains("Thunderchild".ToLower()) && 60000 > e.Distance).OrderBy(e => e.Distance)
                        .FirstOrDefault(); // pick the nearest

                    _minMoveToRange = 73000;
                    _maxMoveToRange = 74000;

                    if (nearestThunderchild == null)
                    {
                        Log("if (nearestThunderchild == null)");
                    }
                    else if (MoveToUsingBestMethod(nearestThunderchild, _minMoveToRange, _maxMoveToRange))
                        return true;

                    return false;
                }
                else if (_leshaksOnGridCount > _leshakTankThreshold) // here we should dive into a deviata supressor (which kills missles?) or go straight backwards keep at range. missile range of marshals is: 2.70 * 6 * 4300 = ~70k, we have 80k drone range.
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (_leshaksOnGridCount [" + _leshaksOnGridCount + "] >= _leshakTankThreshold [" + _leshakTankThreshold + "])");
                    var nearestLeshak = Combat.PotentialCombatTargets.Where(e => e._directEntity.IsAbyssalLeshak).OrderBy(e => e.Distance)
                        .FirstOrDefault(); // pick the nearest

                    _minMoveToRange = 66000;
                    _maxMoveToRange = 68000;

                    if (nearestLeshak == null)
                    {
                        Log("if (nearestLeshak == null)");
                    }
                    else if (MoveToUsingBestMethod(nearestLeshak, _minMoveToRange, _maxMoveToRange))
                        return true;

                    return false;
                }
                else if (Combat.PotentialCombatTargets.Count(i => i.Name.ToLower().Contains("Starving Leshak".ToLower())) >= 4) // move straight backwards keep at range. missile range of marshals is: 2.70 * 6 * 4300 = ~70k, we have 80k drone range.
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.Name.ToLower().Contains(\"Starving Leshak\".ToLower())) >= 4)");
                    var nearestStarvingLeshak = Combat.PotentialCombatTargets.Where(e => e.TypeName.ToLower().Contains("Starving Leshak".ToLower()) && 60000 > e.Distance).OrderBy(e => e.Distance)
                        .FirstOrDefault(); // pick the nearest

                    _minMoveToRange = 73000;
                    _maxMoveToRange = 74000;

                    if (nearestStarvingLeshak == null)
                    {
                        Log("if (nearestStarvingLeshak == null)");
                    }
                    else if (MoveToUsingBestMethod(nearestStarvingLeshak, _minMoveToRange, _maxMoveToRange))
                        return true;

                    return false;
                }
                else if (_droneBattleshipsOnGridCount > _droneBattleshipsTankThreshold) // here we should dive into a deviata supressor (which kills missles?) or go straight backwards keep at range. missile range of marshals is: 2.70 * 6 * 4300 = ~70k, we have 80k drone range.
                {
                    var nearestDroneBattleship = Combat.PotentialCombatTargets.Where(e => e._directEntity.IsAbyssalDroneBattleship).OrderBy(e => e.Distance)
                        .FirstOrDefault(); // pick the nearest

                    _minMoveToRange = 35000;
                    _maxMoveToRange = 37000;

                    if (nearestDroneBattleship == null)
                    {
                        Log("if (nearestDroneBattleship == null)");
                    }
                    else if (MoveToUsingBestMethod(nearestDroneBattleship, _minMoveToRange, _maxMoveToRange))
                        return true;

                    return false;
                }
                else if (_lucidDeepwatcherBattleshipsOnGridCount > _lucidDeepwatcherBattleshipsTankThreshold) // here we should dive into a deviata supressor (which kills missles?) or go straight backwards keep at range. missile range of marshals is: 2.70 * 6 * 4300 = ~70k, we have 80k drone range.
                {
                    var nearestLucidDeepwatcherBattleship = Combat.PotentialCombatTargets.Where(e => e._directEntity.IsAbyssalLucidDeepwatcherBattleship).OrderBy(e => e.Distance)
                        .FirstOrDefault(); // pick the nearest

                    _minMoveToRange = 68000;
                    _maxMoveToRange = 69000;

                    if (nearestLucidDeepwatcherBattleship == null)
                    {
                        Log("if (nearestLucidDeepwatcherBattleship == null)");
                    }
                    else if (MoveToUsingBestMethod(nearestLucidDeepwatcherBattleship, _minMoveToRange, _maxMoveToRange))
                        return true;

                    return false;
                }
                else if (Combat.PotentialCombatTargets.Count(i => i.Name.ToLower().Contains("Lucid Sentinel".ToLower())) >= 4) //LucidCruiserSpawn
                {
                    var nearestLucidSentinel = Combat.PotentialCombatTargets.Where(e => e.Name.ToLower().Contains("Lucid Sentinel".ToLower())).OrderBy(e => e.Distance)
                        .FirstOrDefault(); // pick the nearest

                    _minMoveToRange = 68000;
                    _maxMoveToRange = 69000;

                    if (nearestLucidSentinel == null)
                    {
                        Log("if (nearestLucidSentinel == null)");
                    }
                    else if (MoveToUsingBestMethod(nearestLucidSentinel, _minMoveToRange, _maxMoveToRange))
                        return true;

                    return false;
                }
                else if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn &&
                    (Combat.PotentialCombatTargets.Count(i => i.Name.ToLower().Contains("Ephialtes Dissipator".ToLower())) >= _cruiserTankThreshold))
                        //EphialtesCruiserSpawn
                {
                    var nearestNeutingCruiser = Combat.PotentialCombatTargets.Where(e => e.Name.ToLower().Contains("Ephialtes Dissipator".ToLower())).OrderBy(e => e.Distance)
                        .FirstOrDefault(); // pick the nearest

                    _minMoveToRange = 68000;
                    _maxMoveToRange = 69000;

                    if (nearestNeutingCruiser == null)
                    {
                        Log("if (nearestNeutingCruiser == null)");
                    }
                    else if (MoveToUsingBestMethod(nearestNeutingCruiser, _minMoveToRange, _maxMoveToRange))
                        return true;

                    return false;
                }
                else if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn &&
                        (Combat.PotentialCombatTargets.Count(i => i.Name.ToLower().Contains("Drifter Nullcharge Cruiser".ToLower())) >= _cruiserTankThreshold))
                //EphialtesCruiserSpawn
                {
                    var nearestDrifterNullchargeCruiser = Combat.PotentialCombatTargets.Where(e => e.Name.ToLower().Contains("Drifter Nullcharge Cruiser".ToLower())).OrderBy(e => e.Distance)
                        .FirstOrDefault(); // pick the nearest

                    _minMoveToRange = 68000;
                    _maxMoveToRange = 69000;

                    if (nearestDrifterNullchargeCruiser == null)
                    {
                        Log("if (nearestDrifterNullchargeCruiser == null)");
                    }
                    else if (MoveToUsingBestMethod(nearestDrifterNullchargeCruiser, _minMoveToRange, _maxMoveToRange))
                        return true;

                    return false;
                }
                //
                // need a way to safely approach karen and orbit....
                //
                //else if (_karenOnGridCount > 0) // here we should dive into a deviata supressor (which kills missles?) or go straight backwards keep at range. missile range of marshals is: 2.70 * 6 * 4300 = ~70k, we have 80k drone range.
                //{
                /**
                var nearestKaren = TargetsOnGrid.Where(e => e.IsAbyssalKaren).OrderBy(e => e.Distance)
                    .FirstOrDefault(); // pick the nearest

                if (allDronesInSpace.All(i => i.DroneStateName.ToLower() == "Attacking".ToLower()) || ESCache.Instance.ActiveShip.MaxTargetRange >= 74500)
                {
                    _minMoveToRange = 74000;
                    _maxMoveToRange = 74500;
                }

                // https://everef.net/type/48122 - optimal range 63k
                // if we are already further way do not override
                if (_minMoveToRange > nearestKaren.Distance)
                {
                    Log("Karen: Moving Away");
                    _moveToOverride = CalculateMoveToOverrideSpot(_moveBackwardsDirection, nearestKaren, _minMoveToRange, _maxMoveToRange);
                }
                **/
                //}
                else if (Combat.PotentialCombatTargets.Sum(e => e._directEntity.GigaJouleNeutedPerSecond) >= ESCache.Instance.ActiveShip.MaxGigaJoulePerSecondTank)
                {
                    if (DirectEve.Interval(5000)) Log("All NPCs GigaJouleNeutedPerSecond [" + Combat.PotentialCombatTargets.Sum(e => e._directEntity.GigaJouleNeutedPerSecond) + "] > MaxGigaJoulePerSecondTank [" + Math.Round(ESCache.Instance.ActiveShip.MaxGigaJoulePerSecondTank ?? 0, 1) + "]");
                    // if the neuts on grid neut cap with more than "_maxGigaJouleCapTank" per second
                    var nearestNeut = Combat.PotentialCombatTargets.Where(e => e._directEntity.IsNeutingEntity).OrderBy(e => e.Distance)
                        .FirstOrDefault(); // pick the nearest neut

                    _minMoveToRange = 43000;
                    _maxMoveToRange = 44000;

                    if (MoveToUsingBestMethod(nearestNeut, _minMoveToRange, _maxMoveToRange))
                        return true;

                    return false;
                }

                if (_moveToOverride != null)
                    _lastMoveToOverride = DateTime.UtcNow;

                if (MoveToThisDirectWorldPosition(_moveToOverride))
                {
                    return true;
                }

                return false;
            }
        }

        private bool DoWeNeedToGetIntoRangeOfKaren
        {
            get
            {
                if (Drones.UseDrones && AreAllDronesAttacking)
                    return false;

                //if (Drones.UseDrones)
                //{
                //    if (Math.Min(ESCache.Instance.ActiveShip.GetDroneControlRange(), ESCache.Instance.ActiveShip.MaxtargetingRange) > 55000)
                //    {
                //        return false;
                //    }
                //}

                if (Combat.PotentialCombatTargets.Count(i => i.BracketType != BracketType.Large_Collidable_Structure) > 1)
                {
                    return false;
                }


                return true;
            }
        }

        /// <summary>
        /// Calculates a spot to stay away from enemies at a given range. This is essentially a 'KeepAtRange' method, except we passively use ongrid pathfinding to move around obstacles
        /// </summary>
        /// <param name="direction">The direction (unit vector) we are using to move away from the entity. Null means we use the direction enemyLocation -> active ship</param>
        /// <param name="targetEntity">The target entity we want to avoid</param>
        /// <param name="minRange">The minRange we want to stay away from the enemy</param>
        /// <param name="maxRange">The maxRange we want to stay away from the enemy</param>
        /// <returns></returns>
        private DirectWorldPosition CalculateMoveToOverrideSpot(Vec3? direction, DirectEntity targetEntity,
            double minRange, double maxRange)
        {
            if (minRange >= maxRange)
            {
                Log($"Error: minRange >= maxRange");
            }

            int abyssalBoundsMax = -3500; // the maximum distance we want to move outside of the abyss bounds
            int unitVecMagnitude = 15000; // magnitude of scaled unit vectors

            var activeShipPos = ESCache.Instance.DirectEve.ActiveShip.Entity.DirectAbsolutePosition;
            var directionTargetEntityTowardsUs = targetEntity.DirectAbsolutePosition
                .GetDirectionalVectorTo(activeShipPos).Normalize()
                .Scale(unitVecMagnitude); // 5k of the unit direction vector enemy -> us

            DirectWorldPosition moveAwayPos = null;

            if (direction.HasValue)
            {
                var dirMagnitude = direction.Value.Scale(unitVecMagnitude);
                moveAwayPos = new DirectWorldPosition(activeShipPos.XCoordinate + dirMagnitude.X,
                    activeShipPos.YCoordinate + dirMagnitude.Y, activeShipPos.ZCoordinate + dirMagnitude.Z);
            }
            else
            {
                moveAwayPos = new DirectWorldPosition(activeShipPos.XCoordinate + directionTargetEntityTowardsUs.X,
                    activeShipPos.YCoordinate + directionTargetEntityTowardsUs.Y,
                    activeShipPos.ZCoordinate + directionTargetEntityTowardsUs.Z);
            }

            if (IsSpotWithinAbyssalBounds(activeShipPos, abyssalBoundsMax)) // if we are within the given boundary
            {
                if (activeShipPos.GetDistance(targetEntity.DirectAbsolutePosition) >=
                    minRange) // ok we're further away than minRange, let's check if we are also within maxRage
                {
                    if (activeShipPos.GetDistance(targetEntity.DirectAbsolutePosition) >=
                        maxRange) // we're further away than maxRange, move towards the enemy
                    {
                        _moveDirection = MoveDirection.TowardsEnemy;
                        return targetEntity.DirectAbsolutePosition;
                    }
                    else // we're between minRange and maxRage, this is fine, but we rather still move away
                    {
                        _moveDirection = MoveDirection.AwayFromEnemy;
                        return moveAwayPos;
                    }
                }
                else // here we are too close to the enemy, let's move away from it
                {
                    _moveDirection = MoveDirection.AwayFromEnemy;
                    return moveAwayPos;
                }
            }
            else // if we are outside of the bounds, move towards the enemy
            {
                _moveDirection = MoveDirection.TowardsEnemy;
                return targetEntity.DirectAbsolutePosition;
            }
        }

        private DirectWorldPosition _safeSpotNearGate { get; set; } = null;
        private bool _safeSpotNearGateChecked = false;
        private DirectWorldPosition _safeSpotForSpeedTankToOrbit { get; set; } = null;
        private DateTime _lastSafeSpotForSpeedTankToOrbit = DateTime.MinValue;
        /// <summary>
        /// If the gate is within an entity we want to avoid we calculate a "safespot" to drop the MTU.
        /// </summary>
        internal DirectWorldPosition SafeSpotForSpeedTankToOrbit
        {
            get
            {
                if (_lastSafeSpotForSpeedTankToOrbit.AddSeconds(3) > DateTime.UtcNow && _lastSafeSpotForSpeedTankToOrbit != null)
                    return _safeSpotForSpeedTankToOrbit;

                _lastSafeSpotForSpeedTankToOrbit = DateTime.UtcNow;

                var nextGate = _nextGate;

                var intersectingEnts = DirectEntity.AnyIntersectionAtThisPosition(nextGate._directEntity.DirectAbsolutePosition,
                    ignoreTrackingPolyons: true, ignoreAutomataPylon: true, ignoreWideAreaAutomataPylon: true);

                if (intersectingEnts.Any())
                {
                    _safeSpotForSpeedTankToOrbit = null;
                    var activeShipPos = ESCache.Instance.DirectEve.ActiveShip.Entity.DirectAbsolutePosition;

                    Vec3 direction = nextGate._directEntity.DirectAbsolutePosition.GetDirectionalVectorTo(activeShipPos).Normalize();

                    var bp = _nextGate._directEntity.DirectAbsolutePosition;
                    List<DirectWorldPosition> positions = new List<DirectWorldPosition>();

                    var diagonalFactor = 1 / Math.Sqrt(3);

                    for (int i = 500; i <= 90500; i += 5000)
                    {

                        var diagDist = diagonalFactor * i;
                        var dir = direction.Scale(i);

                        var directDWP = new DirectWorldPosition(bp.XCoordinate + dir.X, bp.YCoordinate + dir.Y, bp.ZCoordinate + dir.Z);
                        var xp = new DirectWorldPosition(bp.XCoordinate + i, bp.YCoordinate, bp.ZCoordinate);
                        var xn = new DirectWorldPosition(bp.XCoordinate - i, bp.YCoordinate, bp.ZCoordinate);
                        var yp = new DirectWorldPosition(bp.XCoordinate, bp.YCoordinate + i, bp.ZCoordinate);
                        var yn = new DirectWorldPosition(bp.XCoordinate, bp.YCoordinate - i, bp.ZCoordinate);
                        var zp = new DirectWorldPosition(bp.XCoordinate, bp.YCoordinate, bp.ZCoordinate + i);
                        var zn = new DirectWorldPosition(bp.XCoordinate, bp.YCoordinate, bp.ZCoordinate - i);

                        var d1 = new DirectWorldPosition(bp.XCoordinate - diagDist, bp.YCoordinate - diagDist, bp.ZCoordinate - diagDist);
                        var d2 = new DirectWorldPosition(bp.XCoordinate - diagDist, bp.YCoordinate - diagDist, bp.ZCoordinate + diagDist);
                        var d3 = new DirectWorldPosition(bp.XCoordinate - diagDist, bp.YCoordinate + diagDist, bp.ZCoordinate - diagDist);
                        var d4 = new DirectWorldPosition(bp.XCoordinate - diagDist, bp.YCoordinate + diagDist, bp.ZCoordinate + diagDist);
                        var d5 = new DirectWorldPosition(bp.XCoordinate + diagDist, bp.YCoordinate - diagDist, bp.ZCoordinate - diagDist);
                        var d6 = new DirectWorldPosition(bp.XCoordinate + diagDist, bp.YCoordinate - diagDist, bp.ZCoordinate + diagDist);
                        var d7 = new DirectWorldPosition(bp.XCoordinate + diagDist, bp.YCoordinate + diagDist, bp.ZCoordinate - diagDist);
                        var d8 = new DirectWorldPosition(bp.XCoordinate + diagDist, bp.YCoordinate + diagDist, bp.ZCoordinate + diagDist);

                        positions.Add(directDWP);
                        positions.Add(xp);
                        positions.Add(xn);
                        positions.Add(yp);
                        positions.Add(yn);
                        positions.Add(zp);
                        positions.Add(zn);

                        positions.Add(d1);
                        positions.Add(d2);
                        positions.Add(d3);
                        positions.Add(d4);
                        positions.Add(d5);
                        positions.Add(d6);
                        positions.Add(d7);
                        positions.Add(d8);

                        var pos = positions.Where(p =>
                                ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.GetDistanceSquared(p) <=
                                DirectEntity.AbyssBoundarySizeSquared
                                && !DirectEntity.AnyIntersectionAtThisPosition(p, ignoreTrackingPolyons: false,
                                    ignoreAutomataPylon: false, ignoreWideAreaAutomataPylon: false).Any())
                            .OrderBy(e => e.GetDistanceSquared(activeShipPos)).FirstOrDefault();
                        if (pos != null)
                        {
                            _safeSpotForSpeedTankToOrbit = pos;
                            break;
                        }
                    }
                }

                return _safeSpotForSpeedTankToOrbit;
            }
        }

        /// <summary>
        /// If the gate is within an entity we want to avoid we calculate a "safespot" to drop the MTU.
        /// </summary>
        internal DirectWorldPosition SafeSpotNearGate
        {
            get
            {
                if (_safeSpotNearGate != null)
                    return _safeSpotNearGate;

                if (_safeSpotNearGateChecked)
                    return null;

                var nextGate = _nextGate;

                var intersectingEnts = DirectEntity.AnyIntersectionAtThisPosition(nextGate._directEntity.DirectAbsolutePosition,
                    ignoreTrackingPolyons: true, ignoreAutomataPylon: true, ignoreWideAreaAutomataPylon: true);

                DirectWorldPosition backupPos = null;

                if (intersectingEnts.Any() || IsNextGateNearASpeedCloud)
                {
                    _safeSpotNearGate = null;
                    var activeShipPos = ESCache.Instance.DirectEve.ActiveShip.Entity.DirectAbsolutePosition;

                    Vec3 direction = nextGate._directEntity.DirectAbsolutePosition.GetDirectionalVectorTo(activeShipPos).Normalize();

                    var bp = _nextGate._directEntity.DirectAbsolutePosition;
                    List<DirectWorldPosition> positions = new List<DirectWorldPosition>();

                    var diagonalFactor = 1 / Math.Sqrt(3);

                    var speedClouds = Framework.Entities.Where(e => e.IsTachCloud).ToList();

                    for (int i = 500; i <= 90500; i += 5000)
                    {

                        var diagDist = diagonalFactor * i;
                        var dir = direction.Scale(i);

                        var directDWP = new DirectWorldPosition(bp.XCoordinate + dir.X, bp.YCoordinate + dir.Y, bp.ZCoordinate + dir.Z);
                        var xp = new DirectWorldPosition(bp.XCoordinate + i, bp.YCoordinate, bp.ZCoordinate);
                        var xn = new DirectWorldPosition(bp.XCoordinate - i, bp.YCoordinate, bp.ZCoordinate);
                        var yp = new DirectWorldPosition(bp.XCoordinate, bp.YCoordinate + i, bp.ZCoordinate);
                        var yn = new DirectWorldPosition(bp.XCoordinate, bp.YCoordinate - i, bp.ZCoordinate);
                        var zp = new DirectWorldPosition(bp.XCoordinate, bp.YCoordinate, bp.ZCoordinate + i);
                        var zn = new DirectWorldPosition(bp.XCoordinate, bp.YCoordinate, bp.ZCoordinate - i);

                        var d1 = new DirectWorldPosition(bp.XCoordinate - diagDist, bp.YCoordinate - diagDist, bp.ZCoordinate - diagDist);
                        var d2 = new DirectWorldPosition(bp.XCoordinate - diagDist, bp.YCoordinate - diagDist, bp.ZCoordinate + diagDist);
                        var d3 = new DirectWorldPosition(bp.XCoordinate - diagDist, bp.YCoordinate + diagDist, bp.ZCoordinate - diagDist);
                        var d4 = new DirectWorldPosition(bp.XCoordinate - diagDist, bp.YCoordinate + diagDist, bp.ZCoordinate + diagDist);
                        var d5 = new DirectWorldPosition(bp.XCoordinate + diagDist, bp.YCoordinate - diagDist, bp.ZCoordinate - diagDist);
                        var d6 = new DirectWorldPosition(bp.XCoordinate + diagDist, bp.YCoordinate - diagDist, bp.ZCoordinate + diagDist);
                        var d7 = new DirectWorldPosition(bp.XCoordinate + diagDist, bp.YCoordinate + diagDist, bp.ZCoordinate - diagDist);
                        var d8 = new DirectWorldPosition(bp.XCoordinate + diagDist, bp.YCoordinate + diagDist, bp.ZCoordinate + diagDist);

                        positions.Add(directDWP);
                        positions.Add(xp);
                        positions.Add(xn);
                        positions.Add(yp);
                        positions.Add(yn);
                        positions.Add(zp);
                        positions.Add(zn);

                        positions.Add(d1);
                        positions.Add(d2);
                        positions.Add(d3);
                        positions.Add(d4);
                        positions.Add(d5);
                        positions.Add(d6);
                        positions.Add(d7);
                        positions.Add(d8);

                        var pos = positions.Where(p =>
                                ESCache.Instance.AbyssalCenter._directEntity.DirectAbsolutePosition.GetDistanceSquared(p) <=
                                DirectEntity.AbyssBoundarySizeSquared
                                && !DirectEntity.AnyIntersectionAtThisPosition(p, ignoreTrackingPolyons: true,
                                    ignoreAutomataPylon: true, ignoreWideAreaAutomataPylon: true).Any())
                            .OrderBy(e => e.GetDistanceSquared(activeShipPos)).FirstOrDefault();
                        if (pos != null)
                        {
                            if (backupPos == null)
                                backupPos = pos;

                            var continueOuter = false;
                            // Ensure the spot is at least ****k away from a speed cloud
                            foreach (var speedCloud in speedClouds)
                            {
                                var rsq = (speedCloud.RadiusOverride + _speedCloudDistance) * (speedCloud.RadiusOverride + _speedCloudDistance);
                                if (speedCloud.DirectAbsolutePosition.GetDistanceSquared(pos) <= rsq)
                                {
                                    continueOuter = true;
                                    break;
                                }
                            }

                            if (continueOuter)
                                continue;

                            _safeSpotNearGate = pos;
                            break;
                        }
                    }
                }

                if (_safeSpotNearGate == null && backupPos != null)
                {
                    _safeSpotNearGate = backupPos;
                }

                _safeSpotNearGateChecked = true;

                return _safeSpotNearGate;
            }
        }


        /// <summary>
        /// If the gate is within an entity we want to avoid we calculate a "safespot" to drop the MTU.
        /// </summary>


        // approx time we need to get to the gate (we also need to factor in the distance of (nextgate;mtu) in here (if we plan to place the mtu further away from the gate (which we do now))
        internal double _secondsNeededToReachTheGate
        {
            get
            {
                var offset = 5d; // add 5 seconds

                if (ESCache.Instance.ActiveShip.MaxVelocity > 0)
                {
                    if (_getMTUInSpace != null)
                    {
                        // dist: ship -> mtu -> gate
                        var dist = _getMTUInSpace._directEntity.DirectAbsolutePosition.GetDistance(_nextGate._directEntity.DirectAbsolutePosition) +
                                   _getMTUInSpace.Distance;
                        return (dist / ESCache.Instance.ActiveShip.MaxVelocity) + offset;
                    }

                    // dist: ship -> nextgate
                    return (_nextGate.Distance / ESCache.Instance.ActiveShip.MaxVelocity) + offset;
                }

                //
                // estimate a value while we wait for the fitting window to load so we can grab the real value
                //
                if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("_secondsNeededToReachTheGate: using estimated value for a few seconds while we read from the fitting window");
                double estimatedMaxVelocity = 0;
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                {
                    if (ESCache.Instance.ActiveShip.HasSpeedMod)
                    {
                        estimatedMaxVelocity = 630;
                        return (_nextGate.Distance / estimatedMaxVelocity) + offset;
                    }

                    estimatedMaxVelocity = 245;
                    return (_nextGate.Distance / estimatedMaxVelocity) + offset;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                {
                    if (ESCache.Instance.ActiveShip.HasSpeedMod)
                    {
                        estimatedMaxVelocity = 600;
                        return (_nextGate.Distance / estimatedMaxVelocity) + offset;
                    }

                    estimatedMaxVelocity = 220;
                    return (_nextGate.Distance / estimatedMaxVelocity) + offset;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.VexorNavyIssue)
                {
                    if (ESCache.Instance.ActiveShip.HasSpeedMod)
                    {
                        estimatedMaxVelocity = 575;
                        return (_nextGate.Distance / estimatedMaxVelocity) + offset;
                    }

                    estimatedMaxVelocity = 235;
                    return (_nextGate.Distance / estimatedMaxVelocity) + offset;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                {
                    if (ESCache.Instance.ActiveShip.HasSpeedMod)
                    {
                        estimatedMaxVelocity = 600;
                        return (_nextGate.Distance / estimatedMaxVelocity) + offset;
                    }

                    estimatedMaxVelocity = 220;
                    return (_nextGate.Distance / estimatedMaxVelocity) + offset;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Vagabond)
                {
                    if (ESCache.Instance.ActiveShip.HasSpeedMod)
                    {
                        estimatedMaxVelocity = 600;
                        return (_nextGate.Distance / estimatedMaxVelocity) + offset;
                    }

                    estimatedMaxVelocity = 220;
                    return (_nextGate.Distance / estimatedMaxVelocity) + offset;
                }

                if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                {
                    if (ESCache.Instance.ActiveShip.HasSpeedMod)
                    {
                        estimatedMaxVelocity = 1185;
                        return (_nextGate.Distance / estimatedMaxVelocity) + offset;
                    }

                    estimatedMaxVelocity = 400;
                    return (_nextGate.Distance / estimatedMaxVelocity) + offset;
                }

                //
                // this will be a bad estimate if we dont know what ship this is, but its better than nothing
                //
                estimatedMaxVelocity = 200;
                return (_nextGate.Distance / 600) + offset;
            }
        }

        internal bool
            AnyEntityOnGridWeWantToBeCloseWith // We ignore abyss clouds here, that means here should be no spawn which can kill us due cloud downsides
        {
            get
            {
                if (allDronesInSpace.Any() && AreAllDronesAttacking)
                    return false;

                if (Combat.PotentialCombatTargets.Any(i => i.IsTarget && i.Velocity > 0))
                    return false;

                //var rogueDrones = TargetsOnGrid.Where(e => e.GroupId == 1997 && (e.IsNPCBattlecruiser || e.IsNPCCruiser || e.IsNPCFrigate)).Any();

                if (Combat.PotentialCombatTargets.Any(e => e.Name.Contains("kikimora")))
                    return true;

                if (Combat.PotentialCombatTargets.Any(e => e._directEntity.IsAbyssalDroneBattleship))
                    return true;

                //var cynabalDramiel = TargetsOnGrid.Any(e => e.TypeName.ToLower().Contains("cynabal")) || TargetsOnGrid.Any(e => e.TypeName.ToLower().Contains("dramiel"));
                //var ephialtes = TargetsOnGrid.Any(e => e.TypeName.ToLower().Contains("ephialtes") || e.TypeId == 56214); // drifter bs + ephi spawn
                if (Combat.PotentialCombatTargets.Any(e => e._directEntity.IsAbyssalKaren))
                    return true;

                if (Combat.PotentialCombatTargets.Any(e => e._directEntity.IsAbyssalLeshak))
                    return true;

                //if (cynabalDramiel)
                //return true;

                return false;
            }
        }


        internal bool IgnoreAbyssEntities
        {
            get
            {
                if (IsAbyssGateOpen)
                    return true;

                if (!IsOurShipWithintheAbyssBounds())
                    return true;

                if (ESCache.Instance.ActiveShip.IsLocatedWithinSpeedCloud && Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                    return false;

                if (AnyEntityOnGridWeWantToBeCloseWith)
                    return true;

                if (Combat.PotentialCombatTargets.Count(i => i.BracketType != BracketType.Large_Collidable_Structure) <= 3)
                    return true;

                if (DoWeNeedToMoveToTheGate())
                    return true;

                return false;
            }
        }

        internal bool DoWeNeedToMoveToTheGate()
        {
            var timeNeededToGate = _secondsNeededToReachTheGate + _secondsNeededToRetrieveWrecks;
            var timeNeededToClearGrid = GetEstimatedStageRemainingTimeToClearGrid();

            //If we have an MTU in space assume we need to not wander off
            if (_getMTUInSpace != null)
                return false;

            //if (NPCsOnGridThatNeedSpecialTreatment)
            //    return false;

            if (Combat.PotentialCombatTargets.Count(i => i.BracketType != BracketType.Large_Collidable_Structure) <= 5)
            {
                if (Combat.PotentialCombatTargets.Any(e => e.BracketType != BracketType.Large_Collidable_Structure && e.Distance < _maxRange))
                {
                    return true;
                }

                if (ESCache.Instance.Targets.Any(e => e.GroupId != (int)Group.AssaultShip && e.BracketType != BracketType.Large_Collidable_Structure))
                    return true;

                if (allDronesInSpace.All(e => e._directEntity.DroneState == (int)Drones.DroneState.Attacking))
                    return true;
            }

            if (timeNeededToClearGrid != null)
            {
                if (timeNeededToGate > timeNeededToClearGrid + 7)
                {
                    if (DebugConfig.DebugNavigateOnGrid)
                    {
                        Log("DoWeNeedToMoveToTheGate: if (timeNeededToGate [" + Math.Round(timeNeededToGate, 0) + "] > timeNeededToClearGrid  [" + Math.Round((double)timeNeededToClearGrid, 0) + "]) true");
                    }

                    return true;
                }
            }

            var SecondsWeHaveLeftOver = CurrentStageRemainingSecondsWithoutPreviousStages - _secondsNeededToReachTheGate - _secondsNeededToRetrieveWrecks;
            //If negative we need to move to the gate
            if (SecondsWeHaveLeftOver < 0)
            {
                if (DebugConfig.DebugNavigateOnGrid)
                {
                    Log("DoWeNeedToMoveToTheGate: if (CurrentStageRemainingSecondsWithoutPreviousStages [" + Math.Round(CurrentStageRemainingSecondsWithoutPreviousStages, 0) + "] - _secondsNeededToReachTheGate [" + Math.Round(_secondsNeededToReachTheGate,0) + "] - _secondsNeededToRetrieveWrecks [" + Math.Round(_secondsNeededToRetrieveWrecks,0) + "] < 0) true");
                }

                return true;
            }

            //
            // if we get down to 2.5 min run for the gate...
            //
            if (230 > ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds)
                return true;

            return false;
        }


        private DateTime _lastMoveOnGrid { get; set; } = DateTime.MinValue;

        private bool MoveToSafespotNearGate
        {
            get
            {
                //
                // reasons to not...
                //
                if (_singleRoomAbyssal && DebugConfig.DebugFourteenBattleshipSpawnRunAway)
                    return false;

                if (SafeSpotNearGate == null)
                    return false;

                if (_getMTUInSpace != null)
                    return false;

                if (_mtuAlreadyDroppedDuringThisStage)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn)
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn)
                    return false;

                //
                // reasons to move to the safespot near gate
                //
                if (!AnyEntityOnGridWeWantToBeCloseWith && _getMTUInSpace == null && _MTUAvailable && Combat.PotentialCombatTargets.Count(i => i.BracketType != BracketType.Large_Collidable_Structure) >= 8)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (!AnyEntityOnGridWeWantToBeCloseWith && _getMTUInSpace == null && _MTUAvailable && Combat.PotentialCombatTargets.Count() >= 8)");
                    return true;
                }

                if (moveSafespotDronesNotReturningAfterGridCleared)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (moveSafespotDronesNotReturningAfterGridCleared)");
                    return true;
                }

                //var speedCloudOnGateCondition = !_abandoningDrones && IsNextGateInASpeedCloud && allDronesInSpace.Any() && (TargetsOnGridWithoutLootTargets.Any(e => e.Distance < _maxRange) || !TargetsOnGridWithoutLootTargets.Any());

                if (moveSafespotDronesNotReturningWhileGridNotClearedAndWeAreInASpeedCloud)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (moveSafespotDronesNotReturningWhileGridNotClearedAndWeAreInASpeedCloud)");
                    return true;
                }

                if (AreAllDronesAttacking || NoDroneBayAllNPCsInRange)
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (AreAllDronesAttacking || NoDroneBayAllNPCsInRange) && KarybdisTyrannosSpawn");
                        return true;
                    }

                    //if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                    //    return true;
                }

                if (!boolShouldWeBeSpeedTanking && ESCache.Instance.AbyssalDeadspaceTachyonClouds.Any() && ESCache.Instance.AbyssalDeadspaceTachyonClouds.Any(i => 15000 > i.DistanceTo(ESCache.Instance.AbyssalGate)))
                {
                    if (DebugConfig.DebugNavigateOnGrid)
                    {
                         Log("if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.Any() && ESCache.Instance.AbyssalDeadspaceTachyonClouds.Any(i => 15000 > i.DistanceTo(ESCache.Instance.AbyssalGate)))");
                    }

                    return true;
                }

                if (ESCache.Instance.ActiveShip.IsLocatedWithinSpeedCloud && AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn &&
                    Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) &&
                    _secondsNeededToReachTheGate < CurrentStageRemainingSecondsWithoutPreviousStages &&
                    (Combat.PotentialCombatTargets.Where(i => i.BracketType != BracketType.Large_Collidable_Structure).All(t => t.Distance < _maxRange)))
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("if (AreWeInASpeedCloud && Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) && _secondsNeededToReachTheGate < CurrentStageRemainingSecondsWithoutPreviousStages &&     (Combat.PotentialCombatTargets.Where(i => i.Velocity > 0).All(t => t.Distance < _maxRange)))");
                    return true;
                }

                return false;
            }
        }

        private bool moveSafespotDronesNotReturningAfterGridCleared
        {
            get
            {
                if (!allDronesInSpace.Any())
                {
                    if (DebugConfig.DebugDrones) Log("if (!allDronesInSpace.Any())");
                    return false;
                }

                if (!IsAbyssGateOpen) // only if abyss gate open
                {
                    if (DebugConfig.DebugDrones) Log("if (!IsAbyssGateOpen)");
                    return false;
                }

                if (_startedToRecallDronesWhileNoTargetsLeft == null) // only while we are recalling
                {
                    if (DebugConfig.DebugDrones) Log("if (_startedToRecallDronesWhileNoTargetsLeft == null)");
                    return false;
                }

                if (15 > (DateTime.UtcNow - _startedToRecallDronesWhileNoTargetsLeft.Value).TotalSeconds) // if we are recalling drones for longer than 15 seconds that is a problem
                {
                    if (DebugConfig.DebugDrones) Log("if (15 > (DateTime.UtcNow - _startedToRecallDronesWhileNoTargetsLeft.Value).TotalSeconds)");
                    return false;
                }

                if (15 > CurrentStageRemainingSecondsWithoutPreviousStages) // if the current stage has at least 25 seconds left
                {
                    if (DebugConfig.DebugDrones) Log("if (15 > CurrentStageRemainingSecondsWithoutPreviousStages)");
                    return false;
                }

                return true;
            }
        }

        private bool moveSafespotDronesNotReturningWhileGridNotClearedAndWeAreInASpeedCloud

        {
            get
            {
                if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                {
                    if (DebugConfig.DebugDrones) Log("if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))");
                    return false;
                }

                if (!ESCache.Instance.ActiveShip.IsLocatedWithinSpeedCloud)
                {
                    if (DebugConfig.DebugDrones) Log("if (!AreWeInASpeedCloud)");
                    return false;
                }

                if (!allDronesInSpace.Any(e => DroneReturningSinceSeconds(e.Id) > 25)) // drone control range is around 80k and slowest drones (gecko) is around 2.8k
                {
                    if (DebugConfig.DebugDrones) Log("if (!allDronesInSpace.Any(e => DroneReturningSinceSeconds(e.Id) > 25))");
                    return false;
                }

                return true;
            }
        }


        private static DirectWorldPosition _lastNormalDirectWorldPosition;

        private static void ResetCurrentXYZCoord()
        {
            _lastNormalDirectWorldPosition = null;
        }

        private static void SetHereToCurrentXYZCoord()
        {
            Log("SetHereToCurrentXYZCoord X [" + ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition.XCoordinate + "] Y [" + ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition.YCoordinate + "] Z [" + ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition.ZCoordinate + "]");
            _lastNormalDirectWorldPosition = ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition;
        }

        internal void MoveOnGrid()
        {
            if (!DirectEve.HasFrameChanged())
                return;

            _lastMoveOnGrid = DateTime.UtcNow;

            if (DirectSceneManager.LastRedrawSceneColliders.AddSeconds(5) < DateTime.UtcNow)
                ESCache.Instance.DirectEve.SceneManager.RedrawSceneColliders(ignoreAbyssEntities: IgnoreAbyssEntities,
                    ignoreTrackingPolyons: true, ignoreAutomataPylon: true, ignoreWideAreaAutomataPylon: true);

            if (HandleTasksOnceRatsAreDead()) return;

            if (!boolShouldWeBeSpeedTanking && KeepMeSafelyInsideTheArenaAndMoving()) return;

            if (_lastNormalDirectWorldPosition != null && ESCache.Instance.DistanceFromMe(_lastNormalDirectWorldPosition.PositionInSpace) > (double)Distances.MaxPocketsDistanceKm)
            {
                Log("CurrentAbyssalStage [" + CurrentAbyssalStage + "]");
                //DirectSession.SetSessionNextSessionReady(7000, 9000);
                //Log("AccelerationGate with ID [" + ESCache.Instance.OldAccelerationGateId + "] no longer found: we must be in the next pocket!");
                //ESCache.Instance.OldAccelerationGateId = null;
                //ESCache.Instance.TaskSetEveAccountAttribute(nameof(EveAccount.LastInteractedWithEVE), DateTime.UtcNow);

                ESCache.Instance.ClearPerPocketCache();
                Time.Instance.LastInitiatedWarp = DateTime.UtcNow;
                Time.Instance.NextActivateAccelerationGate = DateTime.UtcNow.AddSeconds(13);

                //DirectEventManager.NewEvent(new DirectEvent(DirectEvents.KEEP_ALIVE, "Gate Activated"));
                SetHereToCurrentXYZCoord();
                //Log("Change state to 'NextPocket' LostDrones [" + Drones.LostDrones + "] AllDronesInSpaceCount [" + Drones.AllDronesInSpaceCount + "]");
                //ChangeCombatMissionCtrlState(ActionControlState.NextPocket, myMission, myAgent);
                return;
            }

            //wait if we are at the starting spot
            if (20 > Time.Instance.SecondsSinceLastSessionChange && 350 > ESCache.Instance.ActiveShip.Entity.Velocity && 10000 > _nextGate.Distance)
            {
                if (DirectEve.Modules.Any(i => i.GroupId == (int)Group.ShieldHardeners))
                {
                    if (!DirectEve.Modules.FirstOrDefault(i => i.GroupId == (int)Group.ShieldHardeners).IsActive ||
                         DirectEve.Modules.FirstOrDefault(i => i.GroupId == (int)Group.ShieldHardeners).IsInLimboState)
                    {
                        Log("Waiting for Shield Hardener to be active before we start moving");
                        return;
                    }
                }

                if (DirectEve.Modules.Any(i => i.GroupId == (int)Group.ShieldBoosters))
                {
                    if (!DirectEve.Modules.FirstOrDefault(i => i.GroupId == (int)Group.ShieldBoosters).IsActive ||
                         DirectEve.Modules.FirstOrDefault(i => i.GroupId == (int)Group.ShieldBoosters).IsInLimboState)
                    {
                        Log("Waiting for Shield ShieldBooster to be active before we start moving");
                        return;
                    }
                }
            }

            if (allDronesInSpace.Any() && allDronesInSpace.All(i => i._directEntity.DroneState == (int)Drones.DroneState.Returning || i._directEntity.DroneState == (int)Drones.DroneState.Returning2))
            {
                var dronesReturningForLongerThanReasonable = allDronesInSpace.Where(e => DroneReturningSinceSeconds(e.Id) > 10);

                if (dronesReturningForLongerThanReasonable.Any() && DirectEve.Interval(3000))
                {
                    foreach (var drone in dronesReturningForLongerThanReasonable)
                    {
                        Log($"--- Drone [{drone.TypeName}]@[{drone.Nearest1KDistance}k][{Math.Round(drone.Velocity, 0)}m/s] DroneState [{drone._directEntity.DroneStateName}] Id {drone.MaskedId} has been recalling for [" + DroneReturningSinceSeconds(drone.Id) + "] sec: this is probably bad!");
                    }

                    if (DirectEve.Interval(15000) && dronesReturningForLongerThanReasonable.Any(i => (i._directEntity.IsInSpeedCloud || i.Velocity > 4000) && i.Distance > 10000))
                    {
                        var tempdrone = dronesReturningForLongerThanReasonable.FirstOrDefault(i => i.Velocity > 4000);
                        Log("Drone [" + tempdrone.TypeName + "][" + tempdrone.MaskedId + "] was going [" + Math.Round(tempdrone.Velocity, 0) + "] m/s which is > 3000 m/s: CmdDronesReturnAndOrbit");
                        DirectEve.ExecuteCommand(DirectCmd.CmdDronesReturnAndOrbit);
                    }
                    else DirectEve.ExecuteCommand(DirectCmd.CmdDronesReturnToBay);
                }
            }
            /**
            else if (allDronesInSpace.Any())
            {
                var dronesReturningForLongerThanReasonable = allDronesInSpace.Where(e => DroneReturningSinceSeconds(e.Id) > 25);

                if (dronesReturningForLongerThanReasonable.Any() && DirectEve.Interval(3000))
                {
                    foreach (var drone in dronesReturningForLongerThanReasonable)
                    {
                        Log($"--- Drone [{drone.TypeName}]@[{drone.Nearest1KDistance}k][{Math.Round(drone.Velocity, 0)}m/s] DroneState [{drone._directEntity.DroneStateName}] Id {drone.MaskedId} has been recalling for [" + DroneReturningSinceSeconds(drone.Id) + "] sec: this is probably bad!!");
                    }

                    if (DirectEve.Interval(20000))
                    {
                        DirectEve.ExecuteCommand(DirectCmd.CmdDronesReturnAndOrbit);
                    }

                    if (DirectEve.Interval(4000))
                    {
                        DirectEve.ExecuteCommand(DirectCmd.CmdDronesReturnToBay);
                    }
                }
            }
            **/

            if (boolShouldWeBeSpeedTanking)
            {
                AbyssalSpeedTank();
                return;
            }


            if (MoveToOverride)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("MoveToOverride [ true ]");
                return;
            }
            else if (DebugConfig.DebugNavigateOnGrid) Log("MoveToOverride [ false ] IsItSafeToMoveToMTUOrLootWrecks: [" + IsItSafeToMoveToMTUOrLootWrecks + "] ");

            if (SafeSpotNearGate == null && DirectEve.Interval(5000))
            {
                Log($"Warn: SafeSpotNearGate is null.");
            }

            // move to safe spot near gate
            if (MoveToSafespotNearGate)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("MoveToSafespotNearGate [true]");
                // okay if we didn't drop a mtu yet, but we do have calculated a safespot. we move there and drop the mtu
                if (DirectEve.Interval(5000))
                {
                    if (allDronesInSpace.Any())
                    {
                        if (moveSafespotDronesNotReturningAfterGridCleared)
                            Log(
                                $"We are moving to the next best calculated safespot near the gate because drones are not returning after the grid has been cleared.");
                        else if (moveSafespotDronesNotReturningWhileGridNotClearedAndWeAreInASpeedCloud)
                            Log(
                                $"We are moving to the next best calculated safespot near the gate because drones are not returning while the grid is being cleared and we are in a speed cloud.");
                        else
                            Log($"We are moving to the next best calculated safespot near the gate.");
                    }

                }

                try
                {
                    if (DebugConfig.DebugNavigateOnGrid)
                    {
                        Log("MoveToViaAStar: forceRecreatePath: [" + forceRecreatePath + "] dest: moveToTarget._directEntity.DirectAbsolutePosition");
                        Log("ignoreAbyssEntities:[ " + IgnoreAbyssEntities + "] " +
                            "ignoreTrackingPolyons: [" + ESCache.Instance.IgnoreTrackingPolyons_ImprovesTracking_HelpsToHitSmallThings + "] " +
                            "ignoreAutomataPylon: [" + ESCache.Instance.IgnoreAutomataPylon_Kills_NPCDrones_OurDrones_And_Missiles + "] " +
                            "ignoreWideAreaAutomataPylon: true," +
                            "ignoreBioClouds: [" + ESCache.Instance.IgnoreBioClouds_Blue_4xSignatureRadius + "] " +
                            "ignoreFilaCouds: [" + ESCache.Instance.IgnoreFilaClouds_Orange_ShieldBoostCapacitorUsePenalty + "] " +
                            "ignoreTachClouds: [" + ESCache.Instance.IgnoreTachClouds_White_4xSpeedBoost + "] "); ;
                    }
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                }


                DirectEntity.MoveToViaAStar(2000, distanceToTarget: 0, forceRecreatePath: forceRecreatePath, dest: SafeSpotNearGate,
                        ignoreAbyssEntities: IgnoreAbyssEntities,
                        ignoreTrackingPolyons: ESCache.Instance.IgnoreTrackingPolyons_ImprovesTracking_HelpsToHitSmallThings,
                        ignoreAutomataPylon: ESCache.Instance.IgnoreAutomataPylon_Kills_NPCDrones_OurDrones_And_Missiles,
                        ignoreWideAreaAutomataPylon: true,
                        ignoreBioClouds: ESCache.Instance.IgnoreBioClouds_Blue_4xSignatureRadius,
                        ignoreFilaCouds: ESCache.Instance.IgnoreFilaClouds_Orange_ShieldBoostCapacitorUsePenalty,
                        ignoreTachClouds: ESCache.Instance.IgnoreTachClouds_White_4xSpeedBoost);

                if (DebugConfig.DebugNavigateOnGrid) Log("MoveToViaAStar returned true: ESCache.Instance.DirectEve.ActiveShip.MoveTo(SafeSpotNearGate);");
                return;
            }
            else if (DebugConfig.DebugNavigateOnGrid) Log("MoveToSafespotNearGate [false]");

            try
            {
                if (DebugConfig.DebugNavigateOnGrid && moveToTargetEntity != null)
                {
                    Log("MoveToViaAStar: forceRecreatePath: [" + forceRecreatePath + "] dest: moveToTarget._directEntity.DirectAbsolutePosition, destinationEntity: [" + moveToTargetEntity.TypeName + "]");
                    Log("ignoreAbyssEntities:[ " + IgnoreAbyssEntities + "] " +
                        "ignoreTrackingPolyons: [" + ESCache.Instance.IgnoreTrackingPolyons_ImprovesTracking_HelpsToHitSmallThings + "] " +
                        "ignoreAutomataPylon: [" + ESCache.Instance.IgnoreAutomataPylon_Kills_NPCDrones_OurDrones_And_Missiles + "] " +
                        "ignoreWideAreaAutomataPylon: true," +
                        "ignoreBioClouds: [" + ESCache.Instance.IgnoreBioClouds_Blue_4xSignatureRadius + "] " +
                        "ignoreFilaCouds: [" + ESCache.Instance.IgnoreFilaClouds_Orange_ShieldBoostCapacitorUsePenalty + "] " +
                        "ignoreTachClouds: [" + ESCache.Instance.IgnoreTachClouds_White_4xSpeedBoost + "] "); ;
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }

            if (DirectEntity.MoveToViaAStar(stepSize: 2000, distanceToTarget: 12500,
                forceRecreatePath: forceRecreatePath,
                dest: moveToTarget,
                //destinationEntity: moveToTarget._directEntity,
                ignoreAbyssEntities: IgnoreAbyssEntities,
                ignoreTrackingPolyons: ESCache.Instance.IgnoreTrackingPolyons_ImprovesTracking_HelpsToHitSmallThings,
                ignoreAutomataPylon: ESCache.Instance.IgnoreAutomataPylon_Kills_NPCDrones_OurDrones_And_Missiles,
                ignoreWideAreaAutomataPylon: true,
                ignoreBioClouds: ESCache.Instance.IgnoreBioClouds_Blue_4xSignatureRadius,
                ignoreFilaCouds: ESCache.Instance.IgnoreFilaClouds_Orange_ShieldBoostCapacitorUsePenalty,
                ignoreTachClouds: ESCache.Instance.IgnoreTachClouds_White_4xSpeedBoost))
            {
                //returned true: we are within 12.5k
                if (IsItSafeToKeepAtRange)
                {
                    if (DebugConfig.DebugNavigateOnGrid) Log("IsItSafeToKeepAtRange");
                    if (moveToTargetEntity != null && !moveToTargetEntity._directEntity.IsApproachedOrKeptAtRangeByActiveShip)
                    {
                        if (moveToTargetEntity.IsContainer || (moveToTargetEntity.IsMobileTractor && !Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure)))
                        {
                            Log("IsItSafeToKeepAtRange: Approach [" + moveToTargetEntity.TypeName + "][" + Math.Round(moveToTargetEntity.Distance / 1000, 0) + " k][" + moveToTargetEntity.Id + "]");
                            moveToTargetEntity.Approach();
                            return;
                        }

                        //Log("Keep at range [" + moveToTarget.TypeName + "][" + Math.Round(moveToTarget.Distance/1000, 0) + " k][" + moveToTarget.Id + "]");
                        //moveToTarget.KeepAtRange(_keepAtRangeDistance);
                        Log("IsItSafeToKeepAtRange: Orbit [" + moveToTargetEntity.TypeName + "][" + Math.Round(moveToTargetEntity.Distance / 1000, 0) + " k][" + moveToTargetEntity.Id + "]");
                        moveToTargetEntity.Orbit(250);
                        return;
                    }
                }
                else if (_getMTUInSpace != null && 5000 > _getMTUInSpace.Distance && ShouldWeScoopMTU)
                {
                    Log($"Approach MTU [{_getMTUInSpace.TypeName}]@[{_getMTUInSpace.Nearest1KDistance}]");
                    _getMTUInSpace.Approach();
                    return;
                }
                else if (moveToTargetEntity != null && !moveToTargetEntity.IsOrbitedByActiveShip)
                {
                    if (moveToTargetEntity.IsContainer)
                    {
                        Log("Orbit container [" + moveToTargetEntity.TypeName + "][" + Math.Round(moveToTargetEntity.Distance / 1000, 0) + " k]");
                        moveToTargetEntity.Orbit(500);
                        return;
                    }

                    if (DirectEve.Interval(5000, 5000, moveToTargetEntity.Id.ToString()))
                    {
                        Log($"Orbiting [{moveToTargetEntity.TypeName}]@[{moveToTargetEntity.Nearest1KDistance}] OrbitDist [{orbitDist}]");
                        moveToTargetEntity.Orbit(orbitDist);
                        return;
                    }
                }
            }

        }

        private int orbitDist
        {
            get
            {
                try
                {
                    if (moveToTargetEntity != null && _getMTUInSpace != null && moveToTargetEntity.Id == _getMTUInSpace.Id)
                    {
                        if (_getMTUInSpace != null)
                            return _gateMTUOrbitDistance; //1500
                    }

                    if (moveToTargetEntity != null && _nextGate != null && moveToTargetEntity.Id == _nextGate.Id)
                    {
                        if (_nextGate != null)
                            return _gateMTUOrbitDistance; //1500
                    }

                    return _enemyOrbitDistance; //7000
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return 7000;
                }
            }
        }

        private bool IsItSafeToKeepAtRange
        {
            get
            {
                try
                {
                    if (moveToTargetEntity == null)
                        return false;

                    if (_singleRoomAbyssal && DebugConfig.DebugFourteenBattleshipSpawnRunAway)
                        return false;

                    // don't keep at range when there is a bs on grid and move to target is the gate
                    if (Combat.PotentialCombatTargets.Any(e => e.IsNPCBattleship))
                        return false;

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn && Combat.PotentialCombatTargets.Count(e => e.IsNPCCruiser) >= 5)
                        return false;

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn && Combat.PotentialCombatTargets.Count(e => e.IsNPCCruiser) >= 5)
                        return false;

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn && Combat.PotentialCombatTargets.Count(e => e.IsNPCBattleship) >= 1)
                        return false;

                    if (moveToTargetEntity != null && moveToTargetEntity.Id == _nextGate.Id)
                    {
                        if (1 >= Combat.PotentialCombatTargets.Count(i => i.BracketType != BracketType.Large_Collidable_Structure))
                        {
                            if (5000 > _nextGate.Distance)
                                return true;
                        }
                    }

                    if (_getMTUInSpace != null && moveToTargetEntity != null && _getMTUInSpace.Id == moveToTargetEntity.Id)
                    {
                        //Lets just orbit it... safer?
                        //return true;
                    }

                    if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                    {
                        _keepAtRangeDistance = 1000;
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        private EntityCache _higherPriorityTarget = null;

        private EntityCache HigherPriorityTarget
        {
            get
            {
                if (!DirectEve.HasFrameChanged())
                {
                    if (_higherPriorityTarget != null)
                        return _higherPriorityTarget;
                }
                else _higherPriorityTarget = null;

                var highestTargeted = ESCache.Instance.TotalTargetsAndTargeting.Where(i => i.BracketType != BracketType.Large_Collidable_Structure).OrderByDescending(e => e._directEntity.AbyssalTargetPriority)
                    .FirstOrDefault();
                if (highestTargeted != null)
                {
                    // get the lowest on grid
                    var lowestOnGrid = Combat.PotentialCombatTargets.Where(e => e.BracketType != BracketType.Large_Collidable_Structure && !e.IsTarget && !e.IsTargeting)
                        .OrderBy(e => e._directEntity.AbyssalTargetPriority).FirstOrDefault();
                    if (lowestOnGrid != null)
                    {
                        if (lowestOnGrid._directEntity.AbyssalTargetPriority < highestTargeted._directEntity.AbyssalTargetPriority)
                        {
                            if (!lowestOnGrid.IsReadyToTarget || !lowestOnGrid.IsReadyToTarget)
                            {
                                if (DirectEve.Interval(2000, 3000))
                                {
                                    Log($"Higher priority: Out of range? [{lowestOnGrid.TypeName}] TypeId [{lowestOnGrid.TypeId}] Distance [{lowestOnGrid.Distance}]");
                                }

                                _higherPriorityTarget = lowestOnGrid;
                                return _higherPriorityTarget;
                            }

                            _higherPriorityTarget = null;
                            return _higherPriorityTarget;
                        }

                        _higherPriorityTarget = null;
                        return _higherPriorityTarget;
                    }

                    _higherPriorityTarget = null;
                    return _higherPriorityTarget;
                }

                _higherPriorityTarget = null;
                return _higherPriorityTarget;
            }
        }

        private EntityCache? __moveToTarget_MTU_Wreck_Can_or_Gate_Entity = null;
        private EntityCache _moveToTarget_MTU_Wreck_Can_or_Gate_Entity
        {
            get
            {
                try
                {
                    if (!DirectEve.HasFrameChanged())
                    {
                        if (__moveToTarget_MTU_Wreck_Can_or_Gate_Entity != null)
                        {
                            return __moveToTarget_MTU_Wreck_Can_or_Gate_Entity;
                        }
                    }
                    else __moveToTarget_MTU_Wreck_Can_or_Gate_Entity = null;

                    if (_getMTUInSpace != null)
                    {
                        __moveToTarget_MTU_Wreck_Can_or_Gate_Entity = _getMTUInSpace;
                        _moveToTargetEntity = _getMTUInSpace;
                        return __moveToTarget_MTU_Wreck_Can_or_Gate_Entity;
                    }

                    if (_getMTUInSpace == null && _getMTUInBay == null && ESCache.Instance.Wrecks.Any(i => 35000 > i.DistanceTo(_nextGate) && !i.IsWreckEmpty))
                    {
                        __moveToTarget_MTU_Wreck_Can_or_Gate_Entity = ESCache.Instance.Wrecks.FirstOrDefault(i => 35000 > i.DistanceTo(_nextGate) && !i.IsWreckEmpty);
                        return __moveToTarget_MTU_Wreck_Can_or_Gate_Entity;
                    }

                    if (_getMTUInBay == null && _getMTUInSpace == null)
                    {
                        if (ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                        {
                            __moveToTarget_MTU_Wreck_Can_or_Gate_Entity = ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty);
                            return __moveToTarget_MTU_Wreck_Can_or_Gate_Entity;
                        }

                        if (ESCache.Instance.Wrecks.Any() && Combat.PotentialCombatTargets.Any())
                        {
                            if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.Any())
                            {
                                if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.Any(i => 5000 > i.DistanceTo(_nextGate)))
                                {
                                    //Is SafeSpotNearGate in a speed cloud? if not use that spot
                                    //if (!boolShouldWeBeSpeedTanking && ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(SafeSpotNearGate) > 0))
                                    //{
                                    //    __moveToTarget_MTU_Wreck_Can_or_Gate = SafeSpotNearGate;
                                    //    return __moveToTarget_MTU_Wreck_Can_or_Gate_Entity;
                                    //}

                                    //Is the wreck from the cache in a speed cloud? if not use that spot
                                    if (Combat.PotentialCombatTargets.Any(i => i.IsAbyssalBioAdaptiveCache))
                                    {
                                        if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache)) > 0))
                                        {
                                            __moveToTarget_MTU_Wreck_Can_or_Gate_Entity = Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache);
                                            return __moveToTarget_MTU_Wreck_Can_or_Gate_Entity;
                                        }
                                    }

                                    //Is the wreck from the cache in a speed cloud? if not use that spot
                                    if (ESCache.Instance.Wrecks.Any())
                                    {
                                        if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(ESCache.Instance.Wrecks.FirstOrDefault()) > 0))
                                        {
                                            __moveToTarget_MTU_Wreck_Can_or_Gate_Entity = ESCache.Instance.Wrecks.FirstOrDefault();
                                            return __moveToTarget_MTU_Wreck_Can_or_Gate_Entity;
                                        }
                                    }

                                    //Is there an MTU that is not in a speed cloud? if not use that spot
                                    if (_getMTUInSpace != null)
                                    {
                                        if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(_getMTUInSpace) > 0))
                                        {
                                            __moveToTarget_MTU_Wreck_Can_or_Gate_Entity = _getMTUInSpace;
                                            return __moveToTarget_MTU_Wreck_Can_or_Gate_Entity;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (ESCache.Instance.Containers.Any(i => !i.IsWreck))
                    {
                        __moveToTarget_MTU_Wreck_Can_or_Gate_Entity = ESCache.Instance.Containers.FirstOrDefault(i => !i.IsWreck);
                        return __moveToTarget_MTU_Wreck_Can_or_Gate_Entity;
                    }

                    __moveToTarget_MTU_Wreck_Can_or_Gate_Entity = _nextGate;
                    return __moveToTarget_MTU_Wreck_Can_or_Gate_Entity;
                }
                catch (Exception ex)
                {
                    Log("Exception[" + ex + "]");
                    __moveToTarget_MTU_Wreck_Can_or_Gate_Entity = _nextGate;
                    return __moveToTarget_MTU_Wreck_Can_or_Gate_Entity;
                }
            }
        }

        private DirectWorldPosition? __moveToTarget_MTU_Wreck_Can_or_Gate = null;
        private DirectWorldPosition _moveToTarget_MTU_Wreck_Can_or_Gate
        {
            get
            {
                try
                {
                    if (!DirectEve.HasFrameChanged())
                    {
                        if (__moveToTarget_MTU_Wreck_Can_or_Gate != null)
                        {
                            return __moveToTarget_MTU_Wreck_Can_or_Gate;
                        }
                    }
                    else __moveToTarget_MTU_Wreck_Can_or_Gate = null;

                    if (_getMTUInSpace != null && IsItSafeToMoveToMTUOrLootWrecks)
                    {
                        __moveToTarget_MTU_Wreck_Can_or_Gate = _getMTUInSpace._directEntity.DirectAbsolutePosition;
                        _moveToTargetEntity = _getMTUInSpace;
                        return __moveToTarget_MTU_Wreck_Can_or_Gate;
                    }

                    if (_getMTUInSpace == null && _getMTUInBay == null && ESCache.Instance.Wrecks.Any(i => 35000 > i.DistanceTo(_nextGate) && !i.IsWreckEmpty))
                    {
                        __moveToTarget_MTU_Wreck_Can_or_Gate = ESCache.Instance.Wrecks.FirstOrDefault(i => 35000 > i.DistanceTo(_nextGate) && !i.IsWreckEmpty)._directEntity.DirectAbsolutePosition;
                        _moveToTargetEntity = ESCache.Instance.Wrecks.FirstOrDefault(i => 35000 > i.DistanceTo(_nextGate) && !i.IsWreckEmpty);
                        return __moveToTarget_MTU_Wreck_Can_or_Gate;
                    }

                    if (_getMTUInBay == null && _getMTUInSpace == null)
                    {
                        if (ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                        {
                            __moveToTarget_MTU_Wreck_Can_or_Gate = ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty)._directEntity.DirectAbsolutePosition;
                            _moveToTargetEntity = ESCache.Instance.Wrecks.FirstOrDefault(i => !i.IsWreckEmpty);
                            return __moveToTarget_MTU_Wreck_Can_or_Gate;
                        }

                        if (ESCache.Instance.Wrecks.Any() && Combat.PotentialCombatTargets.Any())
                        {
                            if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.Any())
                            {
                                if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.Any(i => 5000 > i.DistanceTo(_nextGate)))
                                {
                                    //Is SafeSpotNearGate in a speed cloud? if not use that spot
                                    if (!boolShouldWeBeSpeedTanking && ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(SafeSpotNearGate) > 0))
                                    {
                                        __moveToTarget_MTU_Wreck_Can_or_Gate = SafeSpotNearGate;
                                        return __moveToTarget_MTU_Wreck_Can_or_Gate;
                                    }

                                    //Is the wreck from the cache in a speed cloud? if not use that spot
                                    if (Combat.PotentialCombatTargets.Any(i => i.IsAbyssalBioAdaptiveCache))
                                    {
                                        if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache)) > 0))
                                        {
                                            __moveToTarget_MTU_Wreck_Can_or_Gate = Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache)._directEntity.DirectAbsolutePosition;
                                            _moveToTargetEntity = Combat.PotentialCombatTargets.FirstOrDefault(i => i.IsAbyssalBioAdaptiveCache);
                                            return __moveToTarget_MTU_Wreck_Can_or_Gate;
                                        }
                                    }

                                    //Is the wreck from the cache in a speed cloud? if not use that spot
                                    if (ESCache.Instance.Wrecks.Any())
                                    {
                                        if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(ESCache.Instance.Wrecks.FirstOrDefault()) > 0))
                                        {
                                            __moveToTarget_MTU_Wreck_Can_or_Gate = ESCache.Instance.Wrecks.FirstOrDefault()._directEntity.DirectAbsolutePosition;
                                            _moveToTargetEntity = ESCache.Instance.Wrecks.FirstOrDefault();
                                            return __moveToTarget_MTU_Wreck_Can_or_Gate;
                                        }
                                    }

                                    //Is there an MTU that is not in a speed cloud? if not use that spot
                                    if (_getMTUInSpace != null)
                                    {
                                        if (ESCache.Instance.AbyssalDeadspaceTachyonClouds.All(i => i.DistanceTo(_getMTUInSpace) > 0))
                                        {
                                            __moveToTarget_MTU_Wreck_Can_or_Gate = _getMTUInSpace._directEntity.DirectAbsolutePosition;
                                            _moveToTargetEntity = _getMTUInSpace;
                                            return __moveToTarget_MTU_Wreck_Can_or_Gate;
                                        }
                                    }
                                }
                            }
                        }
                    }

                    if (ESCache.Instance.Containers.Any(i => !i.IsWreck))
                    {
                        __moveToTarget_MTU_Wreck_Can_or_Gate = ESCache.Instance.Containers.FirstOrDefault(i => !i.IsWreck)._directEntity.DirectAbsolutePosition;
                        _moveToTargetEntity = ESCache.Instance.Containers.FirstOrDefault(i => !i.IsWreck);
                        return __moveToTarget_MTU_Wreck_Can_or_Gate;
                    }

                    __moveToTarget_MTU_Wreck_Can_or_Gate = _nextGate._directEntity.DirectAbsolutePosition;
                    _moveToTargetEntity = _nextGate;
                    return __moveToTarget_MTU_Wreck_Can_or_Gate;
                }
                catch (Exception ex)
                {
                    Log("Exception[" + ex + "]");
                    __moveToTarget_MTU_Wreck_Can_or_Gate = _nextGate._directEntity.DirectAbsolutePosition;
                    return __moveToTarget_MTU_Wreck_Can_or_Gate;
                }
            }
        }

        public EntityCache? _moveToTargetEntity = null;

        public EntityCache moveToTargetEntity
        {
            get
            {
                if (_moveToTargetEntity != null)
                    return _moveToTargetEntity;

                return null;
            }
        }

        public DirectWorldPosition? _moveToTarget = null;
        public DirectWorldPosition moveToTarget
        {
            get
            {
                try
                {
                    if (!DirectEve.HasFrameChanged())
                    {
                        if (_moveToTarget != null)
                            return _moveToTarget;
                    }
                    else
                    {
                        _moveToTarget = null;
                        _moveToTargetEntity = null;
                    }

                    if (.25 > ESCache.Instance.ActiveShip.Entity.SpeedFraction)
                    {
                        if (DebugConfig.DebugNavigateOnGrid) Log("if (.25 > SpeedFraction): CmdSetShipFullSpeed");
                        DirectEve.ExecuteCommand(DirectCmd.CmdSetShipFullSpeed);
                    }

                    if (_moveToTarget == null)
                    {
                        _moveToTarget = _moveToTarget_MTU_Wreck_Can_or_Gate;
                    }

                    if (_singleRoomAbyssal && DebugConfig.DebugFourteenBattleshipSpawnRunAway)
                    {
                        Log($"This is a single room abyss, moving to the next gate.");
                        _moveToTarget = _nextGate._directEntity.DirectAbsolutePosition;
                        _moveToTargetEntity = _nextGate;
                    }
                    //scoop the mtu
                    else if (MtuWillBeReadyByTheTimeWeGetThere)
                    {
                        _moveToTarget = _getMTUInSpace._directEntity.DirectAbsolutePosition;
                        _moveToTargetEntity = _getMTUInSpace;
                    }
                    // move to the gate/mtu if we need to due time restrictions
                    else if (IsItSafeToMoveToMTUOrLootWrecks)
                    {
                        _moveToTarget = _moveToTarget_MTU_Wreck_Can_or_Gate;

                        if (DirectEve.Interval(5000, 7000, _moveToTarget.XCoordinate.ToString() + _moveToTarget.YCoordinate.ToString() + _moveToTarget.ZCoordinate.ToString()))
                        {
                            var mtuDistane = _getMTUInSpace != null ? _getMTUInSpace.Distance : -1;
                            var mtuGateDistance = _getMTUInSpace != null
                                ? _getMTUInSpace._directEntity.DirectAbsolutePosition.GetDistance(_nextGate._directEntity.DirectAbsolutePosition)
                                : -1;
                            Log($"moveToTarget, IsItSafeToMoveToMTU: CurrentSpeed [{Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 2)}] CurrentStageRemainingSecondsWithoutPreviousStages [{Math.Round(CurrentStageRemainingSecondsWithoutPreviousStages, 0)}] CurrentStageRemainingSeconds [{Math.Round(CurrentStageRemainingSeconds, 0)}] Stage [{CurrentAbyssalStage}] SecondsNeededToReachTheGate [{Math.Round(_secondsNeededToReachTheGate, 0)}] DistanceToGate {Math.Round(_nextGate.Distance, 0)} MTUDistance {Math.Round(mtuDistane, 0)} MTUGateDist [{Math.Round(mtuGateDistance, 0)}]");
                        }
                    }
                    else if (DoWeNeedToMoveToTheGate())
                    {
                        _moveToTarget = _moveToTarget_MTU_Wreck_Can_or_Gate;
                        if (DirectEve.Interval(5000, 7000))
                        {
                            var mtuDistane = _getMTUInSpace != null ? _getMTUInSpace.Distance : -1;
                            var mtuGateDistance = _getMTUInSpace != null
                                ? _getMTUInSpace._directEntity.DirectAbsolutePosition.GetDistance(_nextGate._directEntity.DirectAbsolutePosition)
                                : -1;
                            Log($"moveToTarget: DoWeNeedToMoveToTheGate [true]. CurrentSpeed [{Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 2)}] CurrentStageRemainingSecondsWithoutPreviousStages [{Math.Round(CurrentStageRemainingSecondsWithoutPreviousStages, 0)}] CurrentStageRemainingSeconds [{Math.Round(CurrentStageRemainingSeconds, 0)}] Stage [{CurrentAbyssalStage}] SecondsNeededToReachTheGate [{Math.Round(_secondsNeededToReachTheGate, 0)}] DistanceToGate {Math.Round(_nextGate.Distance, 0)} MTUDistance {Math.Round(mtuDistane, 0)} MTUGateDist [{Math.Round(mtuGateDistance,0)}]");
                        }
                    }
                    else if (HigherPriorityTarget != null)
                    {
                        _moveToTarget = HigherPriorityTarget._directEntity.DirectAbsolutePosition;
                        _moveToTargetEntity = HigherPriorityTarget;
                    }
                    else if (Combat.PotentialCombatTargets.Count(i => i.BracketType != BracketType.Large_Collidable_Structure) < 3 && _getMTUInSpace != null ||
                             (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck && _getMTUInSpace != null)) // move to the mtu
                    {
                        _moveToTarget = _moveToTarget_MTU_Wreck_Can_or_Gate;

                        Log($"moveToTarget: PotentialCombatTargets [{Combat.PotentialCombatTargets.Count(i => i.BracketType != BracketType.Large_Collidable_Structure)}], Can we do this safely? CurrentStageRemainingSeconds [{Math.Round(CurrentStageRemainingSeconds, 0)}] Stage [{CurrentAbyssalStage}] MTUDistance {Math.Round(_getMTUInSpace.Nearest1KDistance, 0)}]");
                    }

                    else if (!ESCache.Instance.Targets.Any(i => i.GroupId != (int)Group.AssaultShip)) // if we dont have anything locked, move to the closest target on grid
                    {
                        _moveToTarget = Combat.PotentialCombatTargets.Where(e => e.GroupId != (int)Group.AbyssalBioAdaptiveCache).OrderBy(e => e.Distance).FirstOrDefault()._directEntity.DirectAbsolutePosition;
                        _moveToTargetEntity = Combat.PotentialCombatTargets.Where(e => e.GroupId != (int)Group.AbyssalBioAdaptiveCache).OrderBy(e => e.Distance).FirstOrDefault();
                    }

                    // if there isnt any entity on grid we want to be close with just go to one of the drones
                    else if (AnyEntityOnGridWeWantToBeCloseWith && allDronesInSpace.Any())
                    {
                        _moveToTarget = allDronesInSpace.OrderBy(e => e.Id).FirstOrDefault()._directEntity.DirectAbsolutePosition; // order by id to pick the same, unless it has been scooped
                        _moveToTargetEntity = allDronesInSpace.OrderBy(e => e.Id).FirstOrDefault();
                    }

                    if (_moveToTarget == _nextGate._directEntity.DirectAbsolutePosition)
                        _moveDirection = MoveDirection.Gate;

                    // move to target
                    if (_moveToTarget == null)
                    {
                        _moveToTarget = _moveToTarget_MTU_Wreck_Can_or_Gate;
                    }

                    return _moveToTarget ?? null;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return _nextGate._directEntity.DirectAbsolutePosition;
                }
            }
        }

            //= _getMTUInSpace ?? _nextGate; // the default

        private bool WrecksAreReadyToLoot
        {
            get
            {
                try
                {
                    if (ESCache.Instance.Wrecks == null)
                        return false;

                    if (!ESCache.Instance.Wrecks.Any())
                        return false;

                    if (Combat.PotentialCombatTargets.Any(i => (i.IsAbyssalBioAdaptiveCache || i.IsAbyssalDeadspaceTriglavianExtractionNode) && i.IsReadyToTarget))
                        return false;

                    if (ESCache.Instance.Wrecks.Any(w => !w.IsWreckEmpty && w.IsAbyssalCacheWreck))
                        return true;

                    return false;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return false;
                }
            }
        }

        private bool MtuWillBeReadyByTheTimeWeGetThere
        {
            get
            {
                try
                {
                    if (!IsItSafeToMoveToMTUOrLootWrecks)
                        return false;

                    if (_getMTUInSpace == null)
                        return false;

                    if (ESCache.Instance.Wrecks == null)
                        return false;

                    if (!ESCache.Instance.Wrecks.Any())
                        return false;

                    if (ESCache.Instance.Wrecks.Any(w => w.IsWreckEmpty && w.IsAbyssalCacheWreck)) //wreck is looted, MTU is ready
                        return true;

                    if (ESCache.Instance.Wrecks.Any(w => !w.IsWreckEmpty && w.IsAbyssalCacheWreck && 15000 > w.DistanceTo(_getMTUInSpace)))
                        return true;

                    if (ESCache.Instance.Wrecks.Any(w => !w.IsWreckEmpty && w.IsAbyssalCacheWreck && 25000 > w.DistanceTo(_getMTUInSpace)))
                        return true;

                    return false;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return false;
                }
            }
        }
        public List<EntityCache> GetSortedTargetList(IEnumerable<EntityCache> list)
        {
            if (DebugConfig.DebugDrones) Log("GetSortedTargetList: using AbyssalTargetPriority, IsReadyForDronesToShoot, StructurePct, ArmorPct, ShieldPct");
            list.OrderByDescending(e => e.Id ^ (long.MaxValue - 1337))
            .OrderBy(i => i._directEntity.AbyssalTargetPriority)
            //.ThenByDescending(e => e.Id == _group1OrSingleTargetId || e.Id == _group2TargetId)
            .ThenByDescending(x => (Drones.UseDrones && x.IsReadyForDronesToShoot) || (!Drones.UseDrones && x.IsReadyToShoot))
            .ThenBy(x => x.StructurePct)
            .ThenBy(y => y.ArmorPct)
            .ThenBy(z => z.ShieldPct)
            .ToList();

            return list.ToList();
        }

        private int _mtuScoopAttempts = 0;

        private HashSet<long> _alreadyLootedItems = new HashSet<long>();

        private int MaxDistanceFromGateToAllowMTULaunch
        {
            get
            {
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                            return 10000;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                            return 10000;
                    }

                    return 20000;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                            return 5000;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                            return 3000;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                            return 3000;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikVilaFrigateSwarmerSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                            return 3000;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakVilaCruiserSwarmerSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                            return 3000;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                            return 3000;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.RodivaSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                            return 3000;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                            return 3000;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                            return 5000;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                            return 10000;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                            return 3000;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                            return 10000;
                    }

                    if (!Combat.PotentialCombatTargets.Any())
                        return 3000;

                    return 10000;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                            return 5000;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any())
                            return 5000;
                    }

                    return 12000;
                }

                return 20000;
            }
        }

        bool ShouldWeLaunchMTU
        {
            get
            {
                var nearMTUDropSpot = SafeSpotNearGate != null &&
                      SafeSpotNearGate.GetDistance(DirectEve.ActiveShip.Entity.DirectAbsolutePosition) <=
                      7000;
                var dropMtuTimeConstraint = GetEstimatedStageRemainingTimeToClearGrid() + 40 > _secondsNeededToReachTheGate;
                //var dropMtuTimeConstraint = _secondsNeededToRetrieveWrecks >= GetEstimatedStageRemainingTimeToClearGrid() // + _secondsNeededToReachTheGate
                var dropMTUAllContainersPopped = WrecksAreReadyToLoot;

                var anyVortonProjectorsOnGrid = Combat.PotentialCombatTargets.Any(e => e._directEntity.NPCHasVortonProjectorGuns);
                var nearGate = _nextGate != null && _nextGate.Distance <= 15000;
                var lowNumOfEnemiesLeft = Combat.PotentialCombatTargets.Where(i => i.BracketType != BracketType.Large_Collidable_Structure).Count() <= 6;
                //var closeRangeBCsOnGrid = TargetsOnGrid.Where(e => e.GroupId == 1997 && e.IsNPCBattlecruiser).Count() >= 2; // do not launch the MTU if there are heavy dps close range bcs on the grid, because during moving to the mtu we might get one shot
                //var drifterBs = _targetsOnGrid.Any(e => e.TypeId == 56214);
                //var moveToOverrideExists = Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) && MoveToOverride != null;

                if (!boolDoWeHaveTime)
                {
                    if (DebugConfig.DebugMobileTractor) Log("if (AvoidDeathMoveOnNow)");
                    return false;
                }

                if (60 > Time.Instance.SecondsSinceLastSessionChange && Combat.PotentialCombatTargets.Any() && Combat.PotentialCombatTargets.Count() >= 4)
                {
                    if (DebugConfig.DebugMobileTractor) Log("Time.Instance.SecondsSinceLastSessionChange [" + Math.Round(Time.Instance.SecondsSinceLastSessionChange, 0) + "] is less than 60sec and NPCs Count [" + Combat.PotentialCombatTargets.Count() + "] is at least 4");
                    return false;
                }

                if (ESCache.Instance.Wrecks == null)
                {
                    if (DebugConfig.DebugMobileTractor) Log("if (ESCache.Instance.Wrecks == null)");
                    return false;
                }

                if (!ESCache.Instance.Wrecks.Any()) //should we check for Loot rights?
                {
                    if (DebugConfig.DebugMobileTractor) Log("if (!ESCache.Instance.Wrecks.Any())");
                    return false;
                }

                if (ESCache.Instance.Wrecks.All(i => i.IsWreckEmpty))
                {
                    if (DebugConfig.DebugMobileTractor) Log("All wrecks are empty: no need to drop the MTU");
                    return false;
                }

                if (_getMTUInSpace != null)
                {
                    if (DebugConfig.DebugMobileTractor) Log("if (_getMTUInSpace != null)");
                    return false;
                }

                if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck)
                {
                    if (DebugConfig.DebugMobileTractor) Log("if (WeHavePickedUpTheLootFromTheBioadaptiveCacheWreck)");
                    return false;
                }

                if (_getMTUInBay == null)
                {
                    if (DebugConfig.DebugMobileTractor) Log("if (_getMTUInBay == null)");
                    return false;
                }

                if (DirectEve.Interval(2500, 5000) && anyVortonProjectorsOnGrid)
                {
                    Log(
                        $"------- Vorton projector entity found on grind. Not launching the MTU yet. Vorton entity amount [{Combat.PotentialCombatTargets.Count(e => e._directEntity.NPCHasVortonProjectorGuns)}]");
                }

                if (_singleRoomAbyssal && DebugConfig.DebugFourteenBattleshipSpawnRunAway)
                {
                    if (DebugConfig.DebugMobileTractor) Log("if (_singleRoomAbyssal)");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn)
                {
                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                    {
                        if (DebugConfig.DebugMobileTractor) Log("if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn)");
                        if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship && i.Name.Contains("Blinding")) >= 2)
                        {
                            if (DebugConfig.DebugMobileTractor) Log("if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship)) //damps are a problem!");
                            return false;
                        }

                    }

                    if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch)
                    {
                        if (DebugConfig.DebugMobileTractor) Log("if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch [" + MaxDistanceFromGateToAllowMTULaunch + "])");
                        return false;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn)
                {
                    if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                    if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch)
                    {
                        if (DebugConfig.DebugMobileTractor) Log("if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch [" + MaxDistanceFromGateToAllowMTULaunch + "])");
                        return false;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                {
                    if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                    if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch)
                    {
                        if (DebugConfig.DebugMobileTractor) Log("if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch [" + MaxDistanceFromGateToAllowMTULaunch + "])");
                        return false;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                {
                    if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                    if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch)
                    {
                        if (DebugConfig.DebugMobileTractor) Log("if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch [" + MaxDistanceFromGateToAllowMTULaunch + "])");
                        return false;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn)
                {
                    if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                    if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch)
                    {
                        if (DebugConfig.DebugMobileTractor) Log("if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch [" + MaxDistanceFromGateToAllowMTULaunch + "])");
                        return false;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                {
                    if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                    if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch)
                    {
                        if (DebugConfig.DebugMobileTractor) Log("if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch [" + MaxDistanceFromGateToAllowMTULaunch + "])");
                        return false;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                {
                    if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                    if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch)
                    {
                        if (DebugConfig.DebugMobileTractor) Log("if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch [" + MaxDistanceFromGateToAllowMTULaunch + "])");
                        return false;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn)
                {
                    if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                    if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch)
                    {
                        if (DebugConfig.DebugMobileTractor) Log("if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch [" + MaxDistanceFromGateToAllowMTULaunch + "])");
                        return false;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                {
                    if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                    if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch)
                    {
                        if (DebugConfig.DebugMobileTractor) Log("if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch [" + MaxDistanceFromGateToAllowMTULaunch + "])");
                        return false;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn)
                {
                    if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                    if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch)
                    {
                        if (DebugConfig.DebugMobileTractor) Log("if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch [" + MaxDistanceFromGateToAllowMTULaunch + "])");
                        return false;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                {
                    if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                    if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch)
                    {
                        if (DebugConfig.DebugMobileTractor) Log("if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch [" + MaxDistanceFromGateToAllowMTULaunch + "])");
                        return false;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.RodivaSpawn)
                {
                    if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                    if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch)
                    {
                        if (DebugConfig.DebugMobileTractor) Log("if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch [" + MaxDistanceFromGateToAllowMTULaunch + "])");
                        return false;
                    }
                }

                if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");
                if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch)
                {
                    if (DebugConfig.DebugMobileTractor) Log("if (ESCache.Instance.AbyssalGate.Distance > MaxDistanceFromGateToAllowMTULaunch [" + MaxDistanceFromGateToAllowMTULaunch + "])");
                    return false;
                }

                if (_mtuAlreadyDroppedDuringThisStage)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.BracketType != BracketType.Large_Collidable_Structure) >= 6)
                    {
                        if (7000 > _nextGate.Distance)
                        {
                            if (ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                            {
                                _mtuAlreadyDroppedDuringThisStage = false;
                            }
                        }
                    }

                    if (!ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty && i.IsAbyssalCacheWreck))
                    {
                        if (DebugConfig.DebugMobileTractor) Log("if (_mtuAlreadyDroppedDuringThisStage)");
                        return false;
                    }
                    else //we still have a BioAdaptive cache wreck with loot in it in space ffs!
                    {
                        if (DebugConfig.DebugMobileTractor) Log("_mtuAlreadyDroppedDuringThisStage [true] we still have a BioAdaptive cache wreck with loot in it in space ffs!");
                        _mtuAlreadyDroppedDuringThisStage = false;
                    }
                }

                if (anyVortonProjectorsOnGrid)
                {
                    if (DebugConfig.DebugMobileTractor) Log("if (anyVortonProjectorsOnGrid)");
                    return false;
                }

                if (!IsOurShipWithintheAbyssBounds())
                {
                    if (DebugConfig.DebugMobileTractor) Log("if (!IsOurShipWithintheAbyssBounds())");
                    return false;
                }

                if (ESCache.Instance.ActiveShip.Entity.Distance > ESCache.Instance.SafeDistanceFromAbyssalCenter - 5000)
                {
                    if (DebugConfig.DebugMobileTractor) Log("if (ESCache.Instance.ActiveShip.Entity.Distance [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Distance, 0) + "] > SafeDistanceFromCenter - 5000 [" + (ESCache.Instance.SafeDistanceFromAbyssalCenter - 5000) + "])");
                    return false;
                }

                if (AreWeInsideOfAnyCloud && ESCache.Instance.AbyssalGate.Distance > 20000 && Combat.PotentialCombatTargets.Any(i => i.BracketType == BracketType.Large_Collidable_Structure))
                {
                    Log("AreWeInsideOfAnyCloud [" + AreWeInsideOfAnyCloud + "]");
                    return false;
                }

                if (_singleRoomAbyssal && !DebugConfig.DebugFourteenBattleshipSpawnRunAway)
                {
                    if (ESCache.Instance.AbyssalGate.Distance > 15000)
                        return false;
                }

                //if (!IsOurShipOutSideFromEntitiesWeWantToAvoid)
                //{
                //    Log("IsOurShipOutSideFromEntitiesWeWantToAvoid [" + IsOurShipOutSideFromEntitiesWeWantToAvoid + "]");
                //    return false;
                //}

                if (nearGate)
                {
                    Log("if (nearGate)");
                    return true;
                }

                if (nearMTUDropSpot)
                {
                    Log("if (nearMTUDropSpot)");
                    return true;
                }

                if (dropMtuTimeConstraint)
                {
                    Log("if (dropMtuTimeConstraint)");
                    return true;
                }

                if (dropMTUAllContainersPopped)
                {
                    Log("if (dropMTUAllContainersPopped)");
                    return true;
                }

                if (lowNumOfEnemiesLeft)
                {
                    Log("if (lowNumOfEnemiesLeft)");
                    return true;
                }

                //if (moveToOverrideExists)
                //{
                //    Log("if (moveToOverrideExists)");
                //    return true;
                //}

                //if (DebugConfig.DebugMobileTractor) Log("false");
                return false;
            }
        }

        bool ShouldWeScoopMTU
        {
            get
            {
                if (_getMTUInSpace == null)
                    return false;

                if (_lastMTULaunch.AddSeconds(12) > DateTime.UtcNow)
                    return false;

                var currentStageAddedSeconds =
                CurrentAbyssalStage == AbyssalStage.Stage3
                    ? 20
                    : 0; // remove 20 seconds before starting to empty the mtu in stage 3 (to potentially prevent dying while waiting for the loot)

                //if (DateTime.UtcNow > _lastMTULaunch.AddSeconds(45) && CurrentStageRemainingSecondsWithoutPreviousStages - (currentStageAddedSeconds + _secondsNeededToReachTheGate) < 0)
                //{
                //    Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] CurrentStageRemainingSecondsWithoutPreviousStages [" + CurrentStageRemainingSecondsWithoutPreviousStages + "] - (currentStageAddedSeconds [" + currentStageAddedSeconds + "] + _secondsNeededToReachTheGate [" + _secondsNeededToReachTheGate + "]) < 0) Skip Looting! return true");
                //    return true;
                //}

                if (ESCache.Instance.Wrecks != null && ESCache.Instance.Wrecks.Any(w => !w.IsWreckEmpty))
                {
                    if (ESCache.Instance.Wrecks != null && ESCache.Instance.Wrecks.Any(w => !w.IsWreckEmpty && 10000 > w.DistanceTo(_getMTUInSpace)))
                    {
                        if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] Wreck with loot within 10k: waiting for MTU to collect");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) && _GetEstimatedStageRemainingTimeToClearGrid > 110)
                    {
                        if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] PotentialCombatTargets [" + Combat.PotentialCombatTargets.Count(i => i.BracketType != BracketType.Large_Collidable_Structure) + "] _GetEstimatedStageRemainingTimeToClearGrid [" + Math.Round(_GetEstimatedStageRemainingTimeToClearGrid, 0) + "] > 60)");
                        if (ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                        {
                            if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] We have [" + ESCache.Instance.Wrecks.Count() + "] Wrecks that are not yet empty: do not pickup the MTU yet");
                            return false;
                        }
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) && _GetEstimatedStageRemainingTimeToClearGrid > 90)
                    {
                        if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] PotentialCombatTargets [" + Combat.PotentialCombatTargets.Count(i => i.BracketType != BracketType.Large_Collidable_Structure) + "] _GetEstimatedStageRemainingTimeToClearGrid [" + Math.Round(_GetEstimatedStageRemainingTimeToClearGrid, 0) + "] > 45)");
                        if (ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty && 50000 > i.DistanceTo(_getMTUInSpace)))
                        {
                            if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] unlooted wrecks within 50k [" + ESCache.Instance.Wrecks.Count(i => !i.IsWreckEmpty && 50000 > i.Distance) + "]: dont pickup the MTU yet");
                            return false;
                        }
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) && (_getMTUInSpace != null && 15000 > _getMTUInSpace.DistanceTo(_nextGate)))
                    {
                        if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] PotentialCombatTargets.Any() [" + Combat.PotentialCombatTargets.Count(i => i.BracketType != BracketType.Large_Collidable_Structure) + "] We are within 12k of the gate: waiting for MTu to do its work)");
                        if (ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                        {
                            if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] unlooted wrecks [" + ESCache.Instance.Wrecks.Count(i => !i.IsWreckEmpty) + "]: dont pickup the MTU yet");
                            return false;
                        }
                    }
                }

                //if (_trigItemCaches.Any(e => e.Distance <= _maxDroneRange))
                //{
                //    if (DebugConfig.DebugMobileTractor) Log("if (_trigItemCaches.Any(e => e.Distance <= _maxDroneRange && IsEntityWeWantToLoot(e._directEntity)))");
                //    return false;
                //}

                if (ESCache.Instance.Wrecks != null && ESCache.Instance.Wrecks.Any(w => w.IsWreckEmpty && w.IsAbyssalCacheWreck))
                {
                    Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] Triglavian Bioadaptive Cache Wreck is Empty return true");
                    return true;
                }

                if (!boolDoWeHaveTime)
                    return true;

                if (DebugConfig.DebugMobileTractor) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] return false;");
                return false;
            }
        }

        internal bool HandleMTU()
        {
            try
            {
                if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsPod)
                    return true;

                if (_getMTUInBay != null)
                {
                    _mtuScoopAttempts = 0;
                }

                // loot and scoop the mtu if all wrecks are empty or if time is running out
                if (ShouldWeScoopMTU)
                {
                    if (_getMTUInSpace != null && _getMTUInSpace.Distance <= (double)Distances.ScoopRange &&
                        _lastMTULaunch.AddSeconds(12) < DateTime.UtcNow)
                    {

                        var cont = ESCache.Instance.DirectEve.GetContainer(_getMTUInSpace.Id);
                        if (cont == null)
                        {
                            Log($"Error: Cont == null!");
                            return false;
                        }

                        if (cont.Window == null)
                        {
                            if (DirectEve.Interval(1500, 2500))
                            {
                                /**
                                if (DebugConfig.DebugMobileTractor)
                                {
                                    Log("Checking Each window in ESCache.Instance.Windows");

                                    int windowNum = 0;
                                    foreach (DirectWindow window in ESCache.Instance.Windows)
                                    {
                                        windowNum++;
                                        Log("[" + windowNum + "] Debug_Window.Name: [" + window.Name + "]");
                                        Log("[" + windowNum + "] Debug_Window.Html: [" + window.Html + "]");
                                        Log("[" + windowNum + "] Debug_Window.Guid: [" + window.Guid + "]");
                                        Log("[" + windowNum + "] Debug_Window.IsModal: [" + window.IsModal + "]");
                                        Log("[" + windowNum + "] Debug_Window.Caption: [" + window.Caption + "]");
                                        Log("--------------------------------------------------");
                                    }
                                }
                                **/
                                if (_getMTUInSpace == null)
                                {
                                    Log("MTUSpeed Not Found in space");
                                    return false;
                                }

                                if (_getMTUInSpace.OpenCargo())
                                {
                                    Log($"Opening container cargo.");
                                }
                            }

                            return true;
                        }

                        if (!cont.Window.IsReady)
                        {
                            Log($"Container window not ready yet.");
                            return false;
                        }

                        if (cont.Items.Any())
                        {
                            if (DirectEve.Interval(15000, 20000))
                                Log($"MTU not empty, looting.");

                            var totalVolume =
                                cont.Items.Sum(i =>
                                    i.TotalVolume);
                            var freeCargo = DirectEve.GetShipsCargo().Capacity -
                                            DirectEve.GetShipsCargo().UsedCapacity -
                                            101; // 100 is coming from the MTU itself, which we will scoop after

                            if (freeCargo < totalVolume)
                            {
                                // for now just scoop the mtu to keep going
                                Log($"There is not enough free cargo left. Scooping the mtu to keep going.");
                                if (_lastLoot.AddSeconds(1) < DateTime.UtcNow && _lastMTULaunch.AddSeconds(12) < DateTime.UtcNow && ScoopMTU() && _mtuScoopAttempts < 11)
                                {
                                    _mtuScoopAttempts++;
                                    Log($"Scooping the MTU. MTUScoop attempts: [{_mtuScoopAttempts}]");
                                    return true;
                                }

                                return true;
                            }

                            if (DirectEve.Interval(2500, 3500, freeCargo.ToString()) && DirectEve.GetShipsCargo().Add(cont.Items))
                            {
                                float currentLootedValue = 0;
                                Log($"Moving loot to current ships cargo.");
                                foreach (var item in cont.Items)
                                {
                                    if (_alreadyLootedItems.Contains(item.ItemId))
                                        continue;

                                    var value = item.AveragePrice() * item.Quantity;
                                    Log(
                                        $"Item Typename[{item.TypeName}] Amount [{item.Quantity}] Value [{value}]");
                                    _valueLooted += value;
                                    currentLootedValue += (float)value;
                                    _alreadyLootedItems.Add(item.ItemId);
                                }

                                var totalMinutes = (DateTime.UtcNow - _timeStarted).TotalMinutes;
                                if (totalMinutes != 0 && _valueLooted != 0)
                                {
                                    var millionIskPerHour = (((_valueLooted / totalMinutes) * 60) / 1000000);
                                    UpdateIskLabel(millionIskPerHour);
                                }

                                if (_abyssStatEntry != null)
                                {
                                    switch (CurrentAbyssalStage)
                                    {
                                        case AbyssalStage.Stage1:
                                            _abyssStatEntry.LootValueRoom1 =
                                                (float)Math.Round(currentLootedValue / 1000000, 2);
                                            break;

                                        case AbyssalStage.Stage2:
                                            _abyssStatEntry.LootValueRoom2 =
                                                (float)Math.Round(currentLootedValue / 1000000, 2);
                                            break;

                                        case AbyssalStage.Stage3:
                                            _abyssStatEntry.LootValueRoom3 =
                                                (float)Math.Round(currentLootedValue / 1000000, 2);
                                            break;
                                    }
                                }

                                WCFClient.Instance.GetPipeProxy.SetEveAccountAttributeValue(WCFClient.Instance.GUID, nameof(EveAccount.LootValueGatheredToday), Math.Round(ESCache.Instance.EveAccount.LootValueGatheredToday + _valueLooted, 0));
                                _lastLoot = DateTime.UtcNow;
                                return false; //make sure the mtu is empty before returning true!
                            }
                        }
                        else
                        {
                            // if there are no items, scoop it
                            if (_lastLoot.AddSeconds(1) < DateTime.UtcNow && _lastMTULaunch.AddSeconds(12) < DateTime.UtcNow && ScoopMTU() && _mtuScoopAttempts < 11)
                            {
                                _mtuScoopAttempts++;
                                Log($"Scooping the MTU. MTUScoop attempts: [{_mtuScoopAttempts}]");
                                return true;
                            }
                        }
                    }
                }

                // drop the mtu if near the gate
                // only drop if is not single room abyss
                // prob want to adapt the distance if we are in a speed cloud, may can happen that we go too fast in those clouds and end up dead locking
                // drop the mtu if wreck retrieval time >= (timeNeededToClear + secondsNeededToReachGate)
                var nearMTUDropSpot = SafeSpotNearGate != null &&
                                      SafeSpotNearGate.GetDistance(DirectEve.ActiveShip.Entity.DirectAbsolutePosition) <=
                                      7000;
                var dropMtuTimeConstraint = CurrentStageRemainingSecondsWithoutPreviousStages <= 250;
                //var dropMtuTimeConstraint = _secondsNeededToRetrieveWrecks >= GetEstimatedStageRemainingTimeToClearGrid() // + _secondsNeededToReachTheGate
                var dropMTUAllContainersPopped = WrecksAreReadyToLoot;
                var anyVortonProjectorsOnGrid = Combat.PotentialCombatTargets.Any(e => e._directEntity.NPCHasVortonProjectorGuns);
                var nearGate = _nextGate != null && _nextGate.Distance <= 15000;
                var enemiesLeft = Combat.PotentialCombatTargets.Where(i => i.BracketType != BracketType.Large_Collidable_Structure).Count() <= 4;
                //var closeRangeBCsOnGrid = TargetsOnGrid.Where(e => e.GroupId == 1997 && e.IsNPCBattlecruiser).Count() >= 2; // do not launch the MTU if there are heavy dps close range bcs on the grid, because during moving to the mtu we might get one shot
                //var drifterBs = _targetsOnGrid.Any(e => e.TypeId == 56214);
                //var moveToOverrideExists = Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) && MoveToOverride != null;

                if (ShouldWeLaunchMTU)
                {
                    if (_lastMTUScoop.AddSeconds(10) < DateTime.UtcNow && _lastMTULaunch.AddSeconds(10) < DateTime.UtcNow &&
                        DirectEve.Interval(1500, 2500))
                    {
                        if (LaunchMTU()) // this is and should be the only spot where we launch the MTU.
                        {
                            _mtuAlreadyDroppedDuringThisStage = true;
                            Log($"nearMTUDropSpot: {nearMTUDropSpot} dropMtuTimeConstraint: {dropMtuTimeConstraint} dropMTUAllContainersPopped: {dropMTUAllContainersPopped} enemiesLeft: {enemiesLeft}");
                        }

                        return true;
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
    }
}