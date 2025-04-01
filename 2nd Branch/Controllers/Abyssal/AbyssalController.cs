//
// (c) duketwo 2022
//

// |-- TODO ------------------------------------------->
// 01. Missing statistics: [DroneWaitToReturnStage1 .. 3, WeaponMissesPerStage1 .. 3 DroneMissesPerStage1  .. 3 (read from game logs)], AssaultDamageControlActivated
// 02. -DONE- We may drop the MTU before if the time constraints require us to do so. (i.e EstimatedGridKillTime < RequiredTimeToTractorAllWrecks)
// 03. -DONE- Get the fuck away from neuts. Somehow?
// 04. -DONE- Abyss stage persistence (so we can crash/reconnect with any issue) [done... confirm working state]
// 05. -DONE- Further improve drone launch/scoop. Maybe add additional logic to loop (launch->attack->scoop) to prevent npc locks on the drones. Some enemies are oneshotting med/small drones. Especially in some clouds.
// 05. -DONE- Make use of yellow boxing, entity followId, we are just plain using the followId
// 06. -DONE- Enforce better cap usage. (i.e. perma shield boost only if cap is above > 50, else boost only if the shieldDamage >= shieldBoosterShieldAmount)
// 07. -SKIP- If we are about to die due neuts, spam the shield booster to possibly get one cycle. (Maybe: Boost if currentCap >= shieldBoosterRequiredCapAmount)
// 08. -DONE- Make use of Nanite repair paste
// 09. -NOPE- Some drones are webbed, and we are moving away from them faster than they can get to us. (Move towards them, while they are in armor?)
// 10. -DONE- Activate the shieldbooster while shutting down (for whatever reason) -- We should add some event if the server is shutting down and do something during the last 10 seconds
// 11. -DONE- Keep the Shield booster up as much as possible (in case of a crash)
// 12. -DONE- If there is a vorton projector (how do we know if there are voltron projectors?) on the grid, STAY more than 10KM away from the MTU. Voltron projectors have up to 10KM chaining range. [fixed by not deploying the MTU until there is no vorton projector left.
// 13. -DONE-
// 14. -TEST- Add code to leave the the MTU behind (if we can't scoop it for whatever reason) -- It never happened, but it would be a deadend case, so handle it.
// 15. -IN PROGRESS- Think about how to be more time efficient. What is the optimal pathing? How can we achieve that in code? (i.e. calculating the best spot for the loot, which would be the spot which is the nearest to all loot containers + gate)
// 16. -DONE- We can't go upwards (in case of neuts) through a speed bubble, this will bring us too far outside and kill us. Alternative: Enforce a speed limit while being outside of the bounds and have very high speed (in a speed cloud)! (Already slow down if we are close to the boundary?)
// 17. -DONE- Better weapon management (20k max range, pref closer targets but don't swap too frequently!)
// 18. -DONE- Orbit the gate properly, also orbit the MTU properly (don't turn of the afterburner if there are more than X enemies on the grid?)
// 19. -SEMI DONE- Get data about currentNPCDamageOnGrid (do we even need that?), NeutAmountOnGrid (in optimal will be enough for both calculations to compare different spawns and set thresholds)
// 20. -DONE- (Reworked moveOnGrid) There is a bug within the MoveOnGrid method which selects the gate as moveToTarget for whatever reason when it shouldn't. (It doesn't break anything)
// 21. -DONE- (By fitting) Another deadgecase: If we go down to 0 cap due neuts and manage to survive, the cap regen is not quick enough to stabilize again while the shield booster is being spammed. This will fail horribly while being outside of the abyss boundary where we need the 400-500 hp/s boosts.
// 22. -DONE- (We're not leaving bounds anymore with new fit) Need log about how far we are outside of the abyss bounds.
// 23. -DONE- The tesser blaster spawn still can wipe our drones. Any strategy for this spawn? We possibly solved this by delaying the drone spawn after a session change. Awaiting confirmation [..]
// 24. -SKIP- We need a logic to move to the MTU faster, need check if the looting has been finished. Calculate ahead of time, when looting will be finished and move to the MTU accordingly. This is automatically done by the "time needed to get to the gate" method.
// 25. -DONE- Often we are still moving to the gate after the spawn has finished. Any better logic to be at the gate when all the grid is cleared?
// 26. -DONE- Add a bool = "IgnoreAbyssEntitiesDuringAStarPathfinding"
// 27. -DONE- If there is a vorton projector entity on grind, focus the targets which are further than 10k away from us. Else we kill our drones and waste a lot of time via constant recalling. What if all vorton NPCs are within 10k?
// 28. -DONE- We should not move too far away from the MTU if we are following the current target. We fixed that by calculating the time needed to move to the MTU/GATE. Still needs more testing
// 29. -DONE- We died to this: 1x Triglavian Extraction SubNode,1x Triglavian Extraction Node,1x Triglavian Bioadaptive Cache,4x Tangling Kikimora,4x Anchoring Damavik,1x Ghosting Kikimora,3x Striking Kikimora,2x Striking Damavik,1x Renewing Rodiva
// 30. -DONE- If we go backwards by the override, we should determine if the majority of enemies are in their optimal range, and if so, to save time we should move to to the gate if we are close to the abyss boundary.
// 31. -DONE- Damper priority, Weapon disruption ewar enemies / Anti Tracking which are targeting our drones
// 32. -DONE- Implement functional to launch drones if the majority (> 80%) of enemies are targeting us (except remote repair), also add a wait timeout (10 seconds since last session change)
// 33. -DONE- Possible dead case: If we fight rougue drones (or any other high dps spawn) while ignoring abyss entities, we might end up in a 4x sig radius bubble. This can kill us most likely. What to do about this? [We now move backwards, which seems fine]
// 34. -IN PROGRESS- Add some variations to look more like a hooman! Randomize fixed/static numbers (ammo loading numbers ... fixed percentage speed decrease in speed clouds ... what else? Fixed overheat times i.e.)
// 35. -TEST- [Abandon impl missing] If we are in a speed bubble during a stage and returning drones, they might never come back to us. Any strategy? (Move to the drone which isn't returning since X seconds..? Abandon drones in the worst case? Move out of the speed cloud?)
// 36. -SEMI DONE- Prio higher damage targets ... Can we determine them dynamically? For now maybe just prio "striking"?
// 37. -DONE- Fix Overheat. Propmod: Only overheat if we need to make distance to enemies, or if we can save time while moving to the gate. Hardener: We can overheat this one for a longer time
// 38. -MAYBE NOT REQUIRED- If we go backwards to the move to an override spot, check if we would path through a x4 sigradius cloud. If yes, we need to use different angles and find a backwards direction which is not passing through a sigcloud. It's maybe not required as the grid will be mostly cleared when the high DPS NPCs come close.
// 39. We need to know if we are in an abyss bubble and which type the bubble has. Also add some meaningful statistics to it.
// 40. -DONE- Any statistics for target/drone target swaps, also any better logic to reduce the amount of swaps
// 41. -CCP FIXED IT- Since the last eve patch were only consuming one booster (CCP BUG), enforce to always consume both if one was consumed.
// 42. -UNSURE- Should we also load T2 drones too and use them on certain spawns for more dmg? (Deepwatcher, Rogue Drone BS, Drifter BS, Ephi) -- Spawns with high HP, but lower amount of drone recalls if compared to avg drone recalls
// 43: -DONE- To make it more reliable we need to handle the following cases: a) -DONE- abandon drones impl missing] Track drone return duration timers and move outside of a speed cloud if necessary and possibly abandon drones if there is no other option
//                                                                            b) -DONE- Add code to leave the MTU behind if we can't scoop it for whatever reason, which is a very rare case, but we would be stuck at that point
//                                                                            c) -DONE- Add a logic to have the drones always at more than 10k away while fighting vorton projector NPCs
// 44. -DONE- Remove IsEntityWeWantToAvoidInAbyssals from Entity, it should be possible to set it on each call (default in the function header .. yeah it will be a bit of a mess, but opens the gates for finer grained avoidances)
// 45. We should ignore neuts if the neut GJ/s are below what our cap can handle. Neut NPCs usually do little damage, so most of the time better to spend the gained time elsewhere (i.e. killing targets which are focusing the drones or anything else)
// 46. -DONE- We got oneshot because we couldn't overheat. A repair cycle was active. Disable all repair if we need to overheat.
// 47. -SKIP- Add stats for how long we are waiting for the drones to return on each stage.
// 48. -DONE- Stop repairs if overheat.
// 49. -DONE- Ensure drones are alaways attacking. (There is some deadend state currently)
// 50. Add any logic to keep going when we reach the set threshold of items in the hangar. Currently it's set to 600. What can do we? Delete non faction blueprints? Or do we just leave the non faction BPCs behind (do not loot them -- which is fact may be a downside in the detectability department)?
// 51. -DONE- Randomize the keep at range / orbit distances.
// 52. We still want some numbers on the hitrate of the drones and weapons. Total hits fired / total hits landed.
// 53. -TEST- Make the thresholds for the database survey dump configurable.
// 54. Drones do not hit small targets properly while in a speed cloud. What can we do about it? We should move outside from speed clouds. Do we? If yes, we could recall drones if all entities that are in a speed cloud or focus the targets which are outside? Anything else? We need to lure them out of the cloud somehow while not losing much time.
// 55. To not to abandon drones, we could order them to attack the gate maybe [edit: you can't attack the gate with drones, any other idea]? So they don't MWD. Then move towards them and scoop? Is this viable? What about return and orbit?
// 56. -DONE- If we are in a station and in a pod -> Disabled this instance
// 57. -DONE- Check if any module is offline before entering an abyss.
// 58. We need to also automatically sell items, at least the filaments to keep the market stable.
// 59. -DONE- If we are sitting on the safe spot near gate and moving in and out of a speed cloud, we constantly deactivate/activate/overheat the after burner. Any solution to this? Limit max prop mod overheats per stage? Do only disable the prop mod every (25,35) seconds (given enough cap avail?). Maybe we start to stop moving while on the safe spot near gate unless we drop in shields?
// 60. Save the fucking pod somehow if we get ganked. This is very sloppy in the current state.
// 61. If we are constantly recalling drones, we need to be closer to the targets to reduce dps downtime. If they are in a speed cloud we need to lure them away by recalling drones and waiting a bit for them to get out. For any other case it will probably be fine to move close to them? (Except high dps targets..)
// 62. What can we do that drones are always focusing the 'correct target' while maintaining a low amount of drone commands? Constant swapping hurts the dps.

// |--------------------------------------------- END ->

// Price lookups:
// - T6 filaments: https://evepraisal.com/a/157hmt
// - T5 filaments: https://evepraisal.com/a/157hnw

/// [ - How the usual cycle of the bot looks like
///   01. Start in homestation
///   02. Arm: if out of items, buy with the cloak hauler
///   03. ...
///   04. ...
/// ]
///

extern alias SC;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Questor.Traveller;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.EVE.ClientSettings;
using SC::SharedComponents.EVE.DatabaseSchemas;
using SC::SharedComponents.Extensions;
using SC::SharedComponents.IPC;
using SC::SharedComponents.SQLite;
using SC::SharedComponents.Utility;
using ServiceStack.OrmLite;
using SharpDX.Direct2D1;
using EVESharpCore.Framework.Lookup;
using EVESharpCore.Framework.Events;
using SC::SharedComponents.Events;
using System.Runtime.InteropServices;

namespace EVESharpCore.Controllers.Abyssal
{
    public enum AbyssalRoomStrategy
    {
        Unspecified,
        Normal,
        HeavyDronesForExploitingNPCResistHole,
    }

    public partial class AbyssalController : AbyssalBaseController
    {
        public bool DroneDebugState { get; set; } = false;

        private double _propModMinCapPerc
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate)
                {
                    return 6;
                }

                return 35;
            }
        }
        private double _moduleMinCapPerc = 2;
        private double _capacitorInjectorInjectCapPerc = 15;
        private double _capacitorInjectorWaitCapPerc = 55;
        private double _repairerMinCapPerc
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate)
                {
                    return 4;
                }

                return 5;
            }
        }

        private double _repEnablePerc
        {
            get
            {
                if (ESCache.Instance.DirectEve.Modules.Any(i => i.GroupId == (int)Group.CapacitorInjector))
                {
                    if (ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate)
                        return 85;

                    if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Ishtar)
                        return 80;

                    if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.StormBringer)
                        return 80;

                    if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Vagabond)
                        return 80;

                    return 85;
                }

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Worm)
                    return 90;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Algos)
                    return 90;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Dragoon)
                    return 90;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.StormBringer)
                    return 101;

                return 101;
            }
        }

        private static double _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage = .35;
        private static int logoffEveAfterThisManyAbyssalRuns = 40;
        //private DateTime _nextStack;
        private bool PlayNotificationSounds => true;

        private static Dictionary<int, double> _droneRecoverShieldPerc
        {
            get
            {
                try
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                    {
                        return new Dictionary<int, double>()
                        {
                            [50] = 20,
                            [25] = 35,
                            [10] = 40,
                            [5] = 50,
                            [0] = 50,
                        };
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                    {
                        return new Dictionary<int, double>()
                        {
                            [50] = 20,
                            [25] = 35,
                            [10] = 40,
                            [5] = 70,
                            [0] = 50,
                        };
                    }

                    /**
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
                    {
                        return new Dictionary<int, double>()
                        {
                            [50] = 20,
                            [25] = 35,
                            [10] = 40,
                            [5] = 50,
                            [0] = 50,
                        };
                    }
                    **/

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                    {
                        return new Dictionary<int, double>()
                        {
                            [50] = 20,
                            [25] = 35,
                            [10] = 40,
                            [5] = 50,
                            [0] = 50,
                        };
                    }

                    return new Dictionary<int, double>()
                    {
                        [50] = 35,
                        [25] = 35,
                        [10] = 40,
                        [5] = 50,
                        [0] = 50,
                    };
                }
                catch
                {
                    return new Dictionary<int, double>()
                    {
                        [50] = 35,
                        [25] = 35,
                        [10] = 40,
                        [5] = 50,
                        [0] = 50,
                    };
                }
            }
        }


        private static Dictionary<int, double> _droneLaunchShieldPerc = new Dictionary<int, double>()
        {
            [50] = 60,
            [25] = 60,
            [10] = 65,
            [5] = 70,
            [0] = 70,
        };

        private DateTime _lastLoot;
        //private static int _ammoTypeId = 21906; // Republic Fleet Fusion S

        private static int _naniteRepairPasteTypeId = 28668; // Nanite Repair Paste
        //private static int _capBoosterTypeId = 32006; //Navy Cap Booster 400
        //private static int _capBoosterTypeId = 31990; //Navy Cap Booster 150
        private static int _capBoosterTypeId
        {
            get
            {
                if (Settings.Instance.CapacitorInjectorScript != 0)
                {
                    return Settings.Instance.CapacitorInjectorScript;
                }

                return 31990;
            }
        }

        //private static int _MyAmmoTypeId = 21906; // Republic Fleet Fusion S
        //private static int _MyAmmoTypeId = 210; //

        /**
        private static int _MyAmmoTypeId
        {
            get
            {
                try
                {
                    if (ESCache.Instance.ActiveShip != null)
                    {
                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                        {
                            return 210; //Scourge Light Missile I
                        }

                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                        {
                            return 21906; //Republic fleet fusion s
                        }

                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Worm)
                        {
                            return 210; //Scourge Light Missile I
                        }

                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Hawk)
                        {
                            return 24495; //Scourge Fury Light Missile
                        }

                        return 210;
                    }

                    return 210;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return 210;
                }
            }
        }
        **/

        /// <summary>
        /// </summary>
        ///
        private List<(int, int)> __shipsCargoBayList = null;
        private List<(int, int)> _shipsCargoBayList
        {
            get
            {
                if (__shipsCargoBayList != null) return __shipsCargoBayList;

                int _numOfFilamentsToBring = 1;
                if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Cruiser ||
                    ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila ||
                    ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar ||
                    ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer ||
                    ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Vagabond ||
                    ESCache.Instance.ActiveShip.TypeId == (int)TypeID.VexorNavyIssue
                    //ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Zealot ||
                    //ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Cerberus ||
                    //ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Vexor ||
                    //ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Omen
                    )
                {
                    _numOfFilamentsToBring = 1;
                }
                else if (ESCache.Instance.ActiveShip.GroupId == (int)Group.Destroyer)
                {
                    _numOfFilamentsToBring = 3;
                }
                else
                {
                    _numOfFilamentsToBring = 9;
                }

                if (_numOfFilamentsToBring > numOfFilamentsToBring)
                    numOfFilamentsToBring = _numOfFilamentsToBring;

                __shipsCargoBayList = new List<(int, int)>();
                if (_mtuTypeId != 0 && ESCache.Instance.ActiveShip.Capacity > 400 && !ESCache.Instance.EveAccount.UseFleetMgr && !ESCache.Instance.ActiveShip.IsDestroyer && !ESCache.Instance.ActiveShip.IsAssaultShip && !ESCache.Instance.ActiveShip.IsFrigate)
                {
                    var tempMtu = new DirectItem(DirectEve);
                    tempMtu.TypeId = (int)_mtuTypeId;
                    Log("shipsCargoBayList Adding [" + tempMtu.TypeName + "] x 1");

                    __shipsCargoBayList.Add(((int)_mtuTypeId, 1));
                }

                if (AllowOverloadOfHardener || AllowOverloadOfReps || AllowOverloadOfSpeedMod || AllowOverloadOfWeapons)
                {
                    if (ESCache.Instance.ActiveShip.IsFrigate)
                    {
                        if (_naniteRepairPasteTypeId != 0)
                        {
                            var tempNaniteRepairPaste = new DirectItem(DirectEve);
                            tempNaniteRepairPaste.TypeId = (int)_naniteRepairPasteTypeId;
                            Log("shipsCargoBayList Adding [" + tempNaniteRepairPaste.TypeName + "] x [ 75 ]");
                            __shipsCargoBayList.Add((_naniteRepairPasteTypeId, 75));
                        }
                    }
                    else if (ESCache.Instance.ActiveShip.IsDestroyer)
                    {
                        if (_naniteRepairPasteTypeId != 0)
                        {
                            var tempNaniteRepairPaste = new DirectItem(DirectEve);
                            tempNaniteRepairPaste.TypeId = (int)_naniteRepairPasteTypeId;
                            Log("shipsCargoBayList Adding [" + tempNaniteRepairPaste.TypeName + "] x [ 175 ]");
                            __shipsCargoBayList.Add((_naniteRepairPasteTypeId, 175));
                        }
                    }
                    else
                    {
                        //Add nanite repair paste if we are overheating
                        if (_naniteRepairPasteTypeId != 0)
                        {
                            var tempNaniteRepairPaste = new DirectItem(DirectEve);
                            tempNaniteRepairPaste.TypeId = (int)_naniteRepairPasteTypeId;
                            Log("shipsCargoBayList Adding [" + tempNaniteRepairPaste.TypeName + "] x [ 275 ]");
                            __shipsCargoBayList.Add((_naniteRepairPasteTypeId, 275));
                        }
                    }
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Hawk)
                {
                    //Add Cap Boosters
                    if (Settings.Instance.NumberOfCapBoostersToLoad > 0)
                    {
                        if (_capBoosterTypeId != 0)
                        {
                            var tempCapBoosters = new DirectItem(DirectEve);
                            tempCapBoosters.TypeId = (int)_capBoosterTypeId;
                            Log("shipsCargoBayList Adding [" + tempCapBoosters.TypeName + "] x [" + Settings.Instance.NumberOfCapBoostersToLoad + "]");
                            __shipsCargoBayList.Add((_capBoosterTypeId, Settings.Instance.NumberOfCapBoostersToLoad));
                        }
                    }
                }

                if (ESCache.Instance.EveAccount.UseFleetMgr && !ESCache.Instance.EveAccount.IsLeader)
                {
                    //skip filaments as a member of the fleet that is not the leader we dont need them
                }
                else
                {
                    var tempFilament = new DirectItem(DirectEve);
                    tempFilament.TypeId = (int)_filamentTypeId;
                    Log("shipsCargoBayList Adding [" + tempFilament.TypeName + "] x [" + numOfFilamentsToBring + "]");
                    __shipsCargoBayList.Add(((int)_filamentTypeId, numOfFilamentsToBring));
                }

                foreach (var individualDefinedAmmoType in DirectUIModule.DefinedAmmoTypes.Distinct())
                {
                    if (__shipsCargoBayList.Any(i => i.Item1 == individualDefinedAmmoType.TypeId))
                        continue;

                    if (individualDefinedAmmoType.TypeId != 0)
                    {
                        var tempAmmo = new DirectItem(DirectEve);
                        tempAmmo.TypeId = (int)individualDefinedAmmoType.TypeId;
                        Log("shipsCargoBayList Adding [" + tempAmmo.TypeName + "] x [" + individualDefinedAmmoType.Quantity + "]");
                        __shipsCargoBayList.Add((individualDefinedAmmoType.TypeId, individualDefinedAmmoType.Quantity));
                    }
                }

                //__shipsCargoBayList.Add((_MyAmmoTypeId, 3200));

                foreach (var _booster in _boosterList)
                {
                    var tempbooster = new DirectItem(DirectEve);
                    tempbooster.TypeId = (int)_booster.Item1;
                    Log("shipsCargoBayList Adding [" + tempbooster.TypeName + "] x [" + _booster.Item2 + "]");
                    __shipsCargoBayList.Add(_booster);
                }

                return __shipsCargoBayList;
            }
        }

        public static HashSet<long> BoosterTypesToLoadIntoCargo = new HashSet<long>();


        /// <summary>
        /// tuple(typeid, amount)
        /// </summary>
        private List<(int, int)> __boosterList = null; //= new List<(int, int)> { (46002, 2), (9950, 2) }; // 9950 = Standard Blue Pill Booster, 46002 = Agency 'Hardshell' TB5 Dose II

        private List<(int, int)> _boosterList
        {
            get
            {
                if (!DebugConfig.DebugDisableDrugsBoosters)
                {
                    __boosterList = new List<(int, int)>();

                    if (BoosterTypesToLoadIntoCargo.Count > 0)
                    {
                        foreach (var boosterToLoad in BoosterTypesToLoadIntoCargo)
                        {
                            __boosterList.Add(((int)boosterToLoad, 1));
                        }

                        return __boosterList;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                    {
                        __boosterList.Add((46002, 1)); // 46002 = Agency 'Hardshell' TB5 Dose II
                        //__boosterList.Add((36908, 1)); // 36908 = Antipharmakon Thureo (8% @ 14mil)  // 28670 = Synth Blue Pill Booster (6% @ 2mil)  // 9950 = Standard Blue Pill Booster (20% @ 2mil, + side effects!),
                        __boosterList.Add((28670, 1)); // 28670 = Synth Blue Pill Booster (6% @ 2mil)  // 9950 = Standard Blue Pill Booster (20% @ 2mil, + side effects!),
                        //__boosterList.Add((76110, 1)); // 76110 = State Defense Booster I,  76111 = State Defense Booster II, Slot 15
                        //__boosterList.Add((76123, 1)); // 76123 = Federation Damage Booster II, Slot 17
                        return __boosterList;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                    {
                        //__boosterList.Add((46002, 1)); // SLOT 11 - 46002 = Agency 'Hardshell' TB5 Dose II
                        //__boosterList.Add((36908, 1)); // 36908 = Antipharmakon Thureo (8% @ 14mil)  // 28670 = Synth Blue Pill Booster (6% @ 2mil)  // 9950 = Standard Blue Pill Booster (20% @ 2mil, + side effects!),
                        __boosterList.Add((28670, 1)); // 28670 = Synth Blue Pill Booster (6% @ 2mil)  // 9950 = Standard Blue Pill Booster (20% @ 2mil, + side effects!),
                                                       //__boosterList.Add((76110, 1)); // 76110 = State Defense Booster I,  76111 = State Defense Booster II, Slot 15
                                                       //__boosterList.Add((76123, 1)); // 76123 = Federation Damage Booster II, Slot 17
                        __boosterList.Add((45999, 1)); //Slot 11 // 45999 = Agency 'Pyrolancea' TB5 Dose II
                        return __boosterList;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                    {
                        //Tier 1 Abyssals in a Gila dont need boosters
                        if (_filamentTypeId == 47761 ||
                            _filamentTypeId == 47762 ||
                            _filamentTypeId == 47763 ||
                            _filamentTypeId == 47764 ||
                            _filamentTypeId == 47765
                            )
                        {
                            return __boosterList;
                        }

                        //Tier 2 Abyssals in a Gila dont need boosters
                        if (_filamentTypeId == 47892 ||
                            _filamentTypeId == 47904 ||
                            _filamentTypeId == 47888 ||
                            _filamentTypeId == 47896 ||
                            _filamentTypeId == 47900
                            )
                        {
                            return __boosterList;
                        }

                        __boosterList.Add((46002, 1)); // 46002 = Agency 'Hardshell' TB5 Dose II
                        __boosterList.Add((28670, 1)); // 28670 = Synth Blue Pill Booster (6% @ 2mil) // 36908 = Antipharmakon Thureo (8% @ 14mil)  // 28670 = Synth Blue Pill Booster (6% @ 2mil)  // 9950 = Standard Blue Pill Booster (20% @ 2mil, + side effects!),
                        //__boosterList.Add((76110, 1)); // 76110 = State Defense Booster I,  76111 = State Defense Booster II, Slot 15
                        //__boosterList.Add((76123, 1)); // 76123 = Federation Damage Booster II, Slot 17

                        return __boosterList;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.VexorNavyIssue)
                    {
                        //__boosterList.Add((46002, 1)); // 46002 = Agency 'Hardshell' TB5 Dose II
                        //__boosterList.Add((28670, 1)); // 28670 = Synth Blue Pill Booster (6% @ 2mil) // 36908 = Antipharmakon Thureo (8% @ 14mil)  // 28670 = Synth Blue Pill Booster (6% @ 2mil)  // 9950 = Standard Blue Pill Booster (20% @ 2mil, + side effects!),
                        //__boosterList.Add((76110, 1)); // 76110 = State Defense Booster I,  76111 = State Defense Booster II, Slot 15
                        //__boosterList.Add((76123, 1)); // 76123 = Federation Damage Booster II, Slot 17
                        return __boosterList;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Vagabond)
                    {
                        __boosterList.Add((46002, 1)); // 46002 = Agency 'Hardshell' TB5 Dose II
                                                       //__boosterList.Add((36908, 1)); // 36908 = Antipharmakon Thureo (8% @ 14mil)  // 28670 = Synth Blue Pill Booster (6% @ 2mil)  // 9950 = Standard Blue Pill Booster (20% @ 2mil, + side effects!),
                        __boosterList.Add((28670, 1)); // 28670 = Synth Blue Pill Booster (6% @ 2mil)  // 9950 = Standard Blue Pill Booster (20% @ 2mil, + side effects!),
                                                       //__boosterList.Add((76110, 1)); // 76110 = State Defense Booster I,  76111 = State Defense Booster II, Slot 15
                                                       //__boosterList.Add((76123, 1)); // 76123 = Federation Damage Booster II, Slot 17
                        __boosterList.Add((28674, 1)); // 28674 = Synth Drop
                        return __boosterList;
                    }

                    if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)//worm, dragoon, algos
                    {
                        //__boosterList.Add((46002, 1)); // 46002 = Agency 'Hardshell' TB5 Dose II
                        //__boosterList.Add((36908, 1)); // 36908 = Antipharmakon Thureo (8% @ 14mil)  // 28670 = Synth Blue Pill Booster (6% @ 2mil)  // 9950 = Standard Blue Pill Booster (20% @ 2mil, + side effects!),
                        //__boosterList.Add((76110, 1)); // 76110 = State Defense Booster I,  76111 = State Defense Booster II, Slot 15
                        //__boosterList.Add((76123, 1)); // 76123 = Federation Damage Booster II, Slot 17
                        return __boosterList;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Hawk && ESCache.Instance.EveAccount.UseFleetMgr)
                    {
                        __boosterList.Add((28672, 1)); //Slot 3  // 28672 = Synth Crash (6% Explosion Radius)
                        //__boosterList.Add((78315, 1)); //Slot 14  // 28672 = Tetrimon Anti-Drain Booster II (-12% @2mil each - Capacitor Warfare Resistance Bonus)
                        if (ESCache.Instance.EveAccount.IsLeader)
                        {
                            __boosterList.Add((9950, 1)); //Slot 1  // 9950 = Standard Blue Pill Booster (20% @ 2mil, + side effects!)
                            //__boosterList.Add((45998, 1)); //Slot 11 // 45998 = Agency 'Pyrolancea' TB3 Dose I
                            __boosterList.Add((45999, 1)); //Slot 11 // 45999 = Agency 'Pyrolancea' TB5 Dose II
                            //__boosterList.Add((46001, 1)); //Slot 11 // 46002 = Agency 'Hardshell' TB3 Dose I   //200k
                            //__boosterList.Add((46002, 1)); //Slot 11 // 46002 = Agency 'Hardshell' TB5 Dose II  //3.5mil
                            //__boosterList.Add((46003, 1)); //Slot 11 // 46002 = Agency 'Hardshell' TB7 Dose III //12mil
                            //__boosterList.Add((46004, 1)); //Slot 11 // 46002 = Agency 'Hardshell' TB9 Dose IV  //33mil
                            __boosterList.Add((78314, 1));   //Slot 14 // 78314 = Tetrimon Anti-Drain Booster I //1mil
                            //__boosterList.Add((78315, 1)); //Slot 14 // 78315 = Tetrimon Anti-Drain Booster II //10mil
                            //__boosterList.Add((78316, 1)); //Slot 14 // 78316 = Tetrimon Anti-Drain Booster III //20mil
                            //__boosterList.Add((78317, 1)); //Slot 14 // 78317 = Tetrimon Anti-Drain Booster IV //90mil
                            //__boosterList.Add((78318, 1)); //Slot 15 // 78318 = Tetrimon Capacitor Booster I //1mil
                            //__boosterList.Add((78319, 1)); //Slot 15 // 78319 = Tetrimon Capacitor Booster II //13mil
                            //__boosterList.Add((78320, 1)); //Slot 15 // 78320 = Tetrimon Capacitor Booster III //31mil
                            //__boosterList.Add((78321, 1)); //Slot 15 // 78321 = Tetrimon Capacitor Booster IV //125mil
                        }
                        else
                        {
                            __boosterList.Add((28670, 1)); //Slot 1  // 36908 = Antipharmakon Thureo (8% @ 14mil)  // 28670 = Synth Blue Pill Booster (6% @ 2mil)  // 9950 = Standard Blue Pill Booster (20% @ 2mil, + side effects!),
                            //__boosterList.Add((45998, 1)); //Slot 11 // 45998 = Agency 'Pyrolancea' TB3 Dose I
                            __boosterList.Add((45999, 1)); //Slot 11 // 45999 = Agency 'Pyrolancea' TB5 Dose II
                            //__boosterList.Add((46000, 1)); //Slot 11 // 46000 = Agency 'Pyrolancea' TB7 Dose III
                            //__boosterList.Add((46001, 1)); //Slot 11 // 46001 = Agency 'Pyrolancea' TB9 Dose IV
                            __boosterList.Add((78314, 1));   //Slot 14 // 78314 = Tetrimon Anti-Drain Booster I //1mil
                            //__boosterList.Add((78315, 1)); //Slot 14 // 78315 = Tetrimon Anti-Drain Booster II //10mil
                            //__boosterList.Add((78316, 1)); //Slot 14 // 78316 = Tetrimon Anti-Drain Booster III //20mil
                            //__boosterList.Add((78317, 1)); //Slot 14 // 78317 = Tetrimon Anti-Drain Booster IV //90mil
                            //__boosterList.Add((78318, 1)); //Slot 15 // 78318 = Tetrimon Capacitor Booster I //1mil
                            //__boosterList.Add((78319, 1)); //Slot 15 // 78319 = Tetrimon Capacitor Booster II //13mil
                            //__boosterList.Add((78320, 1)); //Slot 15 // 78320 = Tetrimon Capacitor Booster III //31mil
                            //__boosterList.Add((78321, 1)); //Slot 15 // 78321 = Tetrimon Capacitor Booster IV //125mil

                        }

                        //these were event boosters that dont exist any longer. there might be new ones in the future?
                        //Slot 17 //Republic Application Booster II (8% Explosion Velocity Bonus + 6% Tracking for guns)
                        //Slot 17 //Republic Damage Booster I (2% Missile Damage Bonus or Turret Damage Bonus)
                        //__boosterList.Add((76110, 1)); // 76110 = State Defense Booster I,  76111 = State Defense Booster II, Slot 15
                        //__boosterList.Add((76123, 1)); // 76123 = Federation Damage Booster II, Slot 17
                        return __boosterList;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Hawk)
                    {
                        __boosterList.Add((46002, 1)); // 46002 = Agency 'Hardshell' TB5 Dose II
                        __boosterList.Add((28670, 1)); //Slot 1 // 36908 = Antipharmakon Thureo (8% @ 14mil)  // 28670 = Synth Blue Pill Booster (6% @ 2mil)  // 9950 = Standard Blue Pill Booster (20% @ 2mil, + side effects!),
                        __boosterList.Add((28672, 1)); //Slot 3 // 28672 = Synth Crash (6% Explosion Radius)
                                                       //Slot 17 //Republic Application Booster II (8% Explosion Velocity Bonus + 6% Tracking for guns)
                                                       //Slot 17 //Republic Damage Booster I (2% Missile Damage Bonus or Turret Damage Bonus)
                                                       //__boosterList.Add((76110, 1)); // 76110 = State Defense Booster I,  76111 = State Defense Booster II, Slot 15
                                                       //__boosterList.Add((76123, 1)); // 76123 = Federation Damage Booster II, Slot 17
                        return __boosterList;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                    {
                        //__boosterList.Add((46002, 1)); // 46002 = Agency 'Hardshell' TB5 Dose II
                        //__boosterList.Add((28670, 1)); //Slot 1 // 36908 = Antipharmakon Thureo (8% @ 14mil)  // 28670 = Synth Blue Pill Booster (6% @ 2mil)  // 9950 = Standard Blue Pill Booster (20% @ 2mil, + side effects!),
                        //__boosterList.Add((28672, 1)); //Slot 3 // 28672 = Synth Crash (6% Explosion Radius)
                        //Slot 17 //Republic Application Booster II (8% Explosion Velocity Bonus + 6% Tracking for guns)
                        //Slot 17 //Republic Damage Booster I (2% Missile Damage Bonus or Turret Damage Bonus)
                        //__boosterList.Add((76110, 1)); // 76110 = State Defense Booster I,  76111 = State Defense Booster II, Slot 15
                        //__boosterList.Add((76123, 1)); // 76123 = Federation Damage Booster II, Slot 17
                        return __boosterList;
                    }

                    //__boosterList.Add((46002, 1)); // 46002 = Agency 'Hardshell' TB5 Dose II
                    //__boosterList.Add((36908, 1)); // 36908 = Antipharmakon Thureo (8% @ 14mil)  // 28670 = Synth Blue Pill Booster (6% @ 2mil)  // 9950 = Standard Blue Pill Booster (20% @ 2mil, + side effects!),
                    //__boosterList.Add((76110, 1)); // 76110 = State Defense Booster I,  76111 = State Defense Booster II, Slot 15
                    //__boosterList.Add((76123, 1)); // 76123 = Federation Damage Booster II, Slot 17
                    return __boosterList;
                }

                return new List<(int, int)>();
            }
        }

        private List<(int, int)> __increaseDamageBoostersList = null; //= new List<(int, int)> { (46002, 2), (9950, 2) }; // 9950 = Standard Blue Pill Booster, 46002 = Agency 'Hardshell' TB5 Dose II

        private List<(int, int)> _increaseDamageBoostersList
        {
            get
            {
                if (!DebugConfig.DebugDisableDrugsBoosters)
                {
                    __increaseDamageBoostersList = new List<(int, int)>
                    {
                        //(76123, 1),   // 76123 = Federation Damage Booster II, Slot 17
                        (28672, 1),   //Slot 3 // 28672 = Synth Crash (6% Explosion Radius)
                        (45998, 1), //Slot 11 // 45998 = Agency 'Pyrolancea' TB5 Dose I
                        (45999, 1), //Slot 11 // 45999 = Agency 'Pyrolancea' TB5 Dose II
                        (46000, 1), //Slot 11 // 46000 = Agency 'Pyrolancea' TB5 Dose III
                        (46001, 1), //Slot 11 // 46001 = Agency 'Pyrolancea' TB5 Dose IV
                        (78314, 1), //Slot 14 // 78314 = Tetrimon Anti-Drain Booster I
                        (78315, 1), //Slot 14 // 78315 = Tetrimon Anti-Drain Booster II
                        (78316, 1), //Slot 14 // 78316 = Tetrimon Anti-Drain Booster III
                        (78317, 1), //Slot 14 // 78317 = Tetrimon Anti-Drain Booster IV
                        (78318, 1), //Slot 15 // 78318 = Tetrimon Capacitor Booster I
                        (78319, 1), //Slot 15 // 78319 = Tetrimon Capacitor Booster II
                        (78320, 1), //Slot 15 // 78320 = Tetrimon Capacitor Booster III
                        (78321, 1), //Slot 15 // 78321 = Tetrimon Capacitor Booster IV
                        (28674, 1), //Slot 2  // 28674 = Synth Drop
                    };

                    return __increaseDamageBoostersList;
                }

                return new List<(int, int)>();
            }
        }

        /// <summary>
        /// tuple(typeid, amount)
        /// </summary>
        //private List<(int, int)> _droneBayItemList = new List<(int, int)> { (33681, 5), (28296, 7), (28304, 11) };  // [large -> med -> small] 5x gecko (33681) , 7 augmented valk (28296), 11 augmented warrior (28304)
        //private List<(int, int)> _droneBayItemList = new List<(int, int)> { (33681, 5), (21640, 7), (2488, 11) };  // [large -> med -> small] 5x gecko (33681) , 7 valk t2 (21640), 11 warrior t2 (2488)

        private static int? _geckoDroneTypeId = 33681;
        //private const int _karenDroneTypeId = 60480; // mutated heavys
        private static int? _karenDroneTypeId = 2478; // berserkers
        //private static int? _heavyDroneTypeId = 2478; // berserkers
        private static int? _heavyDroneTypeId = null; // berserkers
        //private const int _mediumDroneTypeId = 31890; //Republic Fleet Valk
        private static int? _mediumDroneTypeId = 31874; //Caldari Navy Vespa
        //private const int _smallDroneTypeId = 31888; //Republic Fleet Warrior
        private static int? _smallDroneTypeId = 31872; //Caldari Navy Hornet

        //private int _overheatRandMaxOffset = 6;

        /// <summary>
        /// typeId, amount, size
        /// </summary>
        ///
        private static List<(int, int, DroneSize, bool)> __droneBayItemList = null;
        private static List<(int, int, DroneSize, bool)> _droneBayItemList
        {
            get
            {
                if (__droneBayItemList == null)
                {
                    if (ESCache.Instance.DirectEve.ActiveShip.TypeId == (int)TypeID.Gila)
                    {
                        //__droneBayItemList = new List<(int, int, DroneSize, bool)>();
                        //__droneBayItemList.Add(_mediumDroneTypeId, 10, DroneSize, bool);
                        __droneBayItemList = new List<(int, int, DroneSize, bool)>
                        {
                            ((int)_mediumDroneTypeId, 10, DroneSize.Medium, false),
                            //((int)_smallDroneTypeId, 4, 1, false)
                        };
                    }
                    else if (ESCache.Instance.DirectEve.ActiveShip.TypeId == (int)TypeID.Ishtar)
                    {
                        //__droneBayItemList = new List<(int, int, int)>();
                        //__droneBayItemList.Add(_geckoDroneTypeId, 3, 3, false);
                        //__droneBayItemList.Add(_heavyDroneTypeId, 6, 3, false);
                        //__droneBayItemList.Add(_mediumDroneTypeId, 5, 2, false);
                        //__droneBayItemList.Add(_smallDroneTypeId, 5, 1, false);
                        __droneBayItemList = new List<(int, int, DroneSize, bool)>
                        {
                            ((int)_geckoDroneTypeId, 6, DroneSize.Gecko, false),
                            //((int)_heavyDroneTypeId, 6, 3, false),
                            ((int)_mediumDroneTypeId, 5, DroneSize.Medium, false),
                            ((int)_smallDroneTypeId, 5, DroneSize.Small, false),
                        };
                    }
                    else if (ESCache.Instance.DirectEve.ActiveShip.TypeId == (int)TypeID.VexorNavyIssue)
                    {
                        //__droneBayItemList = new List<(int, int, int, bool)>();
                        //__droneBayItemList.Add(_geckoDroneTypeId, 3, 3, false);
                        //__droneBayItemList.Add(_heavyDroneTypeId, 6, 3, false);
                        //__droneBayItemList.Add(_mediumDroneTypeId, 5, 2, false);
                        //__droneBayItemList.Add(_smallDroneTypeId, 5, 1, false);
                        __droneBayItemList = new List<(int, int, DroneSize, bool)>
                        {
                            ((int)_heavyDroneTypeId, 5, DroneSize.Large, false), //125m3
                            ((int)_mediumDroneTypeId, 7, DroneSize.Medium, false),//70m3
                            ((int)_smallDroneTypeId, 0, DroneSize.Small, false), //0m3
                        };
                    }
                    else if (ESCache.Instance.DirectEve.ActiveShip.TypeId == (int)TypeID.StormBringer)
                    {
                        //__droneBayItemList = new List<(int, int, int, bool)>();
                        //__droneBayItemList.Add(_geckoDroneTypeId, 3, 3, false);
                        //__droneBayItemList.Add(_heavyDroneTypeId, 6, 3, false);
                        //__droneBayItemList.Add(_mediumDroneTypeId, 5, 2, false);
                        //__droneBayItemList.Add(_smallDroneTypeId, 5, 1, false);
                        __droneBayItemList = new List<(int, int, DroneSize, bool)>
                        {
                            //((int)_geckoDroneTypeId, 6, 3, false),
                            //((int)_heavyDroneTypeId, 6, 3, false),
                            //((int)_mediumDroneTypeId, 5, 2, false),
                            //((int)_smallDroneTypeId, 5, 1, false),
                        };
                    }
                    else if (ESCache.Instance.DirectEve.ActiveShip.IsFrigOrDestroyerWithDroneBonuses) //worm, algos, dragoon
                    {
                        //__droneBayItemList = new List<(int, int, int, bool)>();
                        //__droneBayItemList.Add(_smallDroneTypeId, 5, 1, false);
                        __droneBayItemList = new List<(int, int, DroneSize, bool)>
                        {
                            ((int)_smallDroneTypeId, 5, DroneSize.Small, false),
                        };
                    }
                    else if (ESCache.Instance.DirectEve.ActiveShip.TypeId == (int)TypeID.Retribution)
                    {
                        //__droneBayItemList = new List<(int, int, int, bool)>();
                        //__droneBayItemList.Add(_smallDroneTypeId, 5, 1, false);
                        __droneBayItemList = new List<(int, int, DroneSize, bool)>
                        {
                        };
                    }
                    else if (ESCache.Instance.DirectEve.ActiveShip.TypeId == (int)TypeID.Vagabond || 30 > ESCache.Instance.ActiveShip.DroneBandwidth)
                    {
                        //__droneBayItemList = new List<(int, int, int, bool)>();
                        //__droneBayItemList.Add(_geckoDroneTypeId, 3, 3, false);
                        //__droneBayItemList.Add(_heavyDroneTypeId, 6, 3, false);
                        //__droneBayItemList.Add(_mediumDroneTypeId, 5, 2, false);
                        //__droneBayItemList.Add(_smallDroneTypeId, 5, 1, false);
                        __droneBayItemList = new List<(int, int, DroneSize, bool)>
                        {
                            //((int)_geckoDroneTypeId, 6, 3, false),
                            //((int)_heavyDroneTypeId, 6, 3, false),
                            //((int)_mediumDroneTypeId, 5, 2, false),
                            ((int) _smallDroneTypeId, 5, DroneSize.Small, false),
                        };
                    }
                    else if (ESCache.Instance.ActiveShip.HasDroneBay && ESCache.Instance.DirectEve.ActiveShip.GroupId == (int)Group.Cruiser)
                    {
                        //__droneBayItemList = new List<(int, int, int, bool)>();
                        //__droneBayItemList.Add(_smallDroneTypeId, 5, 1, false);
                        __droneBayItemList = new List<(int, int, DroneSize, bool)>
                        {
                            ((int) _mediumDroneTypeId, 15, DroneSize.Small, false),
                        };
                    }
                    else if (ESCache.Instance.ActiveShip.HasDroneBay && ESCache.Instance.DirectEve.ActiveShip.GroupId == (int)Group.Frigate || ESCache.Instance.DirectEve.ActiveShip.GroupId == (int)Group.Destroyer)
                    {
                        //__droneBayItemList = new List<(int, int, int, bool)>();
                        //__droneBayItemList.Add(_smallDroneTypeId, 5, 1, false);
                        __droneBayItemList = new List<(int, int, DroneSize, bool)>
                        {
                            ((int)_smallDroneTypeId, 5, DroneSize.Small, false),
                        };
                    }

                    //if (DirectEve.ActiveShip.TypeId == TypeID.)
                    //{
                    //    __droneBayItemList = new List<(int, int, int, bool)>();
                    //    __droneBayItemList.Add(_smallDroneTypeId, 5, 1, false);
                    //}
                }

                return __droneBayItemList ?? null;
            }
        }

        private static long _overThisSurveyValueGoSell = 200000000;

        private static int? _mtuTypeId = 33702; // 'Magpie' Mobile Tractor Unit

        private static string
            _homeStationBookmarkName =
                "station"; // this is the station we are starting with the abyss ship <-> this also means, that the safespot of the abyss starting point should be in that system to prevent gate jumps

        private static string
            _repairLocationBookmarkName =
                "repair"; // repair station bm, this can be the same as the homestation if it has a repair facility

        private static string _filamentSpotBookmarkName = "abyss"; // the spot where we open the filament

        //private string _buyStationBookmarkName = "buyStation"; // bookmark of the station where we are buying items from with the cloaky hauler

        private static string _surveyDumpBookmarkName = "surveyDump"; // bookmark of the station where we are dumping the survey data

        private DirectWorldPosition _activeShipPos =>
            ESCache.Instance.DirectEve.ActiveShip.Entity.DirectAbsolutePosition;

        private DateTime? _startedToRecallDronesWhileNoTargetsLeft = null;

        private static string AbyssalDeadspaceFilamentName = string.Empty;

        public List<int> _Tier0Filament = new List<int> { 56132, 56131, 56133, 56134, 56136 };
        public List<int> _Tier1Filament = new List<int> { 47761, 47762, 47763, 47764, 47765 };
        public List<int> _Tier2Filament = new List<int> { 47892, 47904, 47888, 47896, 47900 };
        public List<int> _Tier3Filament = new List<int> { 47893, 47905, 47889, 47897, 47901 };
        public List<int> _Tier4Filament = new List<int> { 47894, 47906, 47890, 47898, 47902 };
        public List<int> _Tier5Filament = new List<int> { 47895, 47907, 47891, 47899, 47903 };
        public List<int> _Tier6Filament = new List<int> { 56140, 56139, 56141, 56142, 56143 };

        public int AbyssalTier
        {
            get
            {
                if (_Tier0Filament.Contains(_filamentTypeId ?? 0))
                    return 0;

                if (_Tier1Filament.Contains(_filamentTypeId ?? 0))
                    return 1;

                if (_Tier2Filament.Contains(_filamentTypeId ?? 0))
                    return 2;

                if (_Tier3Filament.Contains(_filamentTypeId ?? 0))
                    return 3;

                if (_Tier4Filament.Contains(_filamentTypeId ?? 0))
                    return 4;

                if (_Tier5Filament.Contains(_filamentTypeId ?? 0))
                    return 5;

                if (_Tier6Filament.Contains(_filamentTypeId ?? 0))
                    return 6;

                return -1;
            }
        }


        public static int numOfFilamentsToBring = 1;
        //private static int _filamentTypeId = 47764; // gamma t1
        //private static int _filamentTypeId = 47900; // gamma t2
        //private static int _filamentTypeId = 47901; // gamma t3
        public static int? _filamentTypeId = 47889; // exotic t3
        //private static int _filamentTypeId = 47902; // gamma t4
        //private static int _filamentTypeId = 47903; // gamma t5
        //private static int _filamentTypeId = 56143; // gamma t6

        //private int _filamentTypeId = 47899; // fire t5

        //private static int _filamentTypeId = 56139; // elec t6
        private AbyssalRoomStrategy _roomStrategy { get; set; } = AbyssalRoomStrategy.Unspecified;

        public static bool _mtuAlreadyDroppedDuringThisStage { get; set; } = false;

        private DateTime _abyssalControllerStarted = DateTime.MinValue;



        private int _kikimoraTankThreshold
        {
            get
            {
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                {
                    return 6;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                {
                    return 6;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Vagabond)
                {
                    return 99;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                {
                    return 3;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.VexorNavyIssue)
                {
                    return 1;
                }

                return 1;
            }
        }

        private int _leshakTankThreshold
        {
            get
            {
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                {
                    return 9;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                {
                    return 9;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Vagabond)
                {
                    return 99;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                {
                    return 6;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.VexorNavyIssue)
                {
                    return 1;
                }

                return 1;
            }
        }

        private int _cruiserTankThreshold
        {
            get
            {
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                {
                    return 6;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                {
                    return 6;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Vagabond)
                {
                    return 99;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                {
                    return 6;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.VexorNavyIssue)
                {
                    return 4;
                }

                return 2;
            }
        }

        private int _droneBattleshipsTankThreshold
        {
            get
            {
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                {
                    return 1;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                {
                    return 1;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Vagabond)
                {
                    return 99;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                {
                    return 1;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.VexorNavyIssue)
                {
                    return 1;
                }

                return 1;
            }
        }

        private int _lucidDeepwatcherBattleshipsTankThreshold
        {
            get
            {
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                {
                    return 3;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                {
                    return 3;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Vagabond)
                {
                    return 99;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                {
                    return 2;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.VexorNavyIssue)
                {
                    return 1;
                }

                return 1;
            }
        }

        private int _marshalTankThreshold
        {
            get
            {
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                {
                    return 1;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                {
                    return 2;
                }

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Vagabond)
                    return 99;

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                {
                    return 1;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.VexorNavyIssue)
                {
                    return 1;
                }

                return 1;
            }
        }

        private int _bcTankthreshold
        {
            get
            {
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Ishtar)
                {
                    return 4;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                {
                    return 4;
                }

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Vagabond)
                    return 99;

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                {
                    return 3;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.VexorNavyIssue)
                {
                    return 0;
                }

                return 1;
            }
        }




        public AbyssalController()
        {
            //DirectEve.Log("++ Added OnSessionReadyHandler");
            _abyssalControllerStarted = DateTime.UtcNow;
            DirectSession.OnSessionReadyEvent += OnSessionReadyHandler;

            _keepAtRangeDistance = _keepAtRangeDistances[Rnd.Next(_keepAtRangeDistances.Count)];
            _enemyOrbitDistance = _enemyOrbitDistances[Rnd.Next(_enemyOrbitDistances.Count)];

            Log($"AbyssalController started at {_abyssalControllerStarted}. Selected the following random values for this session:");
            Log($"_keepAtRangeDistance: [{_keepAtRangeDistance}] _enemyOrbitDistance: [{_enemyOrbitDistance}] _gateMTUOrbitDistance: [{_gateMTUOrbitDistance}]");
            OnSessionReadyHandler(null, null);
        }

        internal bool AreAllDronesAttacking
        {
            get
            {
                if (!ESCache.Instance.ActiveShip.HasDroneBay)
                    return false;

                if (allDronesInSpace.All(d => d._directEntity.DroneState == (int)Drones.DroneState.Attacking) && allDronesInSpace.Any())
                    return true;

                return false;
            }

        }

        internal bool NoDroneBayAllNPCsInRange
        {
            get
            {
                if (ESCache.Instance.ActiveShip.HasDroneBay)
                    return false;

                if (Combat.PotentialCombatTargets.Any(e => e.IsInRangeOfWeapons))
                {
                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                    {
                        if (ESCache.Instance.Weapons.Any(w => w.Charge != null && w.ChargeName.Contains("ElectroPunch")))
                        {
                            return true;
                        }

                        return false;
                    }

                    return true;
                }

                return false;
            }
        }

        internal DirectBookmark myHomebookmark
        {
            get
            {
                if (ESCache.Instance.DirectEve.Bookmarks.Any())
                {
                    IOrderedEnumerable<DirectBookmark> myBookmarks = ESCache.Instance.DirectEve.Bookmarks.OrderByDescending(e => e.IsInCurrentSystem);
                    if (myBookmarks.Any())
                    {
                        var homebookmark = myBookmarks.FirstOrDefault(b => b.Title == _homeStationBookmarkName);
                        if (homebookmark != null)
                        {
                            return homebookmark;
                        }

                        return null;
                    }

                    return null;
                }

                return null;
            }
        }

        internal bool AreWeDockedInHomeSystem()
        {
            try
            {
                var hbm = ESCache.Instance.DirectEve.Bookmarks.OrderByDescending(e => e.IsInCurrentSystem).FirstOrDefault(b => b.Title == _homeStationBookmarkName);
                if (hbm != null)
                {
                    return hbm.DockedAtBookmark();
                }
                return false;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }

        private bool AreAllTargetsInASpeedCloud => Combat.PotentialCombatTargets.Where(e => !e.IsAbyssalDeadspaceTriglavianBioAdaptiveCache && !e.IsAbyssalDeadspaceTriglavianExtractionNode && !e.IsWreck).ToList().All(e => e._directEntity.IsInSpeedCloud);

        private bool AreAllFrigatesInASpeedCloud => Combat.PotentialCombatTargets.Where(e => !e.IsAbyssalDeadspaceTriglavianBioAdaptiveCache && !e.IsAbyssalDeadspaceTriglavianExtractionNode && !e.IsWreck && e.IsNPCFrigate).Any() && Combat.PotentialCombatTargets.Where(e => e.GroupId != 2009 && e.IsNPCFrigate).ToList().All(e => e._directEntity.IsInSpeedCloud);

        private bool AreAllOurDronesInASpeedCloud => Drones.AllDronesInSpace.Any() && Drones.AllDronesInSpace.All(e => e._directEntity.IsInSpeedCloud && e.Velocity > 2200);

        private bool AreWeCurrentlyAttackingAFrigate => (_groupTarget1OrSingleTarget?.IsNPCFrigate ?? false) || (_groupTarget2?.IsNPCFrigate ?? false);

        public string AbyssalWeather
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return string.Empty;

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Dark_Weather_1_30Percent))
                    return nameof(TypeID.Dark_Weather_1_30Percent);

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Dark_Weather_2_50Percent))
                    return nameof(TypeID.Dark_Weather_2_50Percent);

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Dark_Weather_3_70Percent))
                    return nameof(TypeID.Dark_Weather_3_70Percent);

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Electric_Weather_1_30Percent))
                    return nameof(TypeID.Electric_Weather_1_30Percent);

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Electric_Weather_2_50Percent))
                    return nameof(TypeID.Electric_Weather_2_50Percent);

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Electric_Weather_3_70Percent))
                    return nameof(TypeID.Electric_Weather_3_70Percent);

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Caustic_Weather_1_30Percent))
                    return nameof(TypeID.Caustic_Weather_1_30Percent);

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Caustic_Weather_2_50Percent))
                    return nameof(TypeID.Caustic_Weather_2_50Percent);

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Caustic_Weather_3_70Percent))
                    return nameof(TypeID.Caustic_Weather_3_70Percent);

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Xenon_Weather_1_30Percent))
                    return nameof(TypeID.Xenon_Weather_1_30Percent);

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Xenon_Weather_2_50Percent))
                    return nameof(TypeID.Xenon_Weather_2_50Percent);

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Xenon_Weather_3_70Percent))
                    return nameof(TypeID.Xenon_Weather_3_70Percent);

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Infernal_Weather_1_30Percent))
                    return nameof(TypeID.Infernal_Weather_1_30Percent);

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Infernal_Weather_2_50Percent))
                    return nameof(TypeID.Infernal_Weather_2_50Percent);

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Infernal_Weather_3_70Percent))
                    return nameof(TypeID.Infernal_Weather_3_70Percent);

                if (ESCache.Instance.EntitiesOnGrid.Any(i => i.TypeId == (int)TypeID.Basic_Weather))
                    return nameof(TypeID.Basic_Weather);

                return "None";
            }
        }

        private bool AreWeInsideOfAnyCloud => ESCache.Instance.ActiveShip.IsLocatedWithinBioluminescenceCloud || (ESCache.Instance.ActiveShip.IsLocatedWithinFilamentCloud && ESCache.Instance.ActiveShip.IsShieldTanked) || ESCache.Instance.ActiveShip.IsLocatedWithinSpeedCloud;

        private int _filaStackSize => _activeShip.IsFrigate ? 3 : _activeShip.IsDestroyer ? 2 : 1;

        private DirectActiveShip _activeShip => DirectEve.ActiveShip;

        private void UpdateStrategy()
        {
            if (!DirectEve.Interval(15000))
                return;

            //if (_roomStrategy != AbyssalRoomStrategy.Unspecified)
            //    return;

            if (!Combat.PotentialCombatTargets.Any())
            {
                _roomStrategy = AbyssalRoomStrategy.Normal;
                return;
            }

            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
            {
                //if (!Combat.PotentialCombatTargets.Any(e => e.IsNPCCruiser) && Combat.PotentialCombatTargets.Any(e => e._directEntity.IsAbyssalKaren))
                //{
                if (_roomStrategy != AbyssalRoomStrategy.Normal)
                {
                    _roomStrategy = AbyssalRoomStrategy.Normal;
                    Log($"-------- Selected strategy for current room: [{_roomStrategy}]");
                    return;
                }

                return;
                //}

                //Once we switch, do not switch back until we are not in the room anymore
            }

            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
            {
                if (DirectEve.Entities.Any(e => e.IsNPCBattleship && e.TypeName.Contains("Abyssal Overmind")))
                {
                    if (_roomStrategy != AbyssalRoomStrategy.Normal)
                    {
                        _roomStrategy = AbyssalRoomStrategy.Normal;
                        Log($"-------- Selected strategy for current room: [{_roomStrategy}]");
                        return;
                    }

                    return;
                }
            }

            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn ||
                AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn)
            {
                if (Combat.PotentialCombatTargets.Count(e => e.IsNPCCruiser && e.TypeName.Contains("Lucid")) >= 5)
                {
                    if (_roomStrategy != AbyssalRoomStrategy.Normal)
                    {
                        _roomStrategy = AbyssalRoomStrategy.Normal;
                        Log($"-------- Selected strategy for current room: [{_roomStrategy}]");
                        return;
                    }
                    return;
                }

                return;
            }

            if (_roomStrategy != AbyssalRoomStrategy.Normal)
            {
                _roomStrategy = AbyssalRoomStrategy.Normal;
                Log($"-------- Selected strategy for current room: [{_roomStrategy}]");
            }
        }

        public override void Dispose()
        {
            DirectEve.Log("-- Removed OnSessionReadyHandler");
            DirectSession.OnSessionReadyEvent -= OnSessionReadyHandler;
        }

        internal double GetSecondsToKillWithActiveDrones()
        {
            Dictionary<DirectDamageType, float> dict = new Dictionary<DirectDamageType, float>();
            foreach (var drone in allDronesInSpace.Select(d => new AbyssalDrone(d._directEntity)).ToList())
            {
                foreach (var kv in drone.GetInvType?.GetDroneDPS())
                {
                    if (dict.ContainsKey(kv.Key))
                    {
                        dict[kv.Key] += kv.Value;
                    }
                    else
                    {
                        dict.Add(kv.Key, kv.Value);
                    }
                }
            }

            var secondsToKillActiveDrones = Combat.PotentialCombatTargets.Sum(e => e.GetSecondsToKill(dict));
            return secondsToKillActiveDrones;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        public static int Global_HeatDamageMaximum_RepairModule = 50;
        public static int Global_HeatDamageMaximum_Hardener = 50;
        public static int Global_HeatDamageMaximum_PropMod = 50;
        public static bool AllowOverloadOfWeapons = true;
        public static bool AllowOverloadOfSpeedMod = true;
        public static bool AllowOverloadOfReps = true;
        public static bool AllowOverloadOfHardener { get; set; } = true;
        public static double HighRackHeatDisableThreshold = .60;
        public static double MedRackHeatDisableThreshold = .60;
        public static double LowRackHeatDisableThreshold = .60;

        public static void LoadSettings(XElement CharacterSettingsXml, XElement CommonSettingsXml)
        {
            Log("LoadSettings: AbyssalBehavior");
            //NumOfYellowBoxingAbyssalBCsToPullDrones =
            //    (int?)CharacterSettingsXml.Element("NumOfYellowBoxingAbyssalBCsToPullDrones") ??
            //    (int?)CommonSettingsXml.Element("NumOfYellowBoxingAbyssalBCsToPullDrones") ?? 2;
            //Log("AbyssalBehavior: NumOfYellowBoxingAbyssalBCsToPullDrones [" + NumOfYellowBoxingAbyssalBCsToPullDrones + "]");
            //NumOfYellowBoxingAbyssalFrigsToPullDrones =
            //    (int?)CharacterSettingsXml.Element("NumOfYellowBoxingAbyssalFrigsToPullDrones") ??
            //    (int?)CommonSettingsXml.Element("NumOfYellowBoxingAbyssalFrigsToPullDrones") ?? 6;
            //Log("AbyssalBehavior: NumOfYellowBoxingAbyssalBCsToPullDrones [" + NumOfYellowBoxingAbyssalBCsToPullDrones + "]");
            //NumOfYellowBoxingAbyssalScyllaTyrannosToPullDrones =
            //    (int?)CharacterSettingsXml.Element("NumOfYellowBoxingAbyssalScyllaTyrannosToPullDrones") ??
            //    (int?)CommonSettingsXml.Element("NumOfYellowBoxingAbyssalScyllaTyrannosToPullDrones") ?? 2;
            //Log("AbyssalBehavior: NumOfYellowBoxingAbyssalScyllaTyrannosToPullDrones [" + NumOfYellowBoxingAbyssalScyllaTyrannosToPullDrones + "]");
            //NumOfYellowBoxingAbyssalEphialtesCruisersToPullDrones =
            //     (int?)CharacterSettingsXml.Element("NumOfYellowBoxingAbyssalEphialtesCruisersToPullDrones") ??
            //     (int?)CommonSettingsXml.Element("NumOfYellowBoxingAbyssalEphialtesCruisersToPullDrones") ?? 2;
            //Log("AbyssalBehavior: NumOfYellowBoxingAbyssalEphialtesCruisersToPullDrones [" + NumOfYellowBoxingAbyssalEphialtesCruisersToPullDrones + "]");
            //NumOfYellowBoxingAbyssalNPCsToPullDrones =
            //    (int?)CharacterSettingsXml.Element("NumOfYellowBoxingAbyssalNPCsToPullDrones") ??
            //    (int?)CommonSettingsXml.Element("NumOfYellowBoxingAbyssalNPCsToPullDrones") ?? 6;
            //Log("AbyssalBehavior: NumOfYellowBoxingAbyssalNPCsToPullDrones [" + NumOfYellowBoxingAbyssalNPCsToPullDrones + "]");
            _overThisSurveyValueGoSell =
                (long?)CharacterSettingsXml.Element("OverThisSurveyValueGoSell") ?? (long?)CharacterSettingsXml.Element("OverThisSurveyValueGoSell") ??
                (long?)CommonSettingsXml.Element("OverThisSurveyValueGoSell") ?? (long?)CommonSettingsXml.Element("OverThisSurveyValueGoSell") ?? 200000000;
            _filamentSpotBookmarkName =
                (string)CharacterSettingsXml.Element("AbyssalDeadspaceBookmark") ?? (string)CharacterSettingsXml.Element("AbyssalDeadspaceBookmarks") ??
                (string)CommonSettingsXml.Element("AbyssalDeadspaceBookmark") ?? (string)CommonSettingsXml.Element("AbyssalDeadspaceBookmarks") ?? "abyss";
            Log("AbyssalBehavior: AbyssalDeadspaceBookmarkName [" + _filamentSpotBookmarkName + "] can have multiple bookmarks containing this: we will use them");
            _homeStationBookmarkName =
                (string)CharacterSettingsXml.Element("HomeBookmark") ?? (string)CommonSettingsXml.Element("HomeBookmark") ?? "station";
            Log("AbyssalBehavior: HomeBookmarkName [" + _homeStationBookmarkName + "]");
            _repairLocationBookmarkName =
                (string)CharacterSettingsXml.Element("repairLocationBookmarkName") ?? (string)CommonSettingsXml.Element("repairLocationBookmarkName") ?? "repair";
            Log("AbyssalBehavior: repairLocationBookmarkName [" + _repairLocationBookmarkName + "]");
            _surveyDumpBookmarkName =
                (string)CharacterSettingsXml.Element("surveyDumpBookmarkName") ?? (string)CommonSettingsXml.Element("surveyDumpBookmarkName") ?? "surveys";
            Log("AbyssalBehavior: surveyDumpBookmarkName [" + _surveyDumpBookmarkName + "]");
            _mtuTypeId =
                (int?)CharacterSettingsXml.Element("mtuTypeId") ?? (int?)CommonSettingsXml.Element("mtuTypeId") ?? 33702;
            Log("AbyssalBehavior: mtuTypeId [" + _mtuTypeId + "]");
            _filamentTypeId =
                (int?)CharacterSettingsXml.Element("filamentTypeId") ?? (int?)CommonSettingsXml.Element("filamentTypeId") ?? 47889;
            Log("AbyssalBehavior: filamentTypeId [" + _filamentTypeId + "]");

            numOfFilamentsToBring = (int?)CharacterSettingsXml.Element("numOfFilamentsToBring") ?? (int?)CommonSettingsXml.Element("numOfFilamentsToBring") ?? 1;
            Log("AbyssalBehavior: numOfFilamentsToBring [" + numOfFilamentsToBring + "]");

            _geckoDroneTypeId =
                (int?)CharacterSettingsXml.Element("geckoDroneTypeId") ??
                (int?)CommonSettingsXml.Element("geckoDroneTypeId") ?? 33681;
            Log("AbyssalBehavior: geckoDroneTypeId [" + _geckoDroneTypeId + "]");

            _karenDroneTypeId =
                (int?)CharacterSettingsXml.Element("karenDroneTypeId") ??
                (int?)CommonSettingsXml.Element("karenDroneTypeId") ?? 2478;
            Log("AbyssalBehavior: karenDroneTypeId [" + _karenDroneTypeId + "]");

            _heavyDroneTypeId =
                (int?)CharacterSettingsXml.Element("heavyDroneTypeId") ??
                (int?)CommonSettingsXml.Element("heavyDroneTypeId") ?? 2478;
            Log("AbyssalBehavior: heavyDroneTypeId [" + _heavyDroneTypeId + "]");

            _mediumDroneTypeId =
                (int?)CharacterSettingsXml.Element("mediumDroneTypeId") ??
                (int?)CommonSettingsXml.Element("mediumDroneTypeId") ?? 31874;
            Log("AbyssalBehavior: mediumDroneTypeId [" + _mediumDroneTypeId + "]");

            _smallDroneTypeId =
                (int?)CharacterSettingsXml.Element("smallDroneTypeId") ??
                (int?)CommonSettingsXml.Element("smallDroneTypeId") ?? 31872;
            Log("AbyssalBehavior: smallDroneTypeId [" + _smallDroneTypeId + "]");

            _naniteRepairPasteTypeId =
                (int?)CharacterSettingsXml.Element("naniteRepairPasteTypeId") ??
                (int?)CommonSettingsXml.Element("naniteRepairPasteTypeId") ?? 28668;
            Log("AbyssalBehavior: naniteRepairPasteTypeId [" + _naniteRepairPasteTypeId + "]");

            HealthCheckMinimumShieldPercentage =
                (int?)CharacterSettingsXml.Element("healthCheckMinimumShieldPercentage") ??
                (int?)CommonSettingsXml.Element("healthCheckMinimumShieldPercentage") ?? 65;
            Log("AbyssalBehavior: healthCheckMinimumShieldPercentage [" + HealthCheckMinimumShieldPercentage + "]");

            HealthCheckMinimumArmorPercentage =
                (int?)CharacterSettingsXml.Element("healthCheckMinimumArmorPercentage") ??
                (int?)CommonSettingsXml.Element("healthCheckMinimumArmorPercentage") ?? 65;
            Log("AbyssalBehavior: healthCheckMinimumArmorPercentage [" + HealthCheckMinimumArmorPercentage + "]");

            HealthCheckMinimumCapacitorPercentage =
                (int?)CharacterSettingsXml.Element("healthCheckMinimumCapacitorPercentage") ??
                (int?)CommonSettingsXml.Element("healthCheckMinimumCapacitorPercentage") ?? 35;
            Log("AbyssalBehavior: healthCheckMinimumCapacitorPercentage [" + HealthCheckMinimumCapacitorPercentage + "]");

            IgnoreConcord =
                (bool?)CharacterSettingsXml.Element("ignoreConcord") ??
                (bool?)CommonSettingsXml.Element("ignoreConcord") ?? false;

            Global_HeatDamageMaximum_RepairModule =
                (int?)CharacterSettingsXml.Element("heatDamageMaximum_RepairModule") ??
                (int?)CommonSettingsXml.Element("heatDamageMaximum_RepairModule") ?? 50;
            Log("AbyssalBehavior: heatDamageMaximum_RepairModule [" + Global_HeatDamageMaximum_RepairModule + "]");
            Global_HeatDamageMaximum_Hardener =
                (int?)CharacterSettingsXml.Element("heatDamageMaximum_Hardener") ??
                (int?)CommonSettingsXml.Element("heatDamageMaximum_Hardener") ?? 50;
            Log("AbyssalBehavior: heatDamageMaximum_Hardener [" + Global_HeatDamageMaximum_Hardener + "]");
            Global_HeatDamageMaximum_PropMod =
                (int?)CharacterSettingsXml.Element("heatDamageMaximum_Propmod") ??
                (int?)CommonSettingsXml.Element("heatDamageMaximum_Propmod") ?? 50;
            Log("AbyssalBehavior: heatDamageMaximum_Propmod [" + Global_HeatDamageMaximum_PropMod + "]");
            AllowOverloadOfHardener =
                (bool?)CharacterSettingsXml.Element("allowOverloadOfHardener") ??
                (bool?)CommonSettingsXml.Element("allowOverloadOfHardener") ?? true;
            Log("AbyssalBehavior: allowOverloadOfHardener [" + AllowOverloadOfHardener + "]");
            Defense.GlobalAllowOverLoadOfHardeners = AllowOverloadOfHardener;

            AllowOverloadOfReps =
                (bool?)CharacterSettingsXml.Element("allowOverloadOfReps") ??
                (bool?)CommonSettingsXml.Element("allowOverloadOfReps") ?? true;
            Log("AbyssalBehavior: allowOverloadOfReps [" + AllowOverloadOfReps + "]");
            Defense.GlobalAllowOverLoadOfReps = AllowOverloadOfReps;

            AllowOverloadOfSpeedMod =
                (bool?)CharacterSettingsXml.Element("allowOverloadOfSpeedMod") ??
                (bool?)CommonSettingsXml.Element("allowOverloadOfSpeedMod") ?? true;
            Log("AbyssalBehavior: allowOverloadOfSpeedMod [" + AllowOverloadOfSpeedMod + "]");
            Defense.GlobalAllowOverLoadOfSpeedMod = AllowOverloadOfSpeedMod;

            AllowOverloadOfWeapons =
                (bool?)CharacterSettingsXml.Element("allowOverloadOfWeapons") ??
                (bool?)CommonSettingsXml.Element("allowOverloadOfWeapons") ?? true;
            Log("AbyssalBehavior: allowOverloadOfWeapons [" + AllowOverloadOfWeapons + "]");
            //Defense.GlobalAllowOverLoadOfSpeedMod = AllowOverloadOfSpeedMod;

            HighRackHeatDisableThreshold =
                (double?)CharacterSettingsXml.Element("highRackHeatDisableThreshold") ??
                (double?)CommonSettingsXml.Element("highRackHeatDisableThreshold") ?? .60;
            Log("AbyssalBehavior: highRackHeatDisableThreshold [" + HighRackHeatDisableThreshold + "]");
            MedRackHeatDisableThreshold =
                (double?)CharacterSettingsXml.Element("medRackHeatDisableThreshold") ??
                (double?)CommonSettingsXml.Element("medRackHeatDisableThreshold") ?? .60;
            Log("AbyssalBehavior: medRackHeatDisableThreshold [" + MedRackHeatDisableThreshold + "]");
            LowRackHeatDisableThreshold =
                (double?)CharacterSettingsXml.Element("lowRackHeatDisableThreshold") ??
                (double?)CommonSettingsXml.Element("lowRackHeatDisableThreshold") ?? .60;
            Log("AbyssalBehavior: lowRackHeatDisableThreshold [" + LowRackHeatDisableThreshold + "]");
            _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage =
                (double?)CharacterSettingsXml.Element("drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage") ??
                (double?)CommonSettingsXml.Element("drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage") ?? .35;
            Log("AbyssalBehavior: drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage [" + _drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage + "]");
            logoffEveAfterThisManyAbyssalRuns =
                (int?)CharacterSettingsXml.Element("logoffEveAfterThisManyAbyssalRuns") ??
                (int?)CommonSettingsXml.Element("logoffEveAfterThisManyAbyssalRuns") ?? 40;
            Log("AbyssalBehavior: logoffEveAfterThisManyAbyssalRuns [" + logoffEveAfterThisManyAbyssalRuns + "]");

            XElement boostersToLoadIntoCargoXml = CharacterSettingsXml.Element("boosterTypesToLoadIntoCargo") ?? CommonSettingsXml.Element("boosterTypesToLoadIntoCargo");

            if (boostersToLoadIntoCargoXml != null)
                foreach (XElement boosterToLoadIntoCargo in boostersToLoadIntoCargoXml.Elements("boosterType"))
                {
                    try
                    {
                        long booster = int.Parse(boosterToLoadIntoCargo.Value);
                        DirectInvType boosterInvType = ESCache.Instance.DirectEve.GetInvType(int.Parse(boosterToLoadIntoCargo.Value));
                        Log("Adding booster [" + boosterInvType.TypeName + "] to the list of boosters that will be loaded into cargo");
                        BoosterTypesToLoadIntoCargo.Add(booster);
                    }
                    catch (Exception) { }
                }
            //    (int?)CharacterSettingsXml.Element("AbyssalDeadspaceFilamentsToStock") ??
            //    (int?)CommonSettingsXml.Element("AbyssalDeadspaceFilamentsToStock") ?? 1;
            //Log("AbyssalDeadspaceBehavior: AbyssalDeadspaceFilamentsToStock [" + AbyssalDeadspaceFilamentsToStock + "]");
            //HealthCheckMinimumShieldPercentage =
            //    (int?)CharacterSettingsXml.Element("HealthCheckMinimumShieldPercentage") ??
            //    (int?)CharacterSettingsXml.Element("healthCheckMinimumShieldPercentage") ??
            //    (int?)CommonSettingsXml.Element("HealthCheckMinimumShieldPercentage") ??
            //    (int?)CommonSettingsXml.Element("healthCheckMinimumShieldPercentage") ?? 30;
            //Log("AbyssalDeadspaceBehavior: HealthCheckMinimumShieldPercentage [" + HealthCheckMinimumShieldPercentage + "]");
            //HealthCheckMinimumArmorPercentage =
            //    (int?)CharacterSettingsXml.Element("HealthCheckMinimumArmorPercentage") ??
            //    (int?)CharacterSettingsXml.Element("healthCheckMinimumArmorPercentage") ??
            //    (int?)CommonSettingsXml.Element("HealthCheckMinimumArmorPercentage") ??
            //    (int?)CommonSettingsXml.Element("healthCheckMinimumArmorPercentage") ?? 30;
            //Log("AbyssalDeadspaceBehavior: HealthCheckMinimumArmorPercentage [" + HealthCheckMinimumArmorPercentage + "]");
            //HealthCheckMinimumCapacitorPercentage =
            //    (int?)CharacterSettingsXml.Element("HealthCheckMinimumCapacitorPercentage") ??
            //    (int?)CharacterSettingsXml.Element("healthCheckMinimumCapacitorPercentage") ??
            //    (int?)CommonSettingsXml.Element("HealthCheckMinimumCapacitorPercentage") ??
            //    (int?)CommonSettingsXml.Element("healthCheckMinimumCapacitorPercentage") ?? 30;
            //Log("AbyssalDeadspaceBehavior: HealthCheckMinimumCapacitorPercentage [" + HealthCheckMinimumCapacitorPercentage + "]");

            //AbyssalPocketWarningSeconds =
            //    (int?)CharacterSettingsXml.Element("abyssalPocketWarningSeconds") ??
            //    (int?)CommonSettingsXml.Element("abyssalPocketWarningSeconds") ?? 360;
            //Log("AbyssalDeadspaceBehavior: AbyssalPocketWarningSeconds [" + AbyssalPocketWarningSeconds + "]");

            //TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway =
            //    (bool?)CharacterSettingsXml.Element("TriglavianConstructionSiteSpawnFoundDozenPlusBSSpawn_RunAway") ??
            //    (bool?)CommonSettingsXml.Element("TriglavianConstructionSiteSpawnFoundDozenPlusBSSpawn_RunAway") ??
            //(bool?)CharacterSettingsXml.Element("abyssalConstructionSite14BSSpawnRunAway") ??
            //    (bool?)CommonSettingsXml.Element("abyssalConstructionSite14BSSpawnRunAway") ?? true;
            //Log("AbyssalDeadspaceBehavior: abyssalConstructionSite14BSSpawnRunAway [" + TriglavianConstructionSiteSpawnFoundDozenPlusBsSpawnRunAway + "]");
        }

        private void LoadSettings()
        {
            /**
            int GetFilamentTypeId()
            {
                switch (ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.FilamentType)
                {
                    case AbyssFilamentType.GammaT0:
                        return 56136;
                    case AbyssFilamentType.GammaT1:
                        return 47764;
                    case AbyssFilamentType.GammaT2:
                        return 47900;
                    case AbyssFilamentType.GammaT3:
                        return 47901;
                    case AbyssFilamentType.GammaT4:
                        return 47902;
                    case AbyssFilamentType.GammaT5:
                        return 47903;
                    case AbyssFilamentType.GammaT6:
                        return 56143;
                    case AbyssFilamentType.DarkT0:
                        return 56132;
                    case AbyssFilamentType.DarkT1:
                        return 47762;
                    case AbyssFilamentType.DarkT2:
                        return 47892;
                    case AbyssFilamentType.DarkT3:
                        return 47893;
                    case AbyssFilamentType.DarkT4:
                        return 47894;
                    case AbyssFilamentType.DarkT5:
                        return 47895;
                    case AbyssFilamentType.DarkT6:
                        return 56140;
                    case AbyssFilamentType.FirestormT0:
                        return 56134;
                    case AbyssFilamentType.FirestormT1:
                        return 47763;
                    case AbyssFilamentType.FirestormT2:
                        return 47896;
                    case AbyssFilamentType.FirestormT3:
                        return 47897;
                    case AbyssFilamentType.FirestormT4:
                        return 47898;
                    case AbyssFilamentType.FirestormT5:
                        return 47899;
                    case AbyssFilamentType.FirestormT6:
                        return 56142;
                    case AbyssFilamentType.ExoticT0:
                        return 56133;
                    case AbyssFilamentType.ExoticT1:
                        return 47761;
                    case AbyssFilamentType.ExoticT2:
                        return 47888;
                    case AbyssFilamentType.ExoticT3:
                        return 47889;
                    case AbyssFilamentType.ExoticT4:
                        return 47890;
                    case AbyssFilamentType.ExoticT5:
                        return 47891;
                    case AbyssFilamentType.ExoticT6:
                        return 56141;
                    case AbyssFilamentType.ElectricalT0:
                        return 56131;
                    case AbyssFilamentType.ElectricalT1:
                        return 47765;
                    case AbyssFilamentType.ElectricalT2:
                        return 47904;
                    case AbyssFilamentType.ElectricalT3:
                        return 47905;
                    case AbyssFilamentType.ElectricalT4:
                        return 47906;
                    case AbyssFilamentType.ElectricalT5:
                        return 47907;
                    case AbyssFilamentType.ElectricalT6:
                        return 56139;
                }
                return 0;
            }
            **/

            /**
            int GetMTUTypeId()
            {
                switch (ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.MTUType)
                {
                    case MTUType.Standard:
                        return 33475;
                    case MTUType.Packrat:
                        return 33700;
                    case MTUType.Magpie:
                        return 33702;
                }

                return 0;
            }
            **/

            //_filamentTypeId = GetFilamentTypeId();
            //_shipTypeId = ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.ShipType == AbyssShipType.Ishtar ? 12005
            //    : ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.ShipType == AbyssShipType.Vexor ? 626
            //    : ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.ShipType == AbyssShipType.Gila ? 17715
            //    : ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.ShipType == AbyssShipType.Algos ? 32872
            //    : ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.ShipType == AbyssShipType.Worm ? 17930
            //    : ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.ShipType == AbyssShipType.Tristan ? 593
            //    : -1;
            //_homeSystemId = ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.HomeSystemId;
            //_maxVelocity = ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.MaxSpeedWithPropMod;
            //_maxDroneRange = ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.MaxDroneRange;
            //_maxGigaJoulePerSecondTank =
            //    ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.GigajoulePerSecExcess;
            //_ammoTypeId = ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.AmmoTypeId;
            //_mtuTypeId = GetMTUTypeId();
            //_weaponMaxRange = ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.WeaponMaxRange;

            //_homeStationBookmarkName = ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.HomeStationBookmarkName;
            //_repairLocationBookmarkName = ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.RepairStationBookmarkName;
            //_filamentSpotBookmarkName = ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.AbyssalBookmarkName;
            //_surveyDumpBookmarkName = ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.SurveyDumpStationBookmarkName;
            //_bcTankthreshold = ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.BCTankthreshold;
            //_kikimoraTankThreshold = ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.KikimoraTankThreshold;
            //_marshalTankThreshold = ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.MarshalTankThreshold;


            //_boosterList = new List<(int, int)>();
            //foreach (var booster in ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.Boosters)
            //{
            //    _boosterList.Add((booster.TypeId, booster.Amount));
            //}

            /**
            _droneBayItemList = new List<(int, int, int)>();
            foreach (var drone in ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.DroneBayItems)
            {
                int getDroneSize(DroneSize size)
                {
                    switch (size)
                    {
                        case DroneSize.Small:
                            _smallDroneTypeId = drone.TypeId;
                            return 1;
                        case DroneSize.Medium:
                            _mediumDroneTypeId = drone.TypeId;
                            return 2;
                        case DroneSize.Large:
                            if (drone.TypeId != _geckoDroneTypeId)
                                _heavyDroneTypeId = drone.TypeId;
                            return 3;
                    }

                    return 0;
                }

                var size = getDroneSize(drone.Type);
                _droneBayItemList.Add((drone.TypeId, drone.Amount, size));
            }
            **/

            //var attr =
            //    $"_filementTypeId {_filementTypeId} _shipTypeId {_shipTypeId} _homeSystemId {_homeSystemId} _maxVelocity {_maxVelocity} _maxDroneRange {_maxDroneRange} _maxGigaJoulePerSecondTank {_maxGigaJoulePerSecondTank} _ammoTypeId{_ammoTypeId} _mtuTypeId {_mtuTypeId} _weaponMaxRange {_weaponMaxRange}";

            //var droneLog = String.Join(" - ", _droneBayItemList.Select(a => $"[{a.Item1}, {a.Item2}, {a.Item3}]"));
            //var boosterLog = String.Join(" - ", _boosterList.Select(a => $"[{a.Item1}, {a.Item2}]"));
            //Log($"DroneLog [{droneLog}]");
            //Log($"BoosterLog [{boosterLog}]");
            //Log($"Settings [{attr}]");
        }

        private void OnSessionReadyHandler(object source, EventArgs args)
        {
            LoadSettings();

            DirectEve.Log("OnSessionReadyHandler proc.");
            _mtuAlreadyDroppedDuringThisStage = false;
            _startedToRecallDronesWhileNoTargetsLeft = null;
            _currentStageMaximumEhp = 0;
            _currentStageCurrentEhp = 0;
            _droneEngageCount = 0;
            _safeSpotNearGate = null;
            _safeSpotNearGateChecked = false;
            _moveToOverride = null;
            _droneRecallsStage = 0;
            _droneRecallsStage2 = 0;
            _mtuScoopAttempts = 0;
            _sessionChangeIdleCheck = false;
            _lastDroneInOptimal = null;
            _dronesInOptimalStage = 0;
            _attemptsToJumpFrigateDestroyerAbyss = 0;
            _moveDirection = MoveDirection.None;
            //_shipWasCloseToTheBoundaryAndEnemiesInOptimalDuringThisStage = false;
            _droneRecallTimers = new Dictionary<long, DateTime>();
            _moveBackwardsDirection = null;
            _printDroneEstimatedKillTime = false;
            DirectEntity.InvalidateCache();
            _currentTargetsStage.Clear();
            _roomStrategy = AbyssalRoomStrategy.Unspecified;

            _limboDeployingAbyssalDrones = new List<AbyssalDrone>();
            _recentlyDeployedDronesTimer = new Dictionary<long, DateTime>();
            _limboDeployingDronesTimer = new Dictionary<long, DateTime>();
            //_alreadyLootedItemIds = new HashSet<long>();
            //_alreadyLootedItems = new List<string>();
            _activationErrorTickCount = 0;
            _droneRecallsDueEnemiesBeingInASpeedCloud = 0;
            _nextDroneRecallDueEnemiesBeingInASpeedCloud = DateTime.UtcNow.AddSeconds(Rnd.Next(35, 45));
            _skipWaitOrca = false;
            _forceOverheatPVP = false;
            _leftInvulnAfterAbyssState = false;
            //_minSecondsToLaunchDronesAfterSessionChange = Rnd.Next(9, 10);
            //_abandoningDrones = false;
            _droneDPSUpdate = false;

            _dronesRecalledWhileWeStillhaveTargets = 0;
            //_leftInvulnState = false;
            //_skipWaitOrca = false;
            //_forceOverheatPVP = false;
        }

        public static int HealthCheckMinimumShieldPercentage = 30;
        public static int HealthCheckMinimumArmorPercentage = 30;
        public static int HealthCheckMinimumCapacitorPercentage = 30;

        public static bool IgnoreConcord = false;
        private int _neutsOnGridCount => Combat.PotentialCombatTargets.Count(e => e._directEntity.IsNeutingEntity);

        private int _marshalsOnGridCount => Combat.PotentialCombatTargets.Count(e => e._directEntity.IsAbyssalMarshal);
        private int _leshaksOnGridCount => Combat.PotentialCombatTargets.Count(e => e._directEntity.IsAbyssalLeshak);
        private int _karenOnGridCount => Combat.PotentialCombatTargets.Count(e => e._directEntity.IsAbyssalKaren || e._directEntity.IsDrifterBSSpawn);
        private int _droneBattleshipsOnGridCount => Combat.PotentialCombatTargets.Count(e => e._directEntity.IsAbyssalDroneBattleship);
        private int _lucidDeepwatcherBattleshipsOnGridCount => Combat.PotentialCombatTargets.Count(e => e._directEntity.IsAbyssalLucidDeepwatcherBattleship);

        /// <summary>
        /// Min (_maxDroneRange, _maxTargetRange)
        /// </summary>
        private double _maxRange
        {
            get
            {
                if (ESCache.Instance.ActiveShip.HasDroneBay)
                {
                    return Math.Min((double)ESCache.Instance.ActiveShip.GetDroneControlRange(), _maxTargetRange);
                }

                return (double)Combat.MaxWeaponRange;
            }
        }

        private List<EntityCache> _targetsOngrid = null;

        internal List<EntityCache> TargetsOnGrid
        {
            get
            {
                if (!DirectEve.HasFrameChanged() && _targetsOngrid != null)
                    return _targetsOngrid;

                _targetsOngrid = Combat.PotentialCombatTargets.OrderBy(e => e._directEntity.AbyssalTargetPriority).ThenBy(x => x.Id).ToList();
                return _targetsOngrid;
            }
        }


        internal bool AnyRemoteRepairNonFrigOnGrid => Combat.PotentialCombatTargets.Any(e => e._directEntity.IsRemoteRepairEntity && !e.IsNPCFrigate);

        internal bool AnyRemoteRepairOngrid => Combat.PotentialCombatTargets.Any(e => e._directEntity.IsRemoteRepairEntity);
        internal bool IsCloseToSafeSpotNearGate => SafeSpotNearGate != null && SafeSpotNearGate.GetDistance(_activeShipPos) < 6500;

        internal bool _printDroneEstimatedKillTime;

        private List<string> _kitedEntitiesAsFrig = new List<string>() { "kikimora", "cynabal", "dramiel", "tessera", "devoted knight", "devoted hunter" };

        private bool AnyEntityOnGridToBeKitedAsFrigSizedShip
        {
            get
            {
                if (!ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsDestroyer)
                    return false;

                foreach (var name in _kitedEntitiesAsFrig)
                {
                    if (TargetsOnGrid.Any(e => e.TypeName.ToLower().Contains(name)))
                        return true;
                }
                return false;
            }
        }

        private List<EntityCache> EntitiesToBeKitedAsFrig =>
            TargetsOnGrid.Where(e => _kitedEntitiesAsFrig.Any(x => e.TypeName.ToLower() == x)).ToList();


        private int MTUSpeed
        {
            get
            {
                //switch (ESCache.Instance.EveAccount.ClientSetting.AbyssalMainSetting.MTUType)
                //{
                //    case MTUType.Standard:
                //        return 1000;
                //    case MTUType.Packrat:
                //        return 1250;
                //    case MTUType.Magpie:
                //        return 1500;
                //}
                return 1000;
            }
        }

        internal double _secondsNeededToRetrieveWrecks =>
            (Combat.PotentialCombatTargets.Where(e => IsEntityWeWantToLoot(e._directEntity)).Concat(ESCache.Instance.Wrecks.Where(e => !e.IsWreckEmpty))
                .Sum(e => e.Distance) / MTUSpeed) + Combat.PotentialCombatTargets.Where(e => IsEntityWeWantToLoot(e._directEntity))
                .Concat(ESCache.Instance.Wrecks.Where(e => !e.IsWreckEmpty && e.IsAbyssalCacheWreck)).Count() * 8;

        internal bool _majorityOnNPCsAreAgressingCurrentShip
        {
            get
            {

                if (Combat.PotentialCombatTargets.Any( i => !i.IsLargeCollidable && i.IsAttacking))
                {
                    if (Combat.PotentialCombatTargets.Count(e => e.IsTargetedBy && e.IsAttacking && !e._directEntity.IsRemoteRepairEntity) >= Combat.PotentialCombatTargets.Count(i => !i._directEntity.IsRemoteRepairEntity) * 0.51)
                    {
                        return true;
                    }

                    return false;
                }

                return false;
            }
        }


        internal int DronesInOptimalCount()
        {
            var drones = allDronesInSpace;
            var r = 0;
            foreach (var drone in drones)
            {
                if (drone.FollowId <= 0)
                    continue;

                var targetId = drone.FollowId;

                var target = Combat.PotentialCombatTargets.FirstOrDefault(e => e.Id == targetId)._directEntity;

                if (target == null)
                    continue;

                if (drone._directEntity.IsInOptimalRangeTo(target))
                    r++;
            }

            return r;
        }

        internal void PrintDroneEstimatedKillTimePerStage()
        {
            if (_printDroneEstimatedKillTime)
                return;

            if (!allDronesInSpace.Any())
                return;

            if (_abyssStatEntry == null)
                return;

            switch (CurrentAbyssalStage)
            {
                case AbyssalStage.Stage1:

                    if (String.IsNullOrEmpty(_abyssStatEntry.Room1Dump))
                        return;
                    _printDroneEstimatedKillTime = true;
                    break;
                case AbyssalStage.Stage2:

                    if (String.IsNullOrEmpty(_abyssStatEntry.Room1Dump))
                        return;
                    _printDroneEstimatedKillTime = true;
                    break;
                case AbyssalStage.Stage3:

                    if (String.IsNullOrEmpty(_abyssStatEntry.Room1Dump))
                        return;
                    _printDroneEstimatedKillTime = true;
                    break;
            }

            if (_printDroneEstimatedKillTime)
            {
                Dictionary<DirectDamageType, float> dict = new Dictionary<DirectDamageType, float>();

                foreach (var drone in allDronesInSpace)
                {
                    foreach (var kv in drone._directEntity.GetDroneDPS())
                    {
                        if (dict.ContainsKey(kv.Key))
                        {
                            dict[kv.Key] += kv.Value;
                        }
                        else
                        {
                            dict.Add(kv.Key, kv.Value);
                        }
                    }
                }

                Log($"Estimated time to kill all targets in this room (with drones) [{EntityCache.GetSecondsToKill(dict, Combat.PotentialCombatTargets)}] seconds.");
            }
        }


        private bool _doOnce;
        private bool _droneDPSUpdate = false;

        internal void DoOnceOnStartup()
        {
            if (!_doOnce)
            {
                _doOnce = true;
                if (DirectEve.Me.IsInAbyssalSpace())
                {
                    //_mtuAlreadyDroppedDuringThisStage = true;
                    Log(
                        $"Retrieving current stage from the eve account due a crash/restart. Stage [{ESCache.Instance.EveAccount.AbyssStage}]");
                    // if we start in abyss space we retrieve the stage id from the corresponding eve acc
                    switch (ESCache.Instance.EveAccount.AbyssStage)
                    {
                        case 2:
                            _attemptsToJumpMidgate++; // this is the only case we need
                            break;
                        default:
                            break;
                    }
                }
            }
        }


        private bool _anyOverheat
        {
            get
            {
                if (overHeatReps)
                    return true;

                if (overHeatHardener)
                    return true;

                if (overHeatPropmod)
                    return true;

                if (overHeatCapacitorInjector)
                    return true;

                if (boolOverLoadWeapons)
                    return true;

                return false;
            }
        }

        internal double heatDamageMaximum_RepairModule
        {
            get
            {
                if (Global_HeatDamageMaximum_RepairModule != 0)
                    return Global_HeatDamageMaximum_RepairModule;

                if (ESCache.Instance.ActiveShip == null)
                {
                    return 50;
                }

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Ishtar)
                    return 55;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.StormBringer)
                    return 55;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Vagabond)
                    return 55;

                //low skill?
                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Gila)
                    return 50;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Worm)
                    return 50;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Algos)
                    return 50;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Dragoon)
                    return 50;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Retribution)
                    return 40;

                return 50;
            }
        }

        internal double heatDamageMaximum_Hardener
        {
            get
            {
                if (Global_HeatDamageMaximum_Hardener != 0)
                    return Global_HeatDamageMaximum_Hardener;

                if (ESCache.Instance.ActiveShip == null)
                {
                    return 50;
                }

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Ishtar)
                    return 55;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.StormBringer)
                    return 55;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Vagabond)
                    return 55;

                //low skill?
                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Gila)
                    return 50;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Worm)
                    return 50;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Algos)
                    return 50;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Dragoon)
                    return 50;

                return 50;
            }
        }

        internal double heatDamageMaximum_Propmod
        {
            get
            {
                if (Global_HeatDamageMaximum_PropMod != 0)
                    return Global_HeatDamageMaximum_PropMod;

                if (ESCache.Instance.ActiveShip == null)
                {
                    return 50;
                }

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Ishtar)
                    return 55;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.StormBringer)
                    return 55;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Vagabond)
                    return 55;

                //low skill?
                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Gila)
                    return 50;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Worm)
                    return 50;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Algos)
                    return 50;

                if (ESCache.Instance.ActiveShip.Entity.TypeId == (int)TypeID.Dragoon)
                    return 50;

                return 50;
            }
        }

        internal bool? _overHeatReps;
        internal bool overHeatReps
        {
            get
            {
                if (!AllowOverloadOfReps)
                    return false;

                if (DirectEve.HasFrameChanged())
                {
                    if (DirectEve.Modules.Where(i => i.IsShieldRepairModule || i.IsArmorRepairModule).Any(e => e.HeatDamagePercent >= heatDamageMaximum_RepairModule))
                    {
                        if (DebugConfig.DebugOverLoadReps || DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (DirectEve.Modules.Any(e => e.HeatDamagePercent >= heatDamageMaximum))");
                        return false;
                    }

                    if (DirectEve.Modules.Any(i => i.IsShieldRepairModule) && ESCache.Instance.Modules.Where(i => i.IsMidSlotModule).Any(e => e._module.HeatDamagePercent >= heatDamageMaximum_RepairModule))
                    {
                        if (DebugConfig.DebugOverLoadReps || DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (DirectEve.Modules.Any(i => i.IsShieldRepairModule) && ESCache.Instance.Modules.Where(i => i._module.IsPassiveModule && i.IsMidSlotModule).Any(e => e._module.HeatDamagePercent >= heatDamageMaximum_RepairModule))");
                        return false;
                    }

                    if (DirectEve.Modules.Any(i => i.IsArmorRepairModule) && ESCache.Instance.Modules.Where(i => i.IsLowSlotModule).Any(e => e._module.HeatDamagePercent >= heatDamageMaximum_RepairModule))
                    {
                        if (DebugConfig.DebugOverLoadReps || DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (DirectEve.Modules.Any(i => i.IsArmorRepairModule) && ESCache.Instance.Modules.Where(i => i._module.IsPassiveModule && i.IsLowSlotModule).Any(e => e._module.HeatDamagePercent >= heatDamageMaximum_RepairModule))");
                        return false;
                    }

                    if (ESCache.Instance.EveAccount.UseFleetMgr && 80 > ESCache.Instance.ActiveShip.ShieldPercentage && Combat.PotentialCombatTargets.Count(i => i.IsAttacking && 15000 > i.Distance && i.TypeName.Contains("Lucifer Cynabal")) >= 2)
                    {
                        if (DebugConfig.DebugOverLoadReps || DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (ESCache.Instance.EveAccount.UseFleetMgr && Combat.PotentialCombatTargets.Count(i => i.IsAttacking && 15000 > i.Distance && i.TypeName.Contains(\"Lucifer Cynabal\")) >= 2)");
                        _overHeatReps = true;
                        return (bool)_overHeatReps;
                    }

                    // Are we actively tanking a lot?
                    if (ESCache.Instance.ActiveShip.IsShieldTanked && DirectActiveShip.PastTwentyPulsesShieldArmorStrucValues.Count(d => d.Item1 < 0.35) >= 3)
                    {
                        if (DebugConfig.DebugOverLoadReps || DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (DirectActiveShip.PastTwentyPulsesShieldArmorStrucValues.Count(d => d.Item1 < 0.35) >= 3)");
                        _overHeatReps = true;
                        return (bool)_overHeatReps;
                    }

                    if (ESCache.Instance.ActiveShip.IsArmorTanked && DirectActiveShip.PastTwentyPulsesShieldArmorStrucValues.Count(d => d.Item2 < 0.45) >= 3)
                    {
                        if (DebugConfig.DebugOverLoadReps || DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (DirectActiveShip.PastTwentyPulsesShieldArmorStrucValues.Count(d => d.Item1 < 0.35) >= 3)");
                        _overHeatReps = true;
                        return (bool)_overHeatReps;
                    }

                    if (ESCache.Instance.EntitiesNotSelf.Any(e => e.IsPlayer && e.IsAttacking))
                    {
                        Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (ESCache.Instance.EntitiesNotSelf.Any(e => e.IsPlayer && e.IsAttacking))");
                        _overHeatReps = true;
                        return (bool)_overHeatReps;
                    }

                    _overHeatReps = false;
                    return (bool)_overHeatReps;
                }

                return _overHeatReps ?? false;
            }
        }

        internal bool overHeatCapacitorInjector
        {
            get
            {
                if (DirectEve.Modules.Any(i => i.IsShieldRepairModule) && ESCache.Instance.Modules.Where(i => i.IsMidSlotModule).Any(e => e._module.HeatDamagePercent >= heatDamageMaximum_RepairModule))
                {
                    if (DebugConfig.DebugOverLoadReps || DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (DirectEve.Modules.Any(i => i.IsShieldRepairModule) && ESCache.Instance.Modules.Where(i => i._module.IsPassiveModule && i.IsMidSlotModule).Any(e => e._module.HeatDamagePercent >= heatDamageMaximum_RepairModule))");
                    return false;
                }

                if (ESCache.Instance.Modules.Any(i => i.GroupId == (int)Group.CapacitorInjector && i.IsActive))
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Lucifer Cynabal")))
                            return true;

                        return false;
                    }

                    return false;
                }

                return false;
            }
        }

        internal bool? _overHeatHardener;
        internal bool overHeatHardener
        {
            get
            {
                if (!AllowOverloadOfHardener)
                    return false;

                if (DirectEve.HasFrameChanged())
                {
                    var Hardeners = ESCache.Instance.Modules.Where(e => e.IsActivatable && e.IsShieldHardener || e.IsArmorHardener);

                    foreach (var item in Hardeners)
                    {
                        if (item.IsMidSlotModule && ESCache.Instance.Modules.Where(i => i.IsMidSlotModule).Any(e => e._module.HeatDamagePercent >= heatDamageMaximum_Hardener))
                        {
                            if (DebugConfig.DebugOverLoadReps || DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (item.IsMidSlotModule && ESCache.Instance.Modules.Where(i => i.IsMidSlotModule).Any(e => e._module.HeatDamagePercent >= heatDamageMaximum_Hardener))");
                            return false;
                        }

                        if (item.IsLowSlotModule && ESCache.Instance.Modules.Where(i => i.IsLowSlotModule).Any(e => e._module.HeatDamagePercent >= heatDamageMaximum_Hardener))
                        {
                            if (DebugConfig.DebugOverLoadReps || DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (DirectEve.Modules.Any(i => i.IsArmorRepairModule) && ESCache.Instance.Modules.Where(i => i._module.IsPassiveModule && i.IsLowSlotModule).Any(e => e._module.HeatDamagePercent >= heatDamageMaximum_RepairModule))");
                            return false;
                        }

                        if (item._module.HeatDamagePercent > heatDamageMaximum_Hardener - 5)
                        {
                            if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (item.HeatDamagePercent < heatDamageMaximum - 5) return false");
                            _overHeatHardener = false;
                            return _overHeatHardener ?? false;
                        }
                    }

                    if (DirectEve.Modules.Any(e => e.HeatDamagePercent >= heatDamageMaximum_Hardener))
                    {
                        if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (DirectEve.Modules.Any(e => e.HeatDamagePercent] >= heatDamageMaximum_Hardener [" + heatDamageMaximum_Hardener + "])) return false");
                        _overHeatHardener = false;
                        return _overHeatHardener ?? false;
                    }

                    if (!IsOurShipWithintheAbyssBounds())
                    {
                        if (DirectEve.Interval(10000)) if (DebugConfig.DebugOverLoadHardeners || DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (!IsOurShipWithintheAbyssBounds()) return false");
                        _overHeatHardener = true;
                        return _overHeatHardener ?? true;
                    }

                    if (AllRatsHaveBeenCleared && AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)
                    {
                        if (DirectEve.Interval(10000)) if (DebugConfig.DebugOverLoadHardeners || DebugConfig.DebugOverLoadModules) Log("if (AllRatsHaveBeenCleared) return false");
                        if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure)) return false");
                        _overHeatHardener = false;
                        return _overHeatHardener ?? false;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                        {
                            if (30 > Time.Instance.SecondsSinceLastSessionChange)
                            {
                                if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] 30 > _secondsSinceLastSessionChange [" + Math.Round(Time.Instance.SecondsSinceLastSessionChange, 0) + "] return true");
                                _overHeatHardener = true;
                                return _overHeatHardener ?? true;
                            }

                            if (420 > ESCache.Instance.ActiveShip.Entity.Velocity)
                            {
                                if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] 420 > My Velocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "] return true");
                                _overHeatHardener = true;
                                return _overHeatHardener ?? true;
                            }

                            if (1 > ESCache.Instance.ActiveShip.Entity.ArmorPct)
                            {
                                if (ESCache.Instance.Modules.Any(i => i.IsShieldHardener && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] 1 > ArmorPct [" + Math.Round(ESCache.Instance.ActiveShip.Entity.ArmorPct, 2) + "] return true");
                                _overHeatHardener = true;
                                return _overHeatHardener ?? true;
                            }

                            if (.5 > ESCache.Instance.ActiveShip.Entity.ShieldPct)
                            {
                                if (ESCache.Instance.Modules.Any(i => i.IsShieldHardener && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] .5 > ShieldPct [" + Math.Round(ESCache.Instance.ActiveShip.Entity.ShieldPct, 2) + "] return true");
                                _overHeatHardener = true;
                                return _overHeatHardener ?? true;
                            }

                            if (.25 > ESCache.Instance.ActiveShip.Entity.ArmorPct)
                            {
                                if (ESCache.Instance.Modules.Any(i => i.IsArmorHardener && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] .25 > ArmorPct [" + Math.Round(ESCache.Instance.ActiveShip.Entity.ArmorPct, 2) + "] return true");
                                _overHeatHardener = true;
                                return _overHeatHardener ?? true;
                            }

                            if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 7)
                            {
                                if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]  PotentialCombatTargets.Count(i => i.IsNPCCruiser) [" + Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) + "] >= 7) return true");
                                _overHeatHardener = true;
                                return _overHeatHardener ?? true;
                            }
                        }
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 3)
                        {
                            if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] PotentialCombatTargets.Count(i => i.IsNPCBattleship) [" + Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) + "] >= 4 return true");
                            _overHeatHardener = true;
                            return _overHeatHardener ?? true;
                        }

                        if (420 > ESCache.Instance.ActiveShip.Entity.Velocity)
                        {
                            if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] 420 > My Velocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "] return true");
                            _overHeatHardener = true;
                            return _overHeatHardener ?? true;
                        }
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                    {
                        if (420 > ESCache.Instance.ActiveShip.Entity.Velocity)
                        {
                            if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] 420 > My Velocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "] return true");
                            _overHeatHardener = true;
                            return _overHeatHardener ?? true;
                        }
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Vedmak")) >= 2)
                        {
                            if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] PotentialCombatTargets.Count(i => i.TypeName.Contains(Vedmak)) [" + Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Vedmak")) + "] >= 2");
                            _overHeatHardener = true;
                            return _overHeatHardener ?? true;
                        }
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser && 25000 > i.Distance) >= 4)
                        {
                            if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) [" + Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) + "] >= 4");
                            _overHeatHardener = true;
                            return _overHeatHardener ?? true;
                        }

                        if (420 > ESCache.Instance.ActiveShip.Entity.Velocity)
                        {
                            if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] 420 > My Velocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "] return true");
                            _overHeatHardener = true;
                            return _overHeatHardener ?? true;
                        }
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Count(i => i.IsDestroyer && 30000 > i.Distance) >= 8)
                        {
                            if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) [" + Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) + "] >= 4");
                            _overHeatHardener = true;
                            return _overHeatHardener ?? true;
                        }
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn)
                    {
                        if (ESCache.Instance.EveAccount.UseFleetMgr && Combat.PotentialCombatTargets.Count(i => i.Name.ToLower().Contains("Devoted Knight".ToLower())) >= 2)
                        {
                            if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] PotentialCombatTargets.Any(i => i.TypeName.Contains(Devoted Knight)) [" + Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Devoted Knight")) + "]");
                            _overHeatHardener = true;
                            return _overHeatHardener ?? true;
                        }

                        if (Combat.PotentialCombatTargets.Count(i => i.Name.ToLower().Contains("Devoted Knight".ToLower())) >= 3)
                        {
                            if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] PotentialCombatTargets.Any(i => i.TypeName.Contains(Devoted Knight)) [" + Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Devoted Knight")) + "]");
                            _overHeatHardener = true;
                            return _overHeatHardener ?? true;
                        }
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                    {
                        int intFrigLanceThreshold = 15;
                        if (ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate) intFrigLanceThreshold = 2;

                        if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate && i.TypeName.Contains("lance")) >= intFrigLanceThreshold)
                        {
                            if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] PotentialCombatTargets.Count(i => i.TypeName.Contains(lance)) [" + Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("lance")) + "] >= [" + intFrigLanceThreshold + "]");
                            _overHeatHardener = true;
                            return _overHeatHardener ?? true;
                        }
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                    {
                        if (ESCache.Instance.EveAccount.UseFleetMgr)
                        {
                            if (60 > Time.Instance.SecondsSinceLastSessionChange && ESCache.Instance.EveAccount.IsLeader)
                            {
                                if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] 60 > _secondsSinceLastSessionChange [" + Math.Round(Time.Instance.SecondsSinceLastSessionChange, 0) + "]");
                                _overHeatHardener = true;
                                return _overHeatHardener ?? true;
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.IsTargetedBy) && 85 > ESCache.Instance.ActiveShip.ShieldPercentage)
                            {
                                if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Lucifer Cynabal")) >= 2)
                                {
                                    if (ESCache.Instance.Modules.Any(i => i.IsShieldHardener && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] PotentialCombatTargets.Count(i => i.TypeName.Contains(Lucifer Cynabal)) [" + Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Lucifer Cynabal")) + "] >= 3");
                                    _overHeatHardener = true;
                                    return _overHeatHardener ?? true;
                                }
                            }

                            _overHeatHardener = false;
                            return _overHeatHardener ?? false;
                        }

                        if (60 > Time.Instance.SecondsSinceLastSessionChange)
                        {
                            if (ESCache.Instance.Modules.Any(i => i.IsShieldHardener && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] 60 > _secondsSinceLastSessionChange [" + Math.Round(Time.Instance.SecondsSinceLastSessionChange, 0) + "]");
                            _overHeatHardener = true;
                            return _overHeatHardener ?? true;
                        }

                        if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Lucifer Cynabal")) > 3)
                        {
                            if (ESCache.Instance.Modules.Any(i => i.IsShieldHardener && !i.IsOverloaded)) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] PotentialCombatTargets.Count(i => i.TypeName.Contains(Lucifer Cynabal)) [" + Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Lucifer Cynabal")) + "] >= 3");
                            _overHeatHardener = true;
                            return _overHeatHardener ?? true;
                        }
                    }

                    if (ESCache.Instance.Entities.Any(e => e.IsPlayer && e.IsAttacking)) //|| _forceOverheatPVP)
                    {
                        if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && !i.IsOverloaded)) Log("if (ESCache.Instance.Entities.Any(e => e.IsPlayer && e.IsAttacking))");
                        _overHeatHardener = true;
                        return _overHeatHardener ?? true;
                    }

                    // if we calculated at least 3 occasions of being below 35% shield in the past 20 pulses (10 seconds)
                    if (DirectActiveShip.PastTwentyPulsesShieldArmorStrucValues.Count(d => d.Item1 < 0.35) >= 2)
                    {
                        if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && !i.IsOverloaded)) Log("if (DirectActiveShip.PastTwentyPulsesShieldArmorStrucValues.Count(d => d.Item1 < 0.35) >= 2)");
                        _overHeatHardener = true;
                        return _overHeatHardener ?? true;
                    }

                    if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && i.IsOverloaded)) Log("return false");

                    _overHeatHardener = false;
                    return _overHeatHardener ?? false;
                }

                //do not set _overHeatHardener here, we want the cached value ffs!
                //_overHeatHardener = false;
                return _overHeatHardener ?? false;
            }
        }

        internal bool? _overHeatPropmod;

        internal bool overHeatPropmod
        {
            get
            {
                if (!AllowOverloadOfSpeedMod)
                    return false;

                if (DirectEve.HasFrameChanged())
                {
                    // not if we aren't in the abyss space
                    if (!ESCache.Instance.DirectEve.Me.IsInAbyssalSpace())
                    {
                        if (DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (!DirectEve.Me.IsInAbyssalSpace()) return false");
                        _overHeatPropmod = false;
                        return (bool)_overHeatPropmod;
                    }

                    if (ESCache.Instance.Modules.Any(e => e.IsMidSlotModule && e._module.HeatDamagePercent >= heatDamageMaximum_Propmod))
                    {
                        if (DebugConfig.DebugOverLoadModules)
                        {
                            foreach (var damagedModule in ESCache.Instance.Modules.Where(e => e.IsMidSlotModule && e._module.HeatDamagePercent >= heatDamageMaximum_Propmod))
                            {
                                Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] damagedModule [" + damagedModule.TypeName + "] HeatDamagePercent [" + damagedModule._module.HeatDamagePercent + "] >= heatDamageMaximum [" + heatDamageMaximum_Propmod + "] return false");
                            }
                        }

                        return false;
                    }

                    if (ESCache.Instance.Modules.Any(e => e.IsMidSlotModule && e._module.IsAfterburner && !e.IsOverloaded && e._module.HeatDamagePercent >= heatDamageMaximum_Propmod -.10))
                    {
                        foreach (var damagedSpeedMod in ESCache.Instance.Modules.Where(e => e.IsMidSlotModule && e._module.IsAfterburner && !e.IsOverloaded && e._module.HeatDamagePercent >= heatDamageMaximum_Propmod - .10))
                        {
                            Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] damagedModule [" + damagedSpeedMod.TypeName + "] HeatDamagePercent [" + damagedSpeedMod._module.HeatDamagePercent + "] >= heatDamageMaximum [" + heatDamageMaximum_Propmod + "] return false");
                        }

                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.MedHeatRackState() >= MedRackHeatDisableThreshold)
                    {
                        if (DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] MedHeatRackState [" + ESCache.Instance.ActiveShip.MedHeatRackState() + "] >= MedRackHeatDisableThreshold [" + MedRackHeatDisableThreshold + "] return false");
                        return false;
                    }

                    // not if we are webbed
                    //if (Combat.PotentialCombatTargets.Any(e => e.IsWebbingMe))
                    //{
                    //    _overHeatPropmod = false;
                    //    return (bool)_overHeatPropmod;
                    //}

                    //This will cycle the overheat of the propmod on and then off
                    if (ESCache.Instance.Modules.Any(e => e.GroupId == (int)Group.Afterburner && e.IsOverloaded)) //&& !IsSituationThatNecessitatesWeUseOurUniqueOrbitRoutine)
                    {
                        if (DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (DirectEve.Modules.Any(e => e.GroupId == (int)Group.Afterburner && e.IsOverloaded)) return false");
                        _overHeatPropmod = false;
                        return (bool)_overHeatPropmod;
                    }

                    // not if any other module is being repaired
                    if (ESCache.Instance.Modules.Any(e => e._module.IsBeingRepaired))
                    {
                        if (DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (DirectEve.Modules.Any(e => e.IsBeingRepaired)) return false");
                        _overHeatPropmod = false;
                        return (bool)_overHeatPropmod;
                    }

                    if (ESCache.Instance.InAbyssalDeadspace)
                    {
                        // Do not overheat while near the safe spot near gate
                        if (IsCloseToSafeSpotNearGate && !IsSituationThatNecessitatesWeUseOurUniqueOrbitRoutine)
                        {
                            if (DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (IsCloseToSafeSpotNearGate) return false");
                            _overHeatPropmod = false;
                            return (bool)_overHeatPropmod;
                        }

                        // not if we are close to the gate / mtu
                        if (10000 > ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition.GetDistance(_nextGate._directEntity.DirectAbsolutePosition) && AllRatsHaveBeenCleared)
                        {
                            if (DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if we are within 10k of the gate return false");
                            _overHeatPropmod = false;
                            return (bool)_overHeatPropmod;
                        }

                        if (_getMTUInSpace != null && 10000 > ESCache.Instance.ActiveShip.Entity.DirectAbsolutePosition.GetDistance(_getMTUInSpace._directEntity.DirectAbsolutePosition) && IsItSafeToMoveToMTUOrLootWrecks)
                        {
                            if (DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if we are within 10k of the MTU return false");
                            _overHeatPropmod = false;
                            return (bool)_overHeatPropmod;
                        }

                        if (DirectEve.Modules.Any(i => i.HeatDamagePercent + 5 >= heatDamageMaximum_Propmod))
                        {
                            if (DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] if (DirectEve.Modules.Any(i => i.HeatDamagePercent + 5 >= heatDamageMaximum_Propmod)) return false");
                            _overHeatPropmod = false;
                            return (bool)_overHeatPropmod;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 3)
                            {
                                _overHeatPropmod = true;
                                return (bool)_overHeatPropmod;
                            }
                        }

                        if (ESCache.Instance.EveAccount.UseFleetMgr && AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser && i.Name.Contains("Devoted Knight")) >= 2)
                            {
                                _overHeatPropmod = true;
                                return (bool)_overHeatPropmod;
                            }
                        }

                        if (ESCache.Instance.EveAccount.UseFleetMgr && AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Lucifer Cynabal") && i.IsTarget && i.IsReadyToShoot && i.IsAttacking))
                            {
                                _overHeatPropmod = true;
                                return (bool)_overHeatPropmod;
                            }

                            if (Combat.PotentialCombatTargets.Any(i => i.Name.Contains("Lucifer Dramiel") && i.IsTarget && i.IsReadyToShoot && i.IsAttacking))
                            {
                                _overHeatPropmod = true;
                                return (bool)_overHeatPropmod;
                            }
                        }

                        if (ESCache.Instance.EveAccount.UseFleetMgr && AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattlecruiser) > _bcTankthreshold)
                            {
                                _overHeatPropmod = true;
                                return (bool)_overHeatPropmod;
                            }
                        }

                        if (ESCache.Instance.EveAccount.IsLeader && ESCache.Instance.ActiveShip.HasMicroWarpDrive && Combat.PotentialCombatTargets.Any(i => i._directEntity.IsWarpScramblingEntity))
                        {
                            if (28 > Time.Instance.SecondsSinceLastSessionChange)
                            {
                                _overHeatPropmod = true;
                                return (bool)_overHeatPropmod;
                            }
                        }

                        // if moving to the gate would take longer than killing
                        if (_moveDirection == MoveDirection.Gate && _secondsNeededToReachTheGate >= GetEstimatedStageRemainingTimeToClearGrid())
                        {
                            _overHeatPropmod = true;
                            return (bool)_overHeatPropmod;
                        }

                        // if there are more than 2 neuts
                        if (_neutsOnGridCount > 2 && _moveDirection == MoveDirection.AwayFromEnemy)
                        {
                            _overHeatPropmod = true;
                            return (bool)_overHeatPropmod;
                        }

                        // if there is more than 1 marshal
                        if (_marshalsOnGridCount > 1 && _moveDirection == MoveDirection.AwayFromEnemy)
                        {
                            _overHeatPropmod = true;
                            return (bool)_overHeatPropmod;
                        }

                        if (DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] no reason to overheat return false");
                        _overHeatPropmod = false;
                        return (bool)_overHeatPropmod;
                    }

                    if (DebugConfig.DebugOverLoadModules) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "] in k-space return true");
                    _overHeatPropmod = true;
                    return (bool)_overHeatPropmod;
                }

                return _overHeatPropmod ?? false;
            }
        }

        internal bool IsItSafeToRepairModules
        {
            get
            {
                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                        return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                        return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) >= 2)
                        return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate) >= 12)
                        return false;
                }

                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                    return false;

                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                    return false;

                if (Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser) > 6)
                    return false;

                if (Combat.PotentialCombatTargets.Count() > 16)
                    return false;

                if (DirectEve.Modules.Any(i => i.IsOverloaded || i.IsOverloadLimboState))
                    return false;

                return true;
            }
        }

        internal bool ManageOverheat()
        {
            if (DebugConfig.DebugAbyssalDeadspaceBehavior || DebugConfig.DebugOverLoadHardeners) Log("ManageOverheat");

            if (!ESCache.Instance.InSpace)
                return false;

            if (!ESCache.Instance.Modules.Any())
                return true;

            // not if we aren't in the abyss space
            if (!ESCache.Instance.DirectEve.Me.IsInAbyssalSpace())
                return false;


            var passiveModuleDamagePercentMax = 75;
            var passiveModuleStartRepairPercent = 1;

            var propMods = ESCache.Instance.Modules.Where(e => e.GroupId == (int)Group.Afterburner);
            var capacitorInjector = ESCache.Instance.Modules.Where(e => e.GroupId == (int)Group.CapacitorInjector);
            var Hardeners = ESCache.Instance.Modules.Where(e => e.IsShieldHardener); //|| e.IsArmorHardener); //Armor Hardeners are passive and cant be overloaded!
            var repairers = ESCache.Instance.Modules.Where(e => e.GroupId == (int)Group.ShieldBoosters || e.GroupId == (int)Group.ArmorRepairer);
            var passiveModules = ESCache.Instance.Modules.Where(e => e._module.IsPassiveModule)
                .OrderByDescending(m => m._module.HeatDamagePercent); // passive modules
            var modules = propMods.Concat(Hardeners).Concat(repairers).Concat(capacitorInjector);
            var passiveModuleHighestDamagePercent =
                passiveModules.Any() ? passiveModules.FirstOrDefault()._module.HeatDamagePercent : 0;
            var passiveModuleThresholdExceeded = passiveModuleHighestDamagePercent > passiveModuleDamagePercentMax;


            if (overHeatReps && repairers.Any(e => e._module.IsBeingRepaired))
            {
                if (DebugConfig.DebugOverLoadHardeners) Log("if (overHeatBooster && shieldBooster.Any(e => e.IsBeingRepaired))");
                foreach (var item in repairers.Where(e => e._module.IsBeingRepaired))
                {
                    if (item._module.CancelRepair())
                    {
                        Log($"Canceling repair of a shieldBooster [{item.TypeName}] Id [{item.ItemId}]");
                        item._module.CancelRepair();
                        continue;
                    }
                }
            }

            if (overHeatHardener && Hardeners.Any(e => e._module.IsBeingRepaired))
            {
                if (DebugConfig.DebugOverLoadHardeners) Log("if (overHeatHardener && shieldHardener.Any(e => e.IsBeingRepaired))");
                foreach (var item in Hardeners.Where(e => e._module.IsBeingRepaired))
                {
                    if (item._module.CancelRepair())
                    {
                        Log($"Canceling repair of a hardener [{item.TypeName}] Id [{item.ItemId}]");
                        item._module.CancelRepair();
                        continue;
                    }
                }
            }

            if (overHeatPropmod && propMods.Any(e => e._module.IsBeingRepaired))
            {
                if (DebugConfig.DebugOverLoadHardeners) Log("if (overHeatPropmod && afterBurner.Any(e => e.IsBeingRepaired))");
                foreach (var item in propMods.Where(e => e._module.IsBeingRepaired))
                {
                    if (item._module.CancelRepair())
                    {
                        Log($"Canceling repair of a propMod [{item.TypeName}] Id [{item.ItemId}]");
                        item._module.CancelRepair();
                        continue;
                    }
                }
            }

            var shipsCargo = ESCache.Instance.DirectEve.GetShipsCargo();

            if (shipsCargo != null)
            {
                if (shipsCargo.Items.Any(i => i.TypeId == _naniteRepairPasteTypeId))
                {
                    if (passiveModuleHighestDamagePercent >= passiveModuleStartRepairPercent && passiveModules.Any())
                    {
                        // try to repair the passive modules regardless of any other action, because it's a passive module. the repair will be canceled if it receives any heat damage

                        foreach (var passiveModule in passiveModules)
                        {
                            //if (mod.GroupId != 61) // atm only repair cap battery passive modules
                            //    continue;

                            if (!passiveModule._module.IsBeingRepaired && !passiveModule.IsInLimboState && passiveModule._module.HeatDamagePercent >= passiveModuleStartRepairPercent)
                            {
                                if (!ShouldWeRepair)
                                    continue;

                                if (passiveModule._module.Repair())
                                {
                                    Log($"Repairing [{passiveModule.TypeName}] Id [{passiveModule.ItemId}]");
                                    continue;
                                }
                            }
                        }
                    }
                }
            }

            foreach (var hardener in Hardeners)
            {

                //fixme
                if ((hardener._module.HeatDamagePercent > heatDamageMaximum_Hardener || passiveModuleThresholdExceeded) &&
                    hardener.IsOverloaded && hardener.ToggleOverload())
                {
                    Log(
                        $"Disabling overload on [{hardener.TypeName}] Heatdamage is too high! heatDamageMaximum_Hardener [{heatDamageMaximum_Hardener}] Current [{hardener._module.HeatDamagePercent}]");
                    return true;
                }

                if (modules.Any(e => e.ItemId != hardener.ItemId && e._module.HeatDamagePercent > heatDamageMaximum_RepairModule) &&
                    hardener.IsOverloaded && hardener.ToggleOverload())
                {
                    var damagedModule = modules.FirstOrDefault(e => e._module.HeatDamagePercent > heatDamageMaximum_RepairModule);
                    Log($"Disabling overload on [" + hardener.TypeName + "] Heatdamage is too high on other module [" + damagedModule.TypeName + "]! heatDamageMaximum_RepairModule [" + heatDamageMaximum_RepairModule + "] Current [" + damagedModule._module.HeatDamagePercent + "]");
                    return true;
                }
            }

            foreach (var repairer in repairers)
            {

                //fixme
                if ((repairer._module.HeatDamagePercent > heatDamageMaximum_RepairModule || passiveModuleThresholdExceeded) &&
                    repairer.IsOverloaded && repairer.ToggleOverload())
                {
                    Log(
                        $"Disabling overload on [{repairer.TypeName}] Heatdamage is too high! Max [{heatDamageMaximum_RepairModule}] Current [{repairer._module.HeatDamagePercent}]");
                    return true;
                }

                if (modules.Any(e => e.ItemId != repairer.ItemId && e._module.HeatDamagePercent > heatDamageMaximum_RepairModule) &&
                    repairer.IsOverloaded && repairer.ToggleOverload())
                {
                    var damagedModule = modules.FirstOrDefault(e => e._module.HeatDamagePercent > heatDamageMaximum_RepairModule);
                    Log($"Disabling overload on [" + repairer.TypeName + "] Heatdamage is too high on other module [" + damagedModule.TypeName + "]! Max [" + heatDamageMaximum_RepairModule + "] Current [" + damagedModule._module.HeatDamagePercent + "]");
                    return true;
                }
            }

            foreach (var propmod in propMods)
            {

                //fixme
                if ((propmod._module.HeatDamagePercent > heatDamageMaximum_Propmod || passiveModuleThresholdExceeded) &&
                    propmod.IsOverloaded && propmod.ToggleOverload())
                {
                    Log(
                        $"Disabling overload on [{propmod.TypeName}] Heatdamage is too high! Max [{heatDamageMaximum_RepairModule}] Current [{propmod._module.HeatDamagePercent}]");
                    return true;
                }

                if (modules.Any(e => e.ItemId != propmod.ItemId && e._module.HeatDamagePercent > heatDamageMaximum_RepairModule) &&
                    propmod.IsOverloaded && propmod.ToggleOverload())
                {
                    var damagedModule = modules.FirstOrDefault(e => e._module.HeatDamagePercent > heatDamageMaximum_RepairModule);
                    Log($"Disabling overload on [" + propmod.TypeName + "] Heatdamage is too high on other module [" + damagedModule.TypeName + "]! Max [" + heatDamageMaximum_RepairModule + "] Current [" + damagedModule._module.HeatDamagePercent + "]");
                    return true;
                }
            }

            // if a passive module is damaged too much, prevent any overheat operation
            if (passiveModuleThresholdExceeded)
            {
                if (DirectEve.Interval(10000))
                    Log($"passiveModuleHighestDamagePercent [" + passiveModuleHighestDamagePercent + "] > passiveModuleDamagePercentMax [" + passiveModuleDamagePercentMax + "]! Can't overheat anything.");
                return false;
            }

            if (overHeatHardener)
            {
                if (DebugConfig.DebugOverLoadHardeners) Log("if (overHeatHardener)");
                foreach (var item in Hardeners)
                {
                    if (!item.IsOverloaded)
                    {
                        if (DebugConfig.DebugOverLoadHardeners) Log("if (!item.IsOverloaded)");
                        if (item._module.HeatDamagePercent < heatDamageMaximum_RepairModule - 5)
                        {
                            if (DebugConfig.DebugOverLoadHardeners) Log("if (item.HeatDamagePercent < heatDamageMaximum - 5)");
                            if (item.ToggleOverload())
                            {
                                Log($"Overheating hardener [{item.TypeName}] Heatdamage [{Math.Round(item._module.HeatDamagePercent, 0)}] IsActive [" + item.IsActive + "]");
                                continue;
                            }
                            else Log($"Overheating hardener [{item.TypeName}] Heatdamage [{Math.Round(item._module.HeatDamagePercent, 0)}] Overload failed?!");

                            continue;
                        }

                        if (DirectEve.Interval(5000)) Log($"Overheating hardener [{item.TypeName}] Heatdamage [{Math.Round(item._module.HeatDamagePercent, 0)}] > heatDamageMaximum_RepairModule [" + heatDamageMaximum_RepairModule + "] - 5% Not overheating");
                        continue;
                    }

                    continue;
                }
            }

            if (overHeatCapacitorInjector)
            {
                if (DebugConfig.DebugOverLoadHardeners) Log("if (overHeatHardener)");
                foreach (var item in capacitorInjector)
                {
                    if (!item.IsOverloaded)
                    {
                        if (DebugConfig.DebugOverLoadHardeners) Log("if (!item.IsOverloaded)");
                        if (item._module.HeatDamagePercent < heatDamageMaximum_RepairModule - 5)
                        {
                            if (DebugConfig.DebugOverLoadHardeners) Log("if (item.HeatDamagePercent < heatDamageMaximum - 5)");
                            if (item.ToggleOverload())
                            {
                                Log($"Overheating capacitorInjector [{item.TypeName}] Heatdamage [{Math.Round(item._module.HeatDamagePercent, 0)}] IsActive [" + item.IsActive + "]");
                                continue;
                            }
                            else Log($"Overheating capacitorInjector [{item.TypeName}] Heatdamage [{Math.Round(item._module.HeatDamagePercent, 0)}] Overload failed?!");

                            continue;
                        }

                        if (DirectEve.Interval(5000)) Log($"Overheating capacitorInjector [{item.TypeName}] Heatdamage [{Math.Round(item._module.HeatDamagePercent, 0)}] Damage too high! Not overheating");
                        continue;
                    }

                    continue;
                }
            }

            if (overHeatReps)
                foreach (var item in repairers)
                {
                    if (!item.IsOverloaded)
                    {
                        if (item._module.HeatDamagePercent < heatDamageMaximum_RepairModule - 5)
                        {
                            if (item.ToggleOverload())
                            {
                                Log($"Overheating booster [{item.TypeName}] Heatdamage [{Math.Round(item._module.HeatDamagePercent, 0)}] IsActive [" + item.IsActive + "]");
                                continue;
                            }

                            continue;
                        }

                        Log($"Overheating booster [{item.TypeName}] Heatdamage [{Math.Round(item._module.HeatDamagePercent, 0)}] Damage too high! Not overheating");
                        continue;
                    }

                    continue;
                }

            if (overHeatPropmod)
                foreach (var item in propMods)
                {
                    if (!item.IsOverloaded)
                    {
                        if (item._module.HeatDamagePercent < heatDamageMaximum_Propmod - 7)
                        {
                            if (item.ToggleOverload())
                            {
                                Log($"Overheating speedmod [{item.TypeName}] Heatdamage [{Math.Round(item._module.HeatDamagePercent, 0)}] overHeatPropmod [" + overHeatPropmod + "] IsActive [" + item.IsActive + "]");
                                continue;
                            }

                            continue;
                        }

                        Log($"Overheating speedmod [{item.TypeName}] Heatdamage [{Math.Round(item._module.HeatDamagePercent, 0)}] overHeatPropmod [" + overHeatPropmod + "] Damage too high! Not overheating");
                        continue;
                    }

                    continue;
                }

            // disable overheat
            if (!overHeatPropmod)
                foreach (var item in propMods)
                {
                    // add date time checks
                    if (item.IsOverloaded && item.ToggleOverload())
                    {
                        Log($"Disabling overload on [{item.TypeName}] Heatdamage [{Math.Round(item._module.HeatDamagePercent, 0)}] overHeatPropmod [" + overHeatPropmod + "]");
                        return true;
                    }
                }

            if (!overHeatHardener)
            {
                if (DebugConfig.DebugOverLoadHardeners) Log("if (!overHeatHardener)");
                foreach (var item in Hardeners)
                {
                    if (item.IsOverloaded && item.ToggleOverload())
                    {
                        Log($"Disabling overload on [{item.TypeName}] Heatdamage [{Math.Round(item._module.HeatDamagePercent, 0)}] overHeatHardener [" + overHeatHardener + "]");
                        return true;
                    }
                }
            }

            if (!overHeatCapacitorInjector)
            {
                if (DebugConfig.DebugOverLoadHardeners) Log("if (!overHeatHardener)");
                foreach (var item in capacitorInjector)
                {
                    if (item.IsOverloaded && item.ToggleOverload())
                    {
                        Log($"Disabling overload on [{item.TypeName}] Heatdamage [{Math.Round(item._module.HeatDamagePercent, 0)}] overHeatHardener [" + overHeatHardener + "]");
                        return true;
                    }
                }
            }

            if (!overHeatReps)
                foreach (var item in repairers)
                {
                    if (item.IsOverloaded && _nextOverheatDisableShieldBooster < DateTime.UtcNow && item.ToggleOverload())
                    {
                        Log($"Disabling overload on [{item.TypeName}] Heatdamage [{Math.Round(item._module.HeatDamagePercent, 0)}] overHeatBooster [" + overHeatReps + "]");
                        return true;
                    }
                }


            return false;
        }

        internal bool boolWeNeedIncreaseDamageBoostersNow
        {
            get
            {
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                            return true;

                        return false;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                    {
                        //if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                        //    return true;

                        return false;
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn)
                    {
                        //if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                        //    return true;

                        return false;
                    }

                    return false;
                }

                return true;
            }
        }

        internal bool boolWeNeedBoostersNow
        {
            get
            {
                if (ESCache.Instance.InSpace)
                {
                    if (ESCache.Instance.InAbyssalDeadspace)
                    {
                        if (ESCache.Instance.ActiveShip.IsShieldTanked)
                        {
                            if (_drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage > ESCache.Instance.ActiveShip.Entity.ShieldPct)
                                return true;
                        }

                        if (ESCache.Instance.ActiveShip.IsArmorTanked)
                        {
                            if (_drugsBoostersTakeWhenShieldsOrArmorBelowThisPercentage > ESCache.Instance.ActiveShip.Entity.ArmorPct)
                                return true;
                        }

                        if (ESCache.Instance.EveAccount.UseFleetMgr && !ESCache.Instance.EveAccount.IsLeader && 3 >= Combat.PotentialCombatTargets.Count(i => i.IsAttacking))
                            return false;

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                        {
                            if (!ESCache.Instance.EveAccount.UseFleetMgr)
                            {
                                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                                    return false;

                                return true;
                            }
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                        {
                            if (!ESCache.Instance.EveAccount.UseFleetMgr)
                            {
                                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                                    return false;

                                return true;
                            }
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                        {
                            if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                                return false;

                            return true;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                        {
                            if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                                return false;

                            return true;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                        {
                            int intFrigLanceThreshold = 15;
                            if (ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate) intFrigLanceThreshold = 2;

                            if (Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate && i.TypeName.Contains("lance")) >= intFrigLanceThreshold)
                            {
                                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                                    return false;

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

        private Dictionary<int, List<DateTime>> _boosterHistory = new Dictionary<int, List<DateTime>>();
        private bool _boosterFailedState = false;

        internal bool ManageDrugs()
        {
            if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("ManageDrugs");

            if (DebugConfig.DebugDisableDrugsBoosters)
                return true;

            if (ESCache.Instance.ActiveShip == null || ESCache.Instance.ActiveShip.Entity == null)
                return true;

            if (ESCache.Instance.ActiveShip.IsPod)
                return true;

            //
            // Every time we enter AbyssalDeadspace use these: no need to wait for special conditions
            //
            if (ESCache.Instance.InSpace && ESCache.Instance.InAbyssalDeadspace && boolWeNeedIncreaseDamageBoostersNow && DateTime.UtcNow > _lastDrugUsage.AddSeconds(2))
            {
                var shipsCargo = ESCache.Instance.DirectEve.GetShipsCargo();
                if (shipsCargo == null)
                    return false;

                foreach (var typeId in _increaseDamageBoostersList.Select(b => b.Item1))
                {
                    if (DirectEve.Interval(5000, 6000, typeId.ToString()))
                    {
                        // check if booster is already being used
                        if (ESCache.Instance.DirectEve.Me.Boosters.Any(b => b.TypeID == typeId))
                        {
                            if (DebugConfig.DebugBoosters && DirectEve.Interval(10000, 12000, typeId.ToString()))
                                Log($"Skipping to load booster, already loaded. TypeId [{typeId}]!");
                            continue;
                        }

                        var boosterItem = shipsCargo.Items.FirstOrDefault(i => i.TypeId == typeId);
                        if (boosterItem != null)
                        {
                            if (_boosterHistory.TryGetValue(typeId, out var b) && b.Count(e => e > DateTime.UtcNow.AddMinutes(-60)) >= 3)
                            {
                                Log($"Prevented to consume booster [{boosterItem.TypeName}] We already tried to consume it 3 times the past 60 minutes.");
                                _boosterFailedState = true;
                                return false;
                            }

                            _lastDrugUsage = DateTime.UtcNow;
                            if (boosterItem.ConsumeBooster())
                            {
                                Log($"Consumed booster [{boosterItem.TypeName}]!");

                            }
                            else
                            {
                                string msg = "ManageDrugs: Failed to use booster [" + boosterItem.TypeName + "]";
                                DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                                Log(msg);
                                if (PlayNotificationSounds) Util.PlayNoticeSound();
                                if (PlayNotificationSounds) Util.PlayNoticeSound();
                                if (PlayNotificationSounds) Util.PlayNoticeSound();
                                if (PlayNotificationSounds) Util.PlayNoticeSound();
                            }

                            if (_boosterHistory.TryGetValue(typeId, out var bo))
                            {
                                bo.Add(DateTime.UtcNow);
                            }
                            else
                            {
                                _boosterHistory[typeId] = new List<DateTime> { DateTime.UtcNow };
                            }

                            continue;
                        }
                    }
                }
            }

            if (ESCache.Instance.InSpace && ESCache.Instance.InAbyssalDeadspace && boolWeNeedBoostersNow && DateTime.UtcNow > _lastDrugUsage.AddSeconds(2))
            {
                var shipsCargo = ESCache.Instance.DirectEve.GetShipsCargo();
                if (shipsCargo == null)
                    return false;

                foreach (var typeId in _boosterList.Where(x => !_increaseDamageBoostersList.Contains(x)).Select(b => b.Item1))
                {
                    // check if booster is already being used
                    if (ESCache.Instance.DirectEve.Me.Boosters.Any(b => b.TypeID == typeId))
                    {
                        if (DebugConfig.DebugBoosters && DirectEve.Interval(10000, 12000, typeId.ToString()))
                            Log($"Skipping to load booster, already loaded. TypeId [{typeId}]");
                        continue;
                    }

                    var boosterItem = shipsCargo.Items.FirstOrDefault(i => i.TypeId == typeId);
                    if (boosterItem != null)
                    {
                        if (_boosterHistory.TryGetValue(typeId, out var b) && b.Count(e => e > DateTime.UtcNow.AddMinutes(-60)) >= 3)
                        {
                            Log($"Prevented to consume booster [{boosterItem.TypeName}] We already tried to consume it 3 times the past 60 minutes.");
                            _boosterFailedState = true;
                            return false;
                        }

                        _lastDrugUsage = DateTime.UtcNow;
                        if (boosterItem.ConsumeBooster())
                        {
                            Log($"Consumed booster [{boosterItem.TypeName}]!!");

                        }
                        else
                        {
                            string msg = "ManageDrugs: Failed to use booster [" + boosterItem.TypeName + "]";
                            DirectEventManager.NewEvent(new DirectEvent(DirectEvents.NOTICE, msg));
                            Log(msg);
                            if (PlayNotificationSounds) Util.PlayNoticeSound();
                            if (PlayNotificationSounds) Util.PlayNoticeSound();
                            if (PlayNotificationSounds) Util.PlayNoticeSound();
                            if (PlayNotificationSounds) Util.PlayNoticeSound();
                        }

                        if (_boosterHistory.TryGetValue(typeId, out var bo))
                        {
                            bo.Add(DateTime.UtcNow);
                        }
                        else
                        {
                            _boosterHistory[typeId] = new List<DateTime> { DateTime.UtcNow };
                        }

                        continue;
                    }
                }
            }

            return false;
        }

        internal int TimeToWaitToActivateSpeedModAfterJumpingIntoRoom
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate)
                    return 0;
                //if (ESCache.Instance.Modules.Any(i => i._module.IsMicroWarpDrive))
                //    return 0;

                if (IsSituationThatNecessitatesWeUseOurUniqueOrbitRoutine)
                    return 0;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                    return 0;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                    return 5;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                    return 10;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                    return 0;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.TypeName.ToLower().Contains("Devoted Knight".ToLower())) > 2)
                        return 0;
                }

                return 0;
            }
        }

        internal bool? _safeToTurnOffRemoteRepairModSoWeCanRepairIt = null;

        internal bool SafeToTurnOffRemoteRepsSoWeCanRepairThem
        {
            get
            {
                if (!DirectEve.HasFrameChanged())
                {
                    if (_safeToTurnOffRemoteRepairModSoWeCanRepairIt != null)
                        return (bool)_safeToTurnOffRemoteRepairModSoWeCanRepairIt;
                }

                _safeToTurnOffRemoteRepairModSoWeCanRepairIt = null;

                if (!ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.EntitiesOnGrid.Any(i => i.Id != ESCache.Instance.ActiveShip.Entity.Id && i.TypeId != ESCache.Instance.ActiveShip.Entity.TypeId && i.TypeId != (int)TypeID.Orca && !i.IsHauler && i.IsPlayer))
                    return false;

                if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                {
                    if (DirectEve.Interval(15000)) Log("SafeToTurnOffRemoteRepSoWeCanRepairIt: if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure)) return true;");
                    return true;
                }

                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    var FleetMembersTakingDamage = ESCache.Instance.Targets.Where(i => !Combat.PotentialCombatTargets.Any(x => x.Id == i.Id) && i.GroupId == (int)Group.AssaultShip && .85 > i.ShieldPct && 25000 > i.Distance);
                    if (FleetMembersTakingDamage.Any())
                    {
                        if (DirectEve.Interval(15000)) Log("SafeToTurnOffRemoteRepSoWeCanRepairIt: if (FleetMembersTakingDamage.Any()) return false;");
                        return false;
                    }
                }

                if (DirectEve.Modules.Any(i => (i.IsRemoteShieldRepairModule || i.IsRemoteArmorRepairModule) && 5 > i.HeatDamagePercent))
                {
                    if (DebugConfig.DebugRemoteReps) Log("if (DirectEve.Modules.Any(i => (i.IsRemoteShieldRepairModule || i.IsRemoteArmorRepairModule) && 5 > i.HeatDamagePercent))");
                    _safeToTurnOffRemoteRepairModSoWeCanRepairIt = false;
                    return (bool)_safeToTurnOffRemoteRepairModSoWeCanRepairIt;
                }


                //if (overHeatPropmod)
                //{
                //    if (DebugConfig.DebugSpeedMod) Log("if (overHeatPropmod)");
                //    _safeToTurnOffRemoteRepairModSoWeCanRepairIt = false;
                //   return (bool)_safeToTurnOffRemoteRepairModSoWeCanRepairIt;
                //}

                _safeToTurnOffRemoteRepairModSoWeCanRepairIt = true;
                return (bool)_safeToTurnOffRemoteRepairModSoWeCanRepairIt;
            }
        }


        internal bool? _safeToTurnOffPropModSoWeCanRepairIt = null;

        internal bool SafeToTurnOffPropModSoWeCanRepairIt
        {
            get
            {
                if (!DirectEve.HasFrameChanged())
                {
                    if (_safeToTurnOffPropModSoWeCanRepairIt != null)
                        return (bool)_safeToTurnOffPropModSoWeCanRepairIt;
                }

                _safeToTurnOffPropModSoWeCanRepairIt = null;

                if (!ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.EntitiesOnGrid.Any(i => i.Id != ESCache.Instance.ActiveShip.Entity.Id && i.TypeId != ESCache.Instance.ActiveShip.Entity.TypeId && i.TypeId != (int)TypeID.Orca && !i.IsHauler && i.IsPlayer))
                    return false;

                //this means "speedtank"
                if (IsSituationThatNecessitatesWeUseOurUniqueOrbitRoutine)
                {
                    if (DebugConfig.DebugSpeedMod) Log("if (IsSituationThatNecessitatesWeUseOurUniqueOrbitRoutine)");
                    _safeToTurnOffPropModSoWeCanRepairIt = false;
                    return (bool)_safeToTurnOffPropModSoWeCanRepairIt;
                }

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                {
                    if (_getMTUInSpace != null)
                    {
                        if (DebugConfig.DebugSpeedMod) Log("if (_getMTUInSpace != null)");
                        return false;
                    }
                }

                if (ESCache.Instance.InAbyssalDeadspace)
                {
                    if (ESCache.Instance.ActiveShip.HasAfterburner)
                    {
                        if (_nextGate != null && 3000 > _nextGate.Distance && _nextGate._directEntity.IsAbyssGateOpen() && ESCache.Instance.Wrecks.Any() && ESCache.Instance.Wrecks.All(i => i.IsWreckEmpty))
                        {
                            if (DebugConfig.DebugSpeedMod) Log("3000 > _nextGate.Distance [" + _nextGate.Nearest1KDistance + "]");
                            _safeToTurnOffPropModSoWeCanRepairIt = true;
                            return (bool)_safeToTurnOffPropModSoWeCanRepairIt;
                        }
                    }
                    else if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                    {
                        if (_nextGate != null && 8000 > _nextGate.Distance && _nextGate._directEntity.IsAbyssGateOpen() && ESCache.Instance.Wrecks.Any() && ESCache.Instance.Wrecks.All(i => i.IsWreckEmpty))
                        {
                            if (DebugConfig.DebugSpeedMod) Log("8000 > _nextGate.Distance [" + _nextGate.Nearest1KDistance + "]");
                            _safeToTurnOffPropModSoWeCanRepairIt = true;
                            return (bool)_safeToTurnOffPropModSoWeCanRepairIt;
                        }
                    }
                }


                //if (Combat.PotentialCombatTargets.Count > 5)
                //{
                //    if (DebugConfig.DebugSpeedMod) Log("if (Combat.PotentialCombatTargets.Count > 5)");
                //    return false;
                //}

                if (DirectEve.Modules.Any(i => (i.IsAfterburner || i.IsMicroWarpDrive) && 5 > i.HeatDamagePercent))
                {
                    if (DebugConfig.DebugSpeedMod) Log("if (DirectEve.Modules.Any(i => (i.IsAfterburner || i.IsMicroWarpDrive) && 5 > i.HeatDamagePercent))");
                    _safeToTurnOffPropModSoWeCanRepairIt = false;
                    return (bool)_safeToTurnOffPropModSoWeCanRepairIt;
                }


                if (overHeatPropmod)
                {
                    if (DebugConfig.DebugSpeedMod) Log("if (overHeatPropmod)");
                    _safeToTurnOffPropModSoWeCanRepairIt = false;
                    return (bool)_safeToTurnOffPropModSoWeCanRepairIt;
                }


                //if (IsCloseToSafeSpotNearGate)
                //    return false;

                _safeToTurnOffPropModSoWeCanRepairIt = false;
                return (bool)_safeToTurnOffPropModSoWeCanRepairIt;
            }
        }

        internal bool WaitToActivateSpeedMod
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate)
                {
                    return false;
                }

                if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) || ESCache.Instance.AbyssalMobileTractor != null)
                {
                    if (TimeToWaitToActivateSpeedModAfterJumpingIntoRoom > Time.Instance.SecondsSinceLastSessionChange)
                    {
                        Log("Waiting for _secondsSinceLastSessionChange [" + Math.Round(Time.Instance.SecondsSinceLastSessionChange, 0) + "] > [" + TimeToWaitToActivateSpeedModAfterJumpingIntoRoom + "] sec before we use prop mod");
                        return true;
                    }

                    if (20 > Time.Instance.SecondsSinceLastSessionChange && 350 > ESCache.Instance.ActiveShip.Entity.Velocity)
                    {
                        if (DirectEve.Modules.Any(i => i.GroupId == (int)Group.ShieldHardeners))
                        {
                            if (!DirectEve.Modules.FirstOrDefault(i => i.GroupId == (int)Group.ShieldHardeners).IsActive ||
                                 DirectEve.Modules.FirstOrDefault(i => i.GroupId == (int)Group.ShieldHardeners).IsInLimboState)
                            {
                                Log("Waiting for Shield Hardener to be active before we start moving");
                                return true;
                            }
                        }

                        if (DirectEve.Modules.Any(i => i.GroupId == (int)Group.ShieldBoosters))
                        {
                            if (!DirectEve.Modules.FirstOrDefault(i => i.GroupId == (int)Group.ShieldBoosters).IsActive ||
                                 DirectEve.Modules.FirstOrDefault(i => i.GroupId == (int)Group.ShieldBoosters).IsInLimboState)
                            {
                                Log("Waiting for Shield ShieldBooster to be active before we start moving");
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
        }

        internal int DistanceToNotActivatePropModFromGatesAndWrecks
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate)
                {
                    return DistanceToDeactivatePropModSoWeDontBounce + 3000;
                }

                return DistanceToDeactivatePropModSoWeDontBounce + 3000;
            }
        }

        internal int DistanceToDeactivatePropModSoWeDontBounce
        {
            get
            {
                if (ESCache.Instance.ActiveShip.IsSpeedTankedShortRangeShip)
                {
                    return 2000;
                }

                if (ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate)
                {
                    return 8000;
                }

                return 5000;
            }
        }

        internal bool boolShouldWeDisablePropMod
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace)
                    return false;

                if (ESCache.Instance.ActiveShip.CapacitorPercentage < _propModMinCapPerc)
                    return true;

                if (SafeToTurnOffPropModSoWeCanRepairIt)
                    return true;

                if (!ESCache.Instance.ActiveShip.IsAssaultShip && !ESCache.Instance.ActiveShip.IsFrigate)
                {
                    if (ESCache.Instance.ActiveShip.IsLocatedWithinSpeedCloud)
                        return true;

                    if (ESCache.Instance.ActiveShip.Entity.Velocity > 3500)
                        return true;
                }

                if (IsAbyssGateOpen)
                {
                    if (ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty) && DistanceToDeactivatePropModSoWeDontBounce > ESCache.Instance.Wrecks.Where(i => !i.IsWreckEmpty).OrderBy(x => x.Distance).FirstOrDefault().Distance)
                    {
                        if (DebugConfig.DebugSpeedMod) Log("if (10000 > _nextGate.Distance && IsAbyssGateOpen) return false");
                        return true;
                    }

                    if (ESCache.Instance.Wrecks.Any())
                    {
                        if (ESCache.Instance.Wrecks.All(i => i.IsWreckEmpty))
                        {
                            if (DistanceToDeactivatePropModSoWeDontBounce > _nextGate.Distance)
                            {
                                if (DebugConfig.DebugSpeedMod) Log("if (10000 > _nextGate.Distance && IsAbyssGateOpen) return false");
                                return true;
                            }
                        }
                    }
                }

                return false;
            }
        }

        internal bool ManagePropMod(bool ManageWhilePaused = false)
        {
            if (DebugConfig.DebugSpeedMod)
                Log("ManagePropMod()");

            if (ESCache.Instance.InWarp)
                return true;

            if (!ESCache.Instance.Modules.Any())
                return true;

            if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.Entity.Velocity == 0)
                return true;

            if (!WeWantToBeMoving)
            {
                if (DebugConfig.DebugNavigateOnGrid) Log("WeWantToBeMoving [false]");
                return true;
            }

            if (DebugConfig.DebugNavigateOnGrid) Log("WeWantToBeMoving [true]");

            var capPerc = DirectEve.ActiveShip.CapacitorPercentage;

            if (WaitToActivateSpeedMod) return true;

            foreach (var propmod in DirectEve.Modules.Where(e => e.IsAfterburner || e.IsMicroWarpDrive))
            {
                if (DebugConfig.DebugSpeedMod) Log("foreach (var propmod in DirectEve.Modules.Where(e => e.GroupId == (int)Group.Afterburner))");
                if (propmod.IsActive)
                {
                    if (DebugConfig.DebugSpeedMod) Log($"propmod IsActive [{propmod.IsActive}]");
                    if (boolShouldWeDisablePropMod)
                    {
                        if (DebugConfig.DebugSpeedMod) Log("if (capPerc < _propModMinCapPerc || SafeToTurnOffPropModSoWeCanRepairIt || ESCache.Instance.ActiveShip.IsLocatedWithinSpeedCloud || ESCache.Instance.ActiveShip.Entity.Velocity > 1500)");
                        if (propmod.IsInLimboState)
                        {
                            if (DebugConfig.DebugSpeedMod) Log($"propmod [{propmod.TypeName}] IsInLimboState");
                            continue;
                        }

                        if (SafeToTurnOffPropModSoWeCanRepairIt)
                            Log($"Disabling propmod [{propmod.TypeName}] SafeToTurnOffPropModSoWeCanRepairIt [true]");

                        if (capPerc < _propModMinCapPerc)
                            Log($"Disabling propmod [{propmod.TypeName}] due cap too low. Perc [{capPerc}] Min required cap Perc [{_propModMinCapPerc}]");

                        if (DirectEve.Interval(800, 1500, propmod.ItemId.ToString()))
                        {
                            Log($"Disabling propmod [{propmod.TypeName}].");
                            propmod.Click();
                            return true;
                        }
                    }
                }

                if (propmod.IsBeingRepaired && (!SafeToTurnOffPropModSoWeCanRepairIt || _anyOverheat))
                {
                    if (DebugConfig.DebugSpeedMod) Log($"propmod [{propmod.TypeName}] IsBeingRepaired [{propmod.IsBeingRepaired}] && (!SafeToTurnOffPropModSoWeCanRepairIt || _anyOverheat)).");
                    // cancel repair
                    if (propmod.CancelRepair())
                    {
                        Log($"Canceling repair TypeName[{propmod.TypeName}].");
                    }
                }

                if (propmod.IsInLimboState)
                {
                    if (DebugConfig.DebugSpeedMod) Log($"propmod [{propmod.TypeName}] IsInLimboState!");
                    continue;
                }

                if (!propmod.IsBeingRepaired && SafeToTurnOffPropModSoWeCanRepairIt)
                {
                    if (DebugConfig.DebugSpeedMod) Log($"propmod [{propmod.TypeName}] IsBeingRepaired [{propmod.IsBeingRepaired}] && (SafeToTurnOffPropModSoWeCanRepairIt || !_anyOverheat))!!");
                    var shipsCargo = ESCache.Instance.DirectEve.GetShipsCargo();

                    if (shipsCargo != null)
                    {
                        if (shipsCargo.Items.Any(i => i.TypeId == _naniteRepairPasteTypeId))
                        {
                            if (!ShouldWeRepair)
                                continue;

                            // repair
                            if (propmod.Repair())
                            {
                                Log($"Repairing TypeName[{propmod.TypeName}].");
                                return true;
                            }
                        }

                        if (!shipsCargo.Items.Any(i => i.TypeId == _naniteRepairPasteTypeId))
                        {
                            DirectEve.IntervalLog(60000, 60000, "No nanite repair paste found in cargo, can't repair.");
                        }
                    }
                }

                if (propmod.IsActive)
                {
                    if (DebugConfig.DebugSpeedMod) Log($"propmod [{propmod.TypeName}] IsActive");
                    return true;
                }

                //var scrambled = DirectEve.Entities.Any(e => e.IsWarpScramblingMe);
                //if (capPerc >= _propModMinCapPerc && DirectEve.Interval(800, 1500) && _nextGate.Distance > distToNextGate)
                if (ShouldWeActivateSpeedMod)
                {
                    if (propmod.IsMicroWarpDrive && DirectEve.ActiveShip.IsScrambled)
                        return true;

                    if (DebugConfig.DebugSpeedMod) Log("if (ShouldWeActivateSpeedMod)");
                    if (DirectEve.HasFrameChanged())
                    {
                        Log($"Activating [{propmod.TypeName}]");
                        propmod.Click();
                        return true;
                    }

                    if (DebugConfig.DebugSpeedMod) Log("!if (DirectEve.HasFrameChanged())");
                    return false;
                }

                if (DebugConfig.DebugSpeedMod) Log("!if (ShouldWeActivateSpeedMod)");
                return false;
            }

            if (DebugConfig.DebugSpeedMod) Log("done with foreach (var propmod in DirectEve.Modules.Where(e => e.GroupId == (int)Group.Afterburner))");
            return false;
        }

        internal bool ShouldWeRepair
        {
            get
            {
                try
                {
                    if (!ESCache.Instance.InAbyssalDeadspace)
                        return true;

                    if (ESCache.Instance.Modules.Where(x => x.IsActive).Any(i => i.IsOverloaded)) //dont repair if we have things overheated, it will just fail. eve doesnt let you overheat and repair anything even if there are diff modules, etc
                        return false;

                    if (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsDestroyer)
                    {
                        if (!ESCache.Instance.Targets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                        {
                            if (_endGate != null) //endgate exists and no NPCs here, no need to waste nanite repair paste
                            {
                                return false;
                            }

                            return true;
                        }

                        return true;
                    }

                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                    {
                        if (Combat.PotentialCombatTargets.Count > 3)
                            return false;

                        if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsAbyssalDroneBattleship))
                            return false;

                        if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsAbyssalKaren))
                            return false;

                        if (Combat.PotentialCombatTargets.Any(i => i._directEntity.Name.Contains("Vedmak")))
                            return false;

                        if (Combat.PotentialCombatTargets.Any(i => i._directEntity.Name.Contains("Kikimora")))
                            return false;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return true;
                }
            }
        }

        internal bool ShouldWeActivateSpeedMod
        {
            get
            {
                if (!ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.EntitiesOnGrid.Any(i => i.Id != ESCache.Instance.ActiveShip.Entity.Id && i.TypeId != ESCache.Instance.ActiveShip.Entity.TypeId && i.TypeId != (int)TypeID.Orca && !i.IsHauler && i.IsPlayer))
                    return true;

                if (_propModMinCapPerc >= ESCache.Instance.ActiveShip.CapacitorPercentage)
                {
                    if (DebugConfig.DebugSpeedMod) Log("if (_propModMinCapPerc >= ESCache.Instance.ActiveShip.CapacitorPercentage) return false");
                    return false;
                }

                if (!ESCache.Instance.ActiveShip.IsAssaultShip && !ESCache.Instance.ActiveShip.IsFrigate)
                {
                    if (ESCache.Instance.ActiveShip.IsLocatedWithinSpeedCloud)
                    {
                        if (DebugConfig.DebugSpeedMod) Log("IsLocatedWithinSpeedCloud [" + ESCache.Instance.ActiveShip.IsLocatedWithinSpeedCloud + "] return false");
                        return false;
                    }

                    if (!ESCache.Instance.ActiveShip.HasMicroWarpDrive && ESCache.Instance.ActiveShip.Entity.Velocity > 900)
                    {
                        if (DebugConfig.DebugSpeedMod) Log("ActiveShip Velocity [" + Math.Round(ESCache.Instance.ActiveShip.Entity.Velocity, 0) + "] > 900) return false");
                        return false;
                    }
                }

                //if (_getMTUInSpace != null &&  ESCache.Instance.ActiveShip.FollowingEntity.Id == _getMTUInSpace.Id && 8000 > _getMTUInSpace.Distance)
                //    return false;

                if (ESCache.Instance.InAbyssalDeadspace && IsAbyssGateOpen)
                {
                    if (ESCache.Instance.AbyssalMobileTractor != null)
                    {
                        if (ESCache.Instance.ActiveShip.HasMicroWarpDrive)
                        {
                            if (ESCache.Instance.AbyssalMobileTractor.Distance > 7000)
                            {
                                if (DebugConfig.DebugSpeedMod) Log("ESCache.Instance.AbyssalMobileTractor.Distance > 7000 return true");
                                return true;
                            }
                        }

                        if (ESCache.Instance.AbyssalMobileTractor.Distance > 4000)
                        {
                            if (DebugConfig.DebugSpeedMod) Log("ESCache.Instance.AbyssalMobileTractor.Distance > 4000 return true");
                            return true;
                        }

                        return false;
                    }

                    if (ESCache.Instance.Wrecks.Any())
                    {
                        if (ESCache.Instance.Wrecks.Any(i => !i.IsWreckEmpty))
                        {
                            if (DistanceToNotActivatePropModFromGatesAndWrecks > ESCache.Instance.Wrecks.Where(i => !i.IsWreckEmpty).OrderBy(x => x.Distance).FirstOrDefault().Distance)
                            {
                                if (DebugConfig.DebugSpeedMod) Log("if (10000 > _nextGate.Distance && IsAbyssGateOpen) return false");
                                return false;
                            }

                            return true;
                        }

                        if (ESCache.Instance.Wrecks.All(i => i.IsWreckEmpty))
                        {
                            if (DistanceToNotActivatePropModFromGatesAndWrecks > _nextGate.Distance)
                            {
                                if (DebugConfig.DebugSpeedMod) Log("if (10000 > _nextGate.Distance && IsAbyssGateOpen) return false");
                                return false;
                            }
                        }
                    }
                }

                if (Time.Instance.LastInWarp.AddMilliseconds(10) > DateTime.UtcNow)
                    return false;

                return true;
            }
        }

        internal bool dronesNeedRepair
        {
            get
            {
                if (ESCache.Instance.InStation)
                    return false;

                if (!Drones.UseDrones)
                    return false;


                if (!alldronesInBay.All(d => d.GetDroneInBayDamageState().Value.Y >= 1.0f))
                {
                    ESCache.Instance.NeedRepair = true;
                    return true;
                }

                return false;
            }
        }

        internal bool dronesNeedSeriousRepair
        {
            get
            {
                if (ESCache.Instance.InStation)
                    return false;

                if (!Drones.UseDrones)
                    return false;


                if (!alldronesInBay.All(d => d.GetDroneInBayDamageState().Value.Y >= .5f))
                {
                    ESCache.Instance.NeedRepair = true;
                    return true;
                }

                return false;
            }
        }

        internal bool shipNeedsRepair
        {
            get
            {
                if (ESCache.Instance.InStation)
                    return false;

                if (!ESCache.Instance.Modules.Any(i => i.IsArmorRepairModule))
                {
                    if (ESCache.Instance.ActiveShip.StructurePercentage < 100)
                    {
                        ESCache.Instance.NeedRepair = true;
                        return true;
                    }
                }

                if (ESCache.Instance.ActiveShip.ArmorPercentage < 100 ||
                      ESCache.Instance.ActiveShip.StructurePercentage < 100)
                {
                    ESCache.Instance.NeedRepair = true;
                    return true;
                }

                return false;
            }
        }


        internal bool AbyssalNeedRepair(bool excludeModules = false)
        {

            if (ESCache.Instance.InStation)
                return false;

            var modulesNeedRepair = !excludeModules
                ? !ESCache.Instance.DirectEve.Modules.All(m => m.HeatDamagePercent == 0)
                : false;

            if (DirectEve.Interval(20000))
                Log($"Drones need repair [{dronesNeedRepair}] Modules need repair [{modulesNeedRepair}] Ship needs repair [{shipNeedsRepair}]");
            return dronesNeedRepair || modulesNeedRepair || shipNeedsRepair;
        }

        internal bool AreTheMajorityOfNPCsInOptimalOnGrid()
        {
            var targetsWithoutCaches = Combat.PotentialCombatTargets.Where(e => !e.IsAbyssalDeadspaceTriglavianBioAdaptiveCache && !e.IsAbyssalDeadspaceTriglavianExtractionNode && !e.IsWreck).ToList();

            if (targetsWithoutCaches.Any())
            {
                var totalTargetsCount = targetsWithoutCaches.Count();
                var targetsWhichAreInOptimalCount = targetsWithoutCaches.Count(e => e.IsInOptimalRange);
                if (DirectEve.Interval(3000, 5000))
                {
                    Log($"TargetsOnGridCount [{totalTargetsCount}] TargetsOnGridInOptimal [{targetsWhichAreInOptimalCount}]");
                }

                if (targetsWhichAreInOptimalCount >= totalTargetsCount * 0.51)
                {
                    return true;
                }
            }
            return false;
        }

        internal bool SkipTargetingExtractionNodes(EntityCache target)
        {
            if (target.IsAbyssalDeadspaceTriglavianExtractionNode && !target.IsAbyssalBioAdaptiveCache)
            {
                if (DebugConfig.DebugAlwaysIgnoreExtractionNodes)
                {
                    if (DebugConfig.DebugTargetCombatants) Log("AlwaysIgnoreExtractionNodes [" + DebugConfig.DebugAlwaysIgnoreExtractionNodes + "]");
                    return true;
                }

                if (ESCache.Instance.InAbyssalDeadspace && ESCache.Instance.DirectEve.Me.AbyssalRemainingSeconds == 0)
                {
                    //broken timer?
                    if (DebugConfig.DebugTargetCombatants) Log("AbyssalRemainingSeconds == 0! broken timer?");
                    return true;
                }

                if (!boolDoWeHaveTime)
                {
                    if (DebugConfig.DebugTargetCombatants) Log("boolDoWeHaveTime [ false ]: Do not target any more extraction nodes");
                    return true;
                }

                if (45 > GetEstimatedStageRemainingTimeToClearGrid())
                {
                    if (DebugConfig.DebugTargetCombatants) Log("60 > GetEstimatedStageRemainingTimeToClearGrid [" + GetEstimatedStageRemainingTimeToClearGrid() + "]: Do not target any more caches");
                    return true;
                }

                if (ESCache.Instance.TotalTargetsAndTargeting.Count(i => i.IsAbyssalBioAdaptiveCache || i.IsAbyssalDeadspaceTriglavianExtractionNode) >= 1)
                {
                    if (DebugConfig.DebugTargetCombatants) Log("Targets: AbyssalBioAdaptiveCache or ExtractionNode >= 1: Do not target any more caches");
                    return true;
                }

                if (_getMTUInSpace != null)
                {
                    int MaxDistanceFromExtractionNodeToMtu = 50000;
                    if (MaxDistanceFromExtractionNodeToMtu > target.DistanceTo(_getMTUInSpace))
                    {
                        if (DebugConfig.DebugTargetCombatants) Log("Targets: Extraction Node at [" + target.Nearest1KDistance + "k] is > MaxDistanceFromExtractionNodeToMtu [" + MaxDistanceFromExtractionNodeToMtu + "] not targeting");
                        return true;
                    }
                }
                else if (_getMTUInBay == null) //If we have no MTU at all
                {
                    int MaxDistanceFromExtractionNodeToMe = 40000;
                    if (MaxDistanceFromExtractionNodeToMe > target.Distance)
                    {
                        if (DebugConfig.DebugTargetCombatants) Log("Targets: Extraction Node at [" + target.Nearest1KDistance + "k] is > MaxDistanceFromExtractionNodeToMe [" + MaxDistanceFromExtractionNodeToMe + "] not targeting");
                        return true;
                    }
                }

                //if (_mtuAlreadyDroppedDuringThisStage && _getMTUInSpace == null)
                //{
                //    if (DebugConfig.DebugTargetCombatants) Log("_mtuAlreadyDroppedDuringThisStage [" + _mtuAlreadyDroppedDuringThisStage + "] _getMTUInSpace == null: Do not target any more caches");
                //    return true;
                //}

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.TypeName.ToLower().Contains("Stormbringer".ToLower())))
                        return true;

                    if (Combat.PotentialCombatTargets.Any(i => i.TypeName.ToLower().Contains("Thunderchild".ToLower())))
                        return true;
                }

                if (ESCache.Instance.ActiveShip.Entity.GroupId == (int)Group.AssaultShip)
                {
                    if (Combat.PotentialCombatTargets.Any())
                    {
                        if (Combat.PotentialCombatTargets.Count() >= 3)
                        {
                            if (DebugConfig.DebugTargetCombatants) Log("Combat.PotentialCombatTargets.Count() [" + Combat.PotentialCombatTargets.Count() + "] Do not target caches yet");
                            return true;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i._directEntity.IsAbyssalKaren))
                                return true;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                                return true;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                                return true;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Kikimora")))
                                return true;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.TypeName.Contains("Vedmak")))
                                return true;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DevotedCruiserSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.TypeName.ToLower().Contains("Devoted Knight".ToLower())))
                                return true;
                        }

                        if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Devoted Hunter")) >= 1)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(\"Devoted Hunter\")) >= 1) return true");
                            return true;
                        }

                        if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains("Devoted Knight")) >= 1)
                        {
                            if (DebugConfig.DebugNavigateOnGrid) Log("if (Combat.PotentialCombatTargets.Count(i => i.TypeName.Contains(\"Devoted Knight\")) >= 1) return true");
                            return true;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.TypeName.ToLower().Contains("Lucifer Cynabal".ToLower())))
                                return true;
                        }

                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                            return true;
                    }
                }

                //Combat.PotentialCombatTargets.Any(i => !i.IsAbyssalDeadspaceTriglavianExtractionNode)

                return false;
            }

            return false;
        }

        internal void Target()
        {
            try
            {
                if (ESCache.Instance.EveAccount.UseFleetMgr && Combat.PotentialCombatTargets.All(i => i.BracketType != BracketType.Large_Collidable_Structure && 0 == i.Velocity) && 25 > Time.Instance.SecondsSinceLastSessionChange)
                {
                    //if (DirectEve.Interval(2000, 3000)) ESCache.Instance.DirectEve.ExecuteCommand(DirectCmd.CmdStopShip);
                    return;
                }

                if (!ESCache.Instance.InAbyssalDeadspace)
                {
                    Combat.TargetCombatants();
                    return;
                }

                // target npcs
                if (ESCache.Instance.TotalTargetsAndTargeting.Count(i => i.GroupId != (int)Group.AssaultShip) < _maximumLockedTargets)
                {
                    if (DebugConfig.DebugTargetCombatants) Log("if (ESCache.Instance.TotalTargetsAndTargeting.Count() [" + ESCache.Instance.TotalTargetsAndTargeting.Count() + "] < _maximumLockedTargets [" + _maximumLockedTargets + "])");
                    if (DebugConfig.DebugTargetCombatants) Log("Combat.PotentialCombatTargets [" + Combat.PotentialCombatTargets.Count() + "]");
                    List<EntityCache> cachedListOfEntities = Combat.PotentialCombatTargets;
                    foreach (var individualAbyssalDeadspaceTriglavianExtractionNode in ESCache.Instance.EntitiesOnGrid.Where(i => i.IsAbyssalDeadspaceTriglavianExtractionNode || i.IsAbyssalBioAdaptiveCache))
                    {
                        if (DebugConfig.DebugTargetCombatants) Log("individualAbyssalDeadspaceTriglavianExtractionNode: cachedListOfEntities.Add(individualAbyssalDeadspaceTriglavianExtractionNode);");
                        cachedListOfEntities.Add(individualAbyssalDeadspaceTriglavianExtractionNode);
                    }

                    if (DebugConfig.DebugTargetCombatants) Log("cachedListOfEntities [" + cachedListOfEntities.Count() + "]");

                    var _cachedListOfEntities = cachedListOfEntities.OrderByDescending(x => x.IsReadyToTarget).ThenBy(e => e.Id).OrderBy(e => e._directEntity.AbyssalTargetPriority);
                    if (Combat.PotentialCombatTargets.Count() >= 3)
                    {
                        _cachedListOfEntities = cachedListOfEntities.Where(x => x.IsReadyToTarget).OrderBy(e => e.Id).OrderBy(e => e._directEntity.AbyssalTargetPriority);
                    }

                    foreach (var target in _cachedListOfEntities)
                    {
                        if (target.IsTarget)
                        {
                            if (DebugConfig.DebugTargetCombatants) Log("targetEntity IsTarget [ true ]");
                            continue;
                        }

                        if (target.IsTargeting)
                        {
                            if (DebugConfig.DebugTargetCombatants) Log("targetEntity IsTargeting [ true ]");
                            continue;
                        }

                        if (!Drones.UseDrones && target.Distance > (_maxRange + 2000))
                        {
                            if (DebugConfig.DebugTargetCombatants) Log("targetEntity Distance [" + target.Nearest1KDistance + "] is more than _maxRange + 2000 [" + _maxRange + 2000 + "]");
                            continue;
                        }

                        if (target._directEntity.AbyssalTargetPriority > 900)
                        {
                            if (DebugConfig.DebugTargetCombatants) Log("targetEntity AbyssalTargetPriority [" + target._directEntity.AbyssalTargetPriority + "] is more than 900 ignoring");
                            continue;
                        }

                        if (DebugConfig.DebugTargetCombatants) Log("targetEntity AbyssalTargetPriority - after");


                        if (SkipTargetingExtractionNodes(target))
                        {
                            //
                            // If it is a cache and we want to kill/loot it and we have 2 or more 2 of them locked right now, do not target any more
                            //
                            Log("if (SkipTargetingExtractionNodes(target)) continue): target [" + target.Name + "] @ [" + target.Nearest1KDistance + " k away] ID [" + target.MaskedId + "]");
                            continue;
                        }

                        if (DebugConfig.DebugTargetCombatants) Log("!if (SkipTargetingExtractionNodes(target))");

                        if (ESCache.Instance.TotalTargetsAndTargeting.Count(i => i.GroupId != (int)Group.AssaultShip && i._directEntity.AbyssalTargetPriority == target._directEntity.AbyssalTargetPriority) >= 4 &&
                            Combat.PotentialCombatTargets.Count(x => x.Velocity > 0 && x._directEntity.AbyssalTargetPriority != target._directEntity.AbyssalTargetPriority) >= 4)
                        {
                            if (DebugConfig.DebugTargetCombatants) Log("if (ESCache.Instance.TotalTargetsAndTargeting.Count(i => i._directEntity.AbyssalTargetPriority == target._directEntity.AbyssalTargetPriority) >= 4 && Combat.PotentialCombatTargets.Count(x => x._directEntity.AbyssalTargetPriority != target._directEntity.AbyssalTargetPriority) >= 4)");
                            continue;
                        }

                        if (DebugConfig.DebugTargetCombatants) Log("!!! if (ESCache.Instance.TotalTargetsAndTargeting.Count(i => i._directEntity.AbyssalTargetPriority == target._directEntity.AbyssalTargetPriority) >= 4 && Combat.PotentialCombatTargets.Count(x => x._directEntity.AbyssalTargetPriority != target._directEntity.AbyssalTargetPriority) >= 4)");

                        if (DirectEve.HasFrameChanged())
                        {
                            try
                            {
                                Log($"Targeting [" + target.TypeName + "][" + Math.Round(target.Distance / 1000, 0) + "k] Priority[" + target._directEntity.AbyssalTargetPriority + "] Velocity[" + Math.Round(target.Velocity, 0) + "] " + target.MaskedId + " MyMaxTargets[" + _maximumLockedTargets + "]");
                            }
                            catch (Exception ex)
                            {
                                Log("Exception [" + ex + "]");
                            }

                            if (DebugConfig.DebugTargetCombatants) Log("target.LockTarget();");
                            if (target.LockTarget("ManageTargetLocks"))
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("target.LockTarget() true;");
                            }

                            continue;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return;
            }
        }

        internal bool UnTarget()
        {
            try
            {
                //Do we have any targets?
                if (ESCache.Instance.Targets.Any())
                {
                    if (DebugConfig.DebugTargetCombatants) Log("if (_currentLockedTargets.Any())");
                    EntityCache targetToUnlock = null;
                    // unlock targets being out of range
                    if (ESCache.Instance.Targets.Any(e => e.Distance >= _maxRange) && Combat.PotentialCombatTargets.Count() > _maximumLockedTargets)
                    {
                        if (DebugConfig.DebugTargetCombatants) Log("if (_currentLockedTargets.Any(e => e.Distance >= _maxRange))");
                        targetToUnlock = ESCache.Instance.Targets.Where(z => z.GroupId != (int)Group.AssaultShip).OrderByDescending(i => i.HealthPct).FirstOrDefault(e => e.Distance > _maxRange);
                        if (targetToUnlock != null)
                        {
                            if (DirectEve.Interval(2500, 3200, targetToUnlock.Id.ToString()))
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("if (targetToUnlock.UnlockTarget())");
                                if (targetToUnlock.UnlockTarget())
                                {
                                    Log($"Unlocking [{targetToUnlock.TypeName}]@[{targetToUnlock.Nearest1KDistance}] > MaxRange [" + _maxRange + "] ");
                                    return true;
                                }
                            }
                        }
                    }

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)
                    {
                        if (DebugConfig.DebugTargetCombatants) Log("if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)");
                        if (!DirectEve.Interval(20000))
                        {
                            if (DebugConfig.DebugTargetCombatants) Log("if (!DirectEve.Interval(20000))");
                            return true;
                        }
                    }

                    targetToUnlock = null;
                    //
                    //
                    //
                    if (ESCache.Instance.TotalTargetsAndTargeting.Any() && ESCache.Instance.TotalTargetsAndTargeting.Count(i => i.GroupId != (int)Group.AssaultShip) >= _maximumLockedTargets)
                    {
                        var worstPriorityLockedTarget = ESCache.Instance.Targets.Where(e => e.GroupId != (int)Group.AssaultShip).OrderByDescending(e => e._directEntity.AbyssalTargetPriority).OrderByDescending(i => i.HealthPct).FirstOrDefault();
                        var bestPriorityUnLockedTarget = Combat.PotentialCombatTargets.Where(i => !i.IsTarget && !i.IsTargeting && i.IsReadyToTarget && i._directEntity.AbyssalTargetPriority != 0).OrderByDescending(e => e._directEntity.AbyssalTargetPriority).OrderByDescending(i => i.HealthPct).FirstOrDefault();
                        if (worstPriorityLockedTarget != null && bestPriorityUnLockedTarget != null)
                        {
                            if (worstPriorityLockedTarget._directEntity.AbyssalTargetPriority > bestPriorityUnLockedTarget._directEntity.AbyssalTargetPriority)
                            {
                                if (3 > ESCache.Instance.TotalTargetsAndTargeting.Count(i => i._directEntity.AbyssalTargetPriority == bestPriorityUnLockedTarget._directEntity.AbyssalTargetPriority))
                                {
                                    Log("if (worstPriorityLockedTarget [" + worstPriorityLockedTarget.TypeName + "] Priority [" + worstPriorityLockedTarget._directEntity.AbyssalTargetPriority + "] > bestPriorityUnLockedTarget [" + bestPriorityUnLockedTarget.TypeName + "] Priority [" + bestPriorityUnLockedTarget._directEntity.AbyssalTargetPriority + "] lower is better");
                                    //targetToUnlock = ESCache.Instance.Targets.FirstOrDefault(e => (e.IsAbyssalDeadspaceTriglavianBioAdaptiveCache || e.IsAbyssalDeadspaceTriglavianExtractionNode) && !IsEntityWeWantToLoot(e._directEntity));
                                    targetToUnlock = worstPriorityLockedTarget;
                                }
                            }
                        }
                    }

                    if (AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)
                    {
                        if ((ESCache.Instance.Targets.Count > 2 && ESCache.Instance.Targets.Any(i => i.IsAbyssalDeadspaceTriglavianExtractionNode) && !Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure)) || !boolDoWeHaveTime)
                        {
                            Log("if ((ESCache.Instance.Targets.Count > 2 && ESCache.Instance.Targets.Any(i => i.IsAbyssalDeadspaceTriglavianExtractionNode) && !Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure)) || !boolDoWeHaveTime)");
                            //targetToUnlock = ESCache.Instance.Targets.FirstOrDefault(e => (e.IsAbyssalDeadspaceTriglavianBioAdaptiveCache || e.IsAbyssalDeadspaceTriglavianExtractionNode) && !IsEntityWeWantToLoot(e._directEntity));
                            if (ESCache.Instance.Targets.Where(e => e.GroupId != (int)Group.AssaultShip).OrderByDescending(i => i.HealthPct).FirstOrDefault(e => e.IsAbyssalDeadspaceTriglavianExtractionNode && !e.IsAbyssalBioAdaptiveCache) != null)
                            {
                                Log("if (ESCache.Instance.Targets.Where(e => e.GroupId != (int)Group.AssaultShip).OrderByDescending(i => i.HealthPct).FirstOrDefault(e => e.IsAbyssalDeadspaceTriglavianExtractionNode && !e.IsAbyssalBioAdaptiveCache) != null)");
                                targetToUnlock = ESCache.Instance.Targets.Where(e => e.GroupId != (int)Group.AssaultShip).OrderByDescending(i => i.HealthPct).FirstOrDefault(e => e.IsAbyssalDeadspaceTriglavianExtractionNode && !e.IsAbyssalBioAdaptiveCache);
                            }
                        }
                    }

                    if (targetToUnlock == null)
                    {
                        if (DebugConfig.DebugTargetCombatants) Log("if (targetToUnlock == null)");
                        if (ESCache.Instance.Targets.Any(x => x.GroupId != (int)Group.AssaultShip && x._directEntity.AbyssalTargetPriority >= 900))
                        {
                            if (ESCache.Instance.Targets.Any(x => x.GroupId != (int)Group.AssaultShip && x._directEntity.AbyssalTargetPriority >= 900 && x.TypeName.Contains("Lucifer Cynabal")))
                            {
                                Log("if (ESCache.Instance.Targets.Any(x => x._directEntity.AbyssalTargetPriority >= 900)) - this shouldnt happen: how did this target get locked? - NEVER unlock a Lucifer Cynabal! - this is a BUG");
                            }
                            else if (ESCache.Instance.Targets.Any(x => x.GroupId != (int)Group.AssaultShip && x._directEntity.AbyssalTargetPriority >= 900))
                            {
                                Log("if (ESCache.Instance.Targets.Any(x => x._directEntity.AbyssalTargetPriority >= 900)) - this shouldnt happen: how did this target get locked?");
                                targetToUnlock = ESCache.Instance.Targets.Where(x => x.GroupId != (int)Group.AssaultShip && x._directEntity.AbyssalTargetPriority >= 900).FirstOrDefault();
                            }
                        }
                    }

                    if (targetToUnlock != null)
                    {
                        if (DebugConfig.DebugTargetCombatants) Log("if (targetToUnlock != null)");
                        if (DirectEve.Interval(2500, 3200, targetToUnlock.Id.ToString()))
                        {
                            if (targetToUnlock.UnlockTarget())
                            {
                                Log("Unlocking [" + targetToUnlock.TypeName + "]@[" + Math.Round(targetToUnlock.Distance / 1000, 0) + "k] Id [" + targetToUnlock.MaskedId + "] AbyssalPriority [" + targetToUnlock._directEntity.AbyssalTargetPriority + "]");
                                return true;
                            }
                        }
                    }

                    // check if higher prio is present
                    var highestTargeted = ESCache.Instance.TotalTargetsAndTargeting.Where(x => x.GroupId != (int)Group.AssaultShip && 900 > x._directEntity.AbyssalTargetPriority).OrderByDescending(e => e._directEntity.AbyssalTargetPriority).FirstOrDefault();
                    if (highestTargeted != null)
                    {
                        if (DebugConfig.DebugTargetCombatants) Log("if (lowestOnGrid.AbyssalTargetPriority < highestTargeted.AbyssalTargetPriority)");
                        // get the lowest on grid within _maxRange
                        var lowestOnGrid = Combat.PotentialCombatTargets.Where(e => !e.IsTarget && !e.IsTargeting && e.Distance < _maxRange && 900 > e._directEntity.AbyssalTargetPriority)
                            .OrderBy(e => e._directEntity.AbyssalTargetPriority).FirstOrDefault();
                        if (lowestOnGrid != null)
                        {
                            if (DebugConfig.DebugTargetCombatants) Log("if (lowestOnGrid != null)");
                            // if lowest on grid < highest targeted, then unlock highest targeted
                            if (lowestOnGrid._directEntity.AbyssalTargetPriority < highestTargeted._directEntity.AbyssalTargetPriority)
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("if (lowestOnGrid.AbyssalTargetPriority < highestTargeted.AbyssalTargetPriority)");
                                if (ESCache.Instance.TotalTargetsAndTargeting.Count() >= _maximumLockedTargets)
                                {
                                    if (DebugConfig.DebugTargetCombatants) Log("if (_currentLockedAndLockingTargets.Count() >= _maximumLockedTargets)");
                                    if (DirectEve.Interval(800, 1200, highestTargeted.Id.ToString()))
                                    {
                                        Log($"Unlocking [{highestTargeted.TypeName}]@[{highestTargeted.Nearest1KDistance}] S[" + highestTargeted.ShieldPct + "] A[" + highestTargeted.ArmorPct + "] H[" + highestTargeted.StructurePct + "][" + highestTargeted.MaskedId + "] due higher priority is present. Priority [" + highestTargeted._directEntity.AbyssalTargetPriority + "] will be replaced with [" + lowestOnGrid.TypeName + "] Priority [" + lowestOnGrid._directEntity.AbyssalTargetPriority + "]");
                                        highestTargeted.UnlockTarget();
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return true;
            }
        }


        internal void TargetFleetMembersIfNeeded()
        {
            try
            {
                if (!TargetingOfFleetMembersIsNeeded)
                {
                    if (DebugConfig.DebugTargetCombatants) Log("TargetingOfFleetMembersIsNeeded: false");
                    return;
                }

                if (!DirectEve.Interval(2000))
                    return;

                if (ESCache.Instance.TotalTargetsAndTargeting.Count() >= ESCache.Instance.MaxLockedTargets + 2)
                {
                    if (DebugConfig.DebugTargetCombatants) Log("if (ESCache.Instance.TotalTargetsAndTargeting.Count() [" + ESCache.Instance.TotalTargetsAndTargeting.Count() + "] >= ESCache.Instance.MaxLockedTargets [" + ESCache.Instance.MaxLockedTargets + "])");
                    return;
                }

                // target fleet members
                if (ESCache.Instance.TotalTargetsAndTargeting.Count(i => i.GroupId == (int)Group.AssaultShip) < 2)
                {
                    if (DebugConfig.DebugTargetCombatants) Log("if (ESCache.Instance.TotalTargetsAndTargeting.Count(i => i.GroupId == (int)Group.AssaultShip)) [" + ESCache.Instance.TotalTargetsAndTargeting.Count() + "] < 2)");
                    if (DebugConfig.DebugTargetCombatants) Log("AssaultShips [" + ESCache.Instance.EntitiesNotSelf.Count(i => i.GroupId == (int)Group.AssaultShip) + "]");
                    List<EntityCache> AssaultShipsOnGrid = ESCache.Instance.EntitiesNotSelf.Where(i => i.GroupId == (int)Group.AssaultShip).ToList();

                    if (AssaultShipsOnGrid.Any())
                    {
                        if (DebugConfig.DebugTargetCombatants) Log("if (AssaultShipsOnGrid.Any())");

                        foreach (var FleetMemberToTarget in AssaultShipsOnGrid.OrderBy(e => e.Distance))
                        {
                            if (FleetMemberToTarget.IsTarget)
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("targetEntity IsTarget [ true ]");
                                continue;
                            }

                            if (FleetMemberToTarget.IsTargeting)
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("targetEntity IsTargeting [ true ]");
                                continue;
                            }

                            if (FleetMemberToTarget.Distance > (_maxRange + 2000))
                            {
                                if (DebugConfig.DebugTargetCombatants) Log("targetEntity Distance [" + FleetMemberToTarget.Nearest1KDistance + "] is more than _maxRange + 2000 [" + _maxRange + 2000 + "]");
                                continue;
                            }

                            if (DebugConfig.DebugTargetCombatants) Log("targetEntity AbyssalTargetPriority - after");

                            if (DirectEve.HasFrameChanged())
                            {
                                try
                                {
                                    Log($"Targeting [" + FleetMemberToTarget.Name + "][" + Math.Round(FleetMemberToTarget.Distance / 1000, 0) + "k] Velocity[" + Math.Round(FleetMemberToTarget.Velocity, 0) + "] MaxLockedTargets [" + ESCache.Instance.MaxLockedTargets + "]");
                                }
                                catch (Exception ex)
                                {
                                    Log("Exception [" + ex + "]");
                                }

                                if (DebugConfig.DebugTargetCombatants) Log("target.LockTarget();");
                                if (FleetMemberToTarget.LockTarget("ManageTargetLocks"))
                                {
                                    if (DebugConfig.DebugTargetCombatants) Log("target.LockTarget() true;");
                                }

                                continue;
                            }
                        }
                    }

                }

                return;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return;
            }
        }

        internal bool ManageTargetLocks()
        {
            try
            {
                if (DebugConfig.DebugTargetCombatants)
                    Log("ManageTargetLocks()");

                if (!DirectEve.HasFrameChanged()) return true;

                if (ESCache.Instance.InWarp)
                    return true;

                if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsPod)
                {
                    if (DebugConfig.DebugTargetCombatants)
                        Log("if (ESCache.Instance.ActiveShip.IsPod) return true");
                    return true;
                }

                if (Combat.PotentialCombatTargets.Any())
                {
                    if (ESCache.Instance.MaxLockedTargets == 0)
                    {
                        Log($"We are jammed, targeting the jamming entities.");
                        var jammers = Combat.TargetedBy.Where(t => t.IsTryingToJamMe).ToList();
                        foreach (var jammer in jammers)
                        {
                            if (!jammer.IsTargeting && !jammer.IsTarget && jammer.Distance <= _maxRange && DirectEve.Interval(3500, 4500, jammer.Id.ToString()))
                            {
                                Log($"Targeting jammer [{jammer.Id}] TypeName [{jammer.TypeName}].");
                                jammer.LockTarget("");
                                return true;
                            }
                        }
                    }

                    if (DirectEve.Interval(15000, 20000))
                        Log($"TargetsOnGrid: Locked [" + TargetsOnGrid.Count(i => i.IsTarget || i.IsTargeting) + "] UnLocked [" + TargetsOnGrid.Count(x => !x.IsTarget && !x.IsTargeting) + "] Total On Grid [" + Combat.PotentialCombatTargets.Count() + "]");

                    TargetFleetMembersIfNeeded();

                    Target();

                    UnTarget();
                }

                return false;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return true;
            }
        }

        private double _lastArmorPerc = 100;
        private double _lastStructurePerc = 100;

        internal bool boolShouldWeDeactivateHardener
        {
            get
            {
                //capPerc < _moduleMinCapPerc

                if (DirectEve.Session.IsAbyssalDeadspace)
                {
                    if (ESCache.Instance.Modules.Any(i => i.IsShieldHardener || i.IsArmorHardener))
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                        {
                            if (DirectEve.ActiveShip.CapacitorPercentage < _moduleMinCapPerc)
                            {
                                if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && i.IsActive && !i.IsInLimboState )) Log($"boolShouldWeDeactivateShieldHardener [true] due to cap too low. Perc [{Math.Round(DirectEve.ActiveShip.CapacitorPercentage, 0)}] Min required cap Perc [{_moduleMinCapPerc}]");
                                return true;
                            }

                            //if (DirectEve.ActiveShip.CapacitorPercentage < 31 && DirectEve.ActiveShip.ShieldPercentage > 90)
                            //    return true;

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                            {
                                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                                    return false;
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                            {
                                if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 2)
                                    return false;
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn ||
                                AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn ||
                                AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn ||
                                AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikVilaFrigateSwarmerSpawn ||
                                AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn ||
                                AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn)
                            {
                                if (!ESCache.Instance.ActiveShip.IsAssaultShip && !ESCache.Instance.ActiveShip.IsFrigate)
                                {
                                    if (AbyssalTier >= 3)
                                    {
                                        if (!Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                                        {
                                            if (!Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                                            {
                                                if (!Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
                                                {
                                                    if (2 > Combat.PotentialCombatTargets.Count(i => i.IsNPCCruiser))
                                                    {
                                                        if (4 > Combat.PotentialCombatTargets.Count(i => i.IsNPCFrigate))
                                                        {
                                                            if (HealthCheck())
                                                            {
                                                                if (30 > Time.Instance.SecondsSinceLastSessionChange)
                                                                {
                                                                    if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && i.DamagePercent >= 0))
                                                                        return true;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            if (DirectEve.ActiveShip.CapacitorPercentage < 2 && DirectEve.ActiveShip.ShieldPercentage > 95)
                            {
                                if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && i.IsActive && !i.IsInLimboState)) Log($"boolShouldWeDeactivateShieldHardener [true] due to cap too low. Perc [{DirectEve.ActiveShip.CapacitorPercentage}] Min required cap Perc [{_moduleMinCapPerc}]");
                                return true;
                            }
                        }
                        else if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && Time.Instance.SecondsSinceLastSessionChange > 30 && ESCache.Instance.Modules.Any(i => i.IsShieldHardener && i.DamagePercent > 0)))
                        {
                            if (ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener))) Log($"boolShouldWeDeactivateShieldHardener [true] due to no NPCs left: So we can try to repair shield hardener overheat damage");
                            return true;
                        }
                    }

                    return false;
                }

                return false;
            }
        }

        internal double doubleMinimumCapacitorPercentrageToAllowSecondRep
        {
            get
            {
                return .35;
            }
        }

        internal double doublePercentageToDeactivateSecondRep
        {
            get
            {
                return .90;
            }
        }

        internal double doublePercentageToActivateSecondRep
        {
            get
            {
                return .65;
            }
        }

        internal bool boolShouldWeDeactivateSecondaryShieldRep
        {
            get
            {
                try
                {
                    if (ESCache.Instance.ActiveShip.Entity.ShieldPct > doublePercentageToDeactivateSecondRep)
                    {
                        Log($"Turning off Second Shield Booster: Shield % [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "] > [" + doublePercentageToDeactivateSecondRep + "]");
                        return true;
                    }

                    if (doubleMinimumCapacitorPercentrageToAllowSecondRep > ESCache.Instance.ActiveShip.CapacitorPercentage)
                    {
                        Log($"Turning off Second Shield Booster: low capacitor: Cap % [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 2) + "] is less than [" + doubleMinimumCapacitorPercentrageToAllowSecondRep + "]");
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return true;
                }
            }
        }

        internal bool boolShouldWeDeactivateSecondaryArmorRep
        {
            get
            {
                try
                {
                    if (ESCache.Instance.ActiveShip.Entity.ArmorPct > doublePercentageToDeactivateSecondRep)
                    {
                        Log($"Turning off Second Armor Rep: Armor % [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "] > [" + doublePercentageToDeactivateSecondRep + "]");
                        return true;
                    }

                    if (doubleMinimumCapacitorPercentrageToAllowSecondRep > ESCache.Instance.ActiveShip.CapacitorPercentage)
                    {
                        Log($"Turning off Second Armor rep: low capacitor: Cap % [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 2) + "] is less than [" + doubleMinimumCapacitorPercentrageToAllowSecondRep + "]");
                        return true;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return true;
                }
            }
        }

        internal bool boolShouldWeActivateSecondaryShieldRep
        {
            get
            {
                try
                {
                    if (doubleMinimumCapacitorPercentrageToAllowSecondRep > ESCache.Instance.ActiveShip.CapacitorPercentage)
                    {
                        if (DebugConfig.DebugDefense) Log("CapacitorPercentage [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 2) + "] is lower than doubleMinimumCapacitorPercentrageToAllowSecondShieldBooster [" + doubleMinimumCapacitorPercentrageToAllowSecondRep + "]");
                        return false;
                    }

                    if (shieldRepairModules.Count(i => i.IsShieldRepairModule) > 1)
                    {
                        if (ESCache.Instance.ActiveShip.Entity.ShieldPct > doublePercentageToActivateSecondRep)
                        {
                            if (DebugConfig.DebugDefense) Log("ShieldPct [" + Math.Round(ESCache.Instance.ActiveShip.Entity.ShieldPct, 2) + "] is higher than doubleShieldPercentageToActivateSecondShieldBooster [" + doublePercentageToActivateSecondRep + "]");
                            return false;
                        }

                        return true;
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return true;
                }
            }
        }

        internal bool boolShouldWeActivateSecondaryArmorRep
        {
            get
            {
                try
                {
                    IEnumerable<DirectUIModule> repairModules = DirectEve.Modules.Where(e => (e.GroupId == (int)Group.ShieldBoosters || e.GroupId == (int)Group.ArmorRepairer) && e.IsOnline);

                    if (doubleMinimumCapacitorPercentrageToAllowSecondRep > ESCache.Instance.ActiveShip.CapacitorPercentage)
                    {
                        if (DebugConfig.DebugDefense) Log("CapacitorPercentage [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 2) + "] is lower than doubleMinimumCapacitorPercentrageToAllowSecondShieldBooster [" + doubleMinimumCapacitorPercentrageToAllowSecondRep + "]");
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.IsArmorTanked)
                    {
                        if (repairModules.Count(i => i.IsArmorRepairModule) > 1)
                        {
                            if (ESCache.Instance.ActiveShip.Entity.ArmorPct > doublePercentageToActivateSecondRep)
                            {
                                if (DebugConfig.DebugDefense) Log("ArmorPct [" + Math.Round(ESCache.Instance.ActiveShip.Entity.ArmorPct, 2) + "] is higher than doubleShieldPercentageToActivateSecondShieldBooster [" + doublePercentageToActivateSecondRep + "]");
                                return false;
                            }

                            return true;
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return true;
                }
            }
        }

        internal bool boolShouldWeActivateShieldRep
        {
            get
            {
                try
                {
                    //if (boolShouldWeDeactivateRep)
                    //    return false;

                    if (Combat.CombatShipName.ToLower() != ESCache.Instance.ActiveShip.GivenName.ToLower())
                    {
                        if (ESCache.Instance.ActiveShip.ShieldPercentage > 99)
                            return false;
                    }

                    //Cap is too low
                    if (_repairerMinCapPerc > ESCache.Instance.ActiveShip.CapacitorPercentage)
                    {
                        Log("_shieldBoosterMinCapPerc [" + _repairerMinCapPerc + "] > C [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%])");
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.CapConservationModeNeeded)
                    {
                        //Cap is too low
                        if (20 > ESCache.Instance.ActiveShip.CapacitorPercentage)
                        {
                            Log("20 > C [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%] CapConservationModeNeeded [" + ESCache.Instance.ActiveShip.CapConservationModeNeeded + "]");
                            return false;
                        }

                        if (ESCache.Instance.ActiveShip.ShieldPercentage > 60)
                        {
                            Log("S [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) + "%] > 60% CapConservationModeNeeded [" + ESCache.Instance.ActiveShip.CapConservationModeNeeded + "]");
                            return false;
                        }

                        //return true;
                    }

                    if (ESCache.Instance.DirectEve.Modules.Any(i => i.GroupId == (int)Group.CapacitorInjector))
                    {
                        if (3 > Combat.PotentialCombatTargets.Count(i => i.IsAttacking))
                        {
                            if (DirectEve.ActiveShip.ShieldPercentage > 94)
                            {
                                if (ESCache.Instance.InAbyssalDeadspace && DirectEve.Interval(15000)) Log($"Not Activating Shield Booster: Fewer than 3 NPCs attacking and Shield % [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "]");
                                return false;
                            }
                        }
                    }

                    if (ESCache.Instance.DirectEve.Modules.Any(i => i.GroupId == (int)Group.RemoteShieldRepairer && i.IsActive))
                    {
                        if (3 > Combat.PotentialCombatTargets.Count(i => i.IsAttacking))
                        {
                            if (DirectEve.ActiveShip.ShieldPercentage > 94)
                            {
                                if (ESCache.Instance.InAbyssalDeadspace && DirectEve.Interval(15000)) Log($"Not Activating Shield Booster: RemoteShieldRepairer [isActive] Fewer than 3 NPCs attacking and Shield % [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "]");
                                return false;
                            }
                        }
                    }

                    if (39 > ESCache.Instance.ActiveShip.CapacitorPercentage)
                    {
                        if (_repEnablePerc > ESCache.Instance.ActiveShip.ShieldPercentage)
                        {
                            Log("_repEnableShieldPerc [" + _repEnablePerc + "] > S [" + ESCache.Instance.ActiveShip.ShieldPercentage + "%])");
                            return false;
                        }
                    }

                    if (!Combat.PotentialCombatTargets.Any(i => i.Velocity > 0))
                    {
                        if (DebugConfig.DebugDefense) Log("if (!Combat.PotentialCombatTargets.Any(i => i.Velocity > 0))");
                        if (DirectEve.ActiveShip.ShieldPercentage > 99 && Time.Instance.SecondsSinceLastSessionChange > 30)
                        {
                            if (DirectEve.Interval(15000)) Log("Leaving Shield Booster off allow time to repair: No NPCs and Shield % [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "] Armor % [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "]");
                            return false;
                        }
                    }

                    if (DirectEve.Session.IsAbyssalDeadspace)
                    {
                        if (39 > ESCache.Instance.ActiveShip.CapacitorPercentage || (_repEnablePerc > 100 && ESCache.Instance.ActiveShip.IsFrigate))
                        {
                            if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                            {
                                Log("_shieldBoosterEnableShieldPerc [" + _repEnablePerc + "] > S [" + ESCache.Instance.ActiveShip.ShieldPercentage + "%])");
                                return false;
                            }

                            if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) && ESCache.Instance.ActiveShip.ShieldPercentage > 99)
                            {
                                Log("No NPCs and S [" + ESCache.Instance.ActiveShip.ShieldPercentage + " ] > 99%");
                                return false;
                            }
                        }

                        return true;
                    }
                    // what do we do in normal space during pvp?

                    if (ESCache.Instance.Citadels.Any(i => 5000 > i.Distance) || ESCache.Instance.Stations.Any(i => 5000 > i.Distance))
                    {
                        if (ESCache.Instance.Modules.Any(i => !i.IsOnline))
                        {
                            if (ESCache.Instance.ActiveShip.IsShieldTanked && 95 > ESCache.Instance.ActiveShip.ShieldPercentage)
                                return false;
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return true;
                }
            }
        }

        internal bool boolShouldWeActivateArmorRep
        {
            get
            {
                try
                {
                    if (shieldRepairModules.Any() && ESCache.Instance.ActiveShip.ArmorPercentage > 99)
                        return false;

                    if (Combat.CombatShipName.ToLower() != ESCache.Instance.ActiveShip.GivenName.ToLower())
                    {
                        if (ESCache.Instance.ActiveShip.ArmorPercentage > 99)
                            return false;
                    }

                    //Cap is too low
                    if (_repairerMinCapPerc > ESCache.Instance.ActiveShip.CapacitorPercentage)
                    {
                        Log("_shieldBoosterMinCapPerc [" + _repairerMinCapPerc + "] > C [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%])");
                        return false;
                    }

                    if (ESCache.Instance.ActiveShip.CapConservationModeNeeded)
                    {
                        //Cap is too low
                        if (20 > ESCache.Instance.ActiveShip.CapacitorPercentage)
                        {
                            Log("20 > C [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "%] CapConservationModeNeeded [" + ESCache.Instance.ActiveShip.CapConservationModeNeeded + "]");
                            return false;
                        }

                        if (ESCache.Instance.ActiveShip.ArmorPercentage > 60)
                        {
                            Log("A [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0) + "%] > 60% CapConservationModeNeeded [" + ESCache.Instance.ActiveShip.CapConservationModeNeeded + "]");
                            return false;
                        }

                        //return true;
                    }

                    if (ESCache.Instance.DirectEve.Modules.Any(i => i.GroupId == (int)Group.CapacitorInjector))
                    {
                        if (3 > Combat.PotentialCombatTargets.Count(i => i.IsAttacking))
                        {
                            if (DirectEve.ActiveShip.ArmorPercentage > 94)
                            {
                                if (ESCache.Instance.InAbyssalDeadspace && DirectEve.Interval(15000)) Log($"Not Activating Rep: Fewer than 3 NPCs attacking and Armor % [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "]");
                                return false;
                            }
                        }
                    }

                    if (ESCache.Instance.DirectEve.Modules.Any(i => i.GroupId == (int)Group.RemoteShieldRepairer && i.IsActive))
                    {
                        if (3 > Combat.PotentialCombatTargets.Count(i => i.IsAttacking))
                        {
                            if (ESCache.Instance.ActiveShip.IsArmorTanked && DirectEve.ActiveShip.ArmorPercentage > 94)
                            {
                                if (ESCache.Instance.InAbyssalDeadspace && DirectEve.Interval(15000)) Log($"Not Activating Rep: RemoteShieldRepairer [isActive] Fewer than 3 NPCs attacking and Armor % [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "]");
                                return false;
                            }
                        }
                    }

                    if (39 > ESCache.Instance.ActiveShip.CapacitorPercentage)
                    {
                        if (_repEnablePerc > ESCache.Instance.ActiveShip.ArmorPercentage)
                        {
                            Log("_repEnableShieldPerc [" + _repEnablePerc + "] > A [" + ESCache.Instance.ActiveShip.ArmorPercentage + "%])");
                            return false;
                        }
                    }

                    if (!Combat.PotentialCombatTargets.Any(i => i.Velocity > 0))
                    {
                        if (DebugConfig.DebugDefense) Log("if (!Combat.PotentialCombatTargets.Any(i => i.Velocity > 0))");
                        if (DirectEve.ActiveShip.ArmorPercentage > 99 && Time.Instance.SecondsSinceLastSessionChange > 30)
                        {
                            if (DirectEve.Interval(15000)) Log("Leaving Shield Booster off allow time to repair: No NPCs and Shield % [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "] Armor % [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "]");
                            return false;
                        }
                    }

                    if (DirectEve.Session.IsAbyssalDeadspace)
                    {
                        if (39 > ESCache.Instance.ActiveShip.CapacitorPercentage || (_repEnablePerc > 100 && ESCache.Instance.ActiveShip.IsFrigate))
                        {
                            if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                            {
                                Log("_shieldBoosterEnableShieldPerc [" + _repEnablePerc + "] > S [" + ESCache.Instance.ActiveShip.ShieldPercentage + "%])");
                                return false;
                            }

                            if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) && ESCache.Instance.ActiveShip.IsArmorTanked && ESCache.Instance.ActiveShip.ArmorPercentage > 99)
                            {
                                Log("No NPCs and A [" + ESCache.Instance.ActiveShip.ArmorPercentage + " ] > 99%");
                                return false;
                            }
                        }

                        return true;
                    }
                    // what do we do in normal space during pvp?

                    if (ESCache.Instance.Citadels.Any(i => 5000 > i.Distance) || ESCache.Instance.Stations.Any(i => 5000 > i.Distance))
                    {
                        if (ESCache.Instance.Modules.Any(i => !i.IsOnline))
                        {
                            if (95 > ESCache.Instance.ActiveShip.ArmorPercentage)
                                return false;
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                    return true;
                }
            }
        }

        internal bool boolShouldWeDeactivateShieldRep
        {
            get
            {
                try
                {
                    if (DebugConfig.DebugDefense) Log("DebugDefense: boolShouldWeDeactivateRep: IsShieldTanked [" + ESCache.Instance.ActiveShip.IsShieldTanked + "] IsArmorTanked [" + ESCache.Instance.ActiveShip.IsArmorTanked + "]");

                    if (DirectEve.Session.IsAbyssalDeadspace)
                    {
                        if (ESCache.Instance.AbyssalCenter.Distance > ESCache.Instance.SafeDistanceFromAbyssalCenter)
                        {
                            if (DebugConfig.DebugDefense) Log("if (ESCache.Instance.AbyssalCenter.Distance [" + ESCache.Instance.AbyssalCenter.Distance + "] > ESCache.Instance.SafeDistanceFromAbyssalCenter [" + ESCache.Instance.SafeDistanceFromAbyssalCenter + "]) return true");
                            return false;
                        }

                        if (39 > ESCache.Instance.ActiveShip.CapacitorPercentage || (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                        {
                            if (DebugConfig.DebugDefense) Log("if (39 > ESCache.Instance.ActiveShip.CapacitorPercentage [" + ESCache.Instance.ActiveShip.CapacitorPercentage + "] || (ESCache.Instance.ActiveShip.IsFrigate [" + ESCache.Instance.ActiveShip.IsFrigate + "] || ESCache.Instance.ActiveShip.IsAssaultShip [" + ESCache.Instance.ActiveShip.IsAssaultShip + "])");
                            if (AllRatsHaveBeenCleared && AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)
                            {
                                if (DebugConfig.DebugDefense) Log("if (AllRatsHaveBeenCleared) return true");
                                if (DebugConfig.DebugDefense) Log("if (ESCache.Instance.ActiveShip.IsShieldTanked)");
                                if (ESCache.Instance.ActiveShip.ShieldPercentage > 99)
                                {
                                    Log("No NPCs and S [" + ESCache.Instance.ActiveShip.ShieldPercentage + "%])");
                                    return true;
                                }
                            }
                        }

                        if (ESCache.Instance.DirectEve.Modules.Any(i => i.GroupId == (int)Group.CapacitorInjector))
                        {
                            if (3 > Combat.PotentialCombatTargets.Count(i => i.IsAttacking))
                            {
                                if (DirectEve.ActiveShip.ShieldPercentage > 94)
                                {
                                    Log($"Turning off Shield Booster: Fewer than 3 NPCs attacking and Shield % [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "]");
                                    return true;
                                }
                            }
                        }

                        if (ESCache.Instance.ActiveShip.CapConservationModeNeeded)
                        {
                            if (DebugConfig.DebugDefense) Log("DebugDefense: CapConservationModeNeeded [true]");
                            if (ESCache.Instance.ActiveShip.ShieldPercentage >= 95)
                            {
                                Log("CapConservationModeNeeded [" + ESCache.Instance.ActiveShip.CapConservationModeNeeded + "] C [" + ESCache.Instance.ActiveShip.CapacitorPercentage + "] S [" + ESCache.Instance.ActiveShip.ShieldPercentage + "] > 92");
                                return true;
                            }
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                                return false;
                        }

                        if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                        {
                            if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 2)
                                return false;
                        }

                        if (ESCache.Instance.DirectEve.Modules.Any(i => (i.GroupId == (int)Group.RemoteShieldRepairer || i.GroupId == (int)Group.RemoteArmorRepairer) && i.IsActive))
                        {
                            if (3 > Combat.PotentialCombatTargets.Count(i => i.IsAttacking) && AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                            {
                                if (DirectEve.ActiveShip.ShieldPercentage > 94)
                                {
                                    Log($"Turning off Shield Booster: RemoteShieldRepairer [isActive] Fewer than 3 NPCs attacking and Shield % [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "]");
                                    return true;
                                }
                            }
                        }


                        /**
                        //just let the cap run out if we are this low?
                        if (DirectEve.ActiveShip.CapacitorPercentage < _repairerMinCapPerc) //5%?
                        {
                            Log($"Turning off Rep due to cap too low. Perc [{Math.Round(DirectEve.ActiveShip.CapacitorPercentage, 0)}] Min required cap Perc [{_repairerMinCapPerc}]");
                            return true;
                        }
                        **/

                        //if (DirectEve.ActiveShip.CapacitorPercentage < 31 && DirectEve.ActiveShip.ShieldPercentage > 90)
                        //    return true;

                        /**
                        if (DirectEve.ActiveShip.CapacitorPercentage < _repairerMinCapPerc) //5%?
                        {
                            if (ESCache.Instance.ActiveShip.IsShieldTanked)
                            {
                                if (DirectEve.ActiveShip.ShieldPercentage > 95 && ESCache.Instance.ActiveShip.GroupId != (int)Group.Cruiser)
                                {
                                    Log($"Turning off Shield Booster due to cap too low. C% [{Math.Round(DirectEve.ActiveShip.CapacitorPercentage, 2)}] Min required cap Perc [{_repairerMinCapPerc}]");
                                    return true;
                                }
                            }
                            else if (ESCache.Instance.ActiveShip.IsArmorTanked)
                            {
                                if (DirectEve.ActiveShip.ArmorPercentage > 95 && ESCache.Instance.ActiveShip.GroupId != (int)Group.Cruiser)
                                {
                                    Log($"Turning off Rep due to cap too low. C% [{Math.Round(DirectEve.ActiveShip.CapacitorPercentage, 2)}] Min required cap Perc [{_repairerMinCapPerc}]");
                                    return true;
                                }
                            }
                        }
                        **/

                        if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                        {
                            if (DirectEve.ActiveShip.ShieldPercentage > 99 && Time.Instance.SecondsSinceLastSessionChange > 30 && ESCache.Instance.Modules.Any(i => i.IsShieldRepairModule))
                            {
                                var ShieldBooster = ESCache.Instance.Modules.FirstOrDefault(i => i.IsShieldRepairModule && i.DamagePercent > Combat.WeaponOverloadDamageAllowed);
                                if (ShieldBooster != null)
                                {
                                    Log($"Turning off Shield Booster: No NPCs and Shield % [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "] DamagePercent [" + ShieldBooster.DamagePercent + "] > [65%]");
                                    return true;
                                }
                            }
                        }
                    }
                    else if (Combat.CombatShipName.ToLower() != ESCache.Instance.ActiveShip.GivenName.ToLower() || ESCache.Instance.ActiveShip.IsFrigate)
                    {
                        if (ESCache.Instance.ActiveShip.ShieldPercentage > 99)
                            return true;
                    }

                    if (DirectEve.ActiveShip.CapacitorPercentage < _repairerMinCapPerc)
                    {
                        Log($"Turning off Rep due to cap too low. Perc [{DirectEve.ActiveShip.CapacitorPercentage}] Min required cap Perc [{_repairerMinCapPerc}]");
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

        internal bool boolShouldWeDeactivateArmorRep
        {
            get
            {
                try
                {
                    if (DebugConfig.DebugDefense) Log("DebugDefense: boolShouldWeDeactivateRep: IsShieldTanked [" + ESCache.Instance.ActiveShip.IsShieldTanked + "] IsArmorTanked [" + ESCache.Instance.ActiveShip.IsArmorTanked + "]");

                    if (DirectEve.Session.IsAbyssalDeadspace)
                    {
                        if (ESCache.Instance.AbyssalCenter.Distance > ESCache.Instance.SafeDistanceFromAbyssalCenter)
                        {
                            if (DebugConfig.DebugDefense) Log("if (ESCache.Instance.AbyssalCenter.Distance [" + ESCache.Instance.AbyssalCenter.Distance + "] > ESCache.Instance.SafeDistanceFromAbyssalCenter [" + ESCache.Instance.SafeDistanceFromAbyssalCenter + "]) return true");
                            return false;
                        }

                        if (39 > ESCache.Instance.ActiveShip.CapacitorPercentage || (ESCache.Instance.ActiveShip.IsFrigate || ESCache.Instance.ActiveShip.IsAssaultShip))
                        {
                            if (DebugConfig.DebugDefense) Log("if (39 > ESCache.Instance.ActiveShip.CapacitorPercentage [" + ESCache.Instance.ActiveShip.CapacitorPercentage + "] || (ESCache.Instance.ActiveShip.IsFrigate [" + ESCache.Instance.ActiveShip.IsFrigate + "] || ESCache.Instance.ActiveShip.IsAssaultShip [" + ESCache.Instance.ActiveShip.IsAssaultShip + "])");
                            if (AllRatsHaveBeenCleared && AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.FourteenBattleshipSpawn)
                            {
                                if (DebugConfig.DebugDefense) Log("if (AllRatsHaveBeenCleared) return true");
                                if (ESCache.Instance.ActiveShip.ArmorPercentage > 99)
                                {
                                    Log("No NPCs and A [" + ESCache.Instance.ActiveShip.ArmorPercentage + "%])");
                                    return true;
                                }
                            }
                        }

                        if (ESCache.Instance.DirectEve.Modules.Any(i => i.GroupId == (int)Group.CapacitorInjector))
                        {
                            if (3 > Combat.PotentialCombatTargets.Count(i => i.IsAttacking))
                            {
                                if (DirectEve.ActiveShip.ArmorPercentage > 94)
                                {
                                    Log($"Turning off Shield Booster: Fewer than 3 NPCs attacking and Shield % [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "]");
                                    return true;
                                }
                            }
                        }

                        if (ESCache.Instance.ActiveShip.CapConservationModeNeeded)
                        {
                            if (DebugConfig.DebugDefense) Log("DebugDefense: CapConservationModeNeeded [true]");
                            if (ESCache.Instance.ActiveShip.ArmorPercentage >= 95)
                            {
                                Log("CapConservationModeNeeded [" + ESCache.Instance.ActiveShip.CapConservationModeNeeded + "] ArmorPercentage [" + ESCache.Instance.ActiveShip.ArmorPercentage + "] > 92");
                                return true;
                            }
                        }

                        if (!shieldRepairModules.Any())
                        {
                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                            {
                                if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                                    return false;
                            }

                            if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                            {
                                if (Combat.PotentialCombatTargets.Count(i => i.IsNPCBattleship) >= 2)
                                    return false;
                            }
                        }

                        if (ESCache.Instance.DirectEve.Modules.Any(i => (i.GroupId == (int)Group.RemoteShieldRepairer || i.GroupId == (int)Group.RemoteArmorRepairer) && i.IsActive))
                        {
                            if (3 > Combat.PotentialCombatTargets.Count(i => i.IsAttacking) && AbyssalSpawn.DetectSpawn != AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                            {
                                if (DirectEve.ActiveShip.ArmorPercentage > 94)
                                {
                                    Log($"Turning off Shield Booster: RemoteRepairer [isActive] Fewer than 3 NPCs attacking and Armor % [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "]");
                                    return true;
                                }
                            }
                        }


                        /**
                        //just let the cap run out if we are this low?
                        if (DirectEve.ActiveShip.CapacitorPercentage < _repairerMinCapPerc) //5%?
                        {
                            Log($"Turning off Rep due to cap too low. Perc [{Math.Round(DirectEve.ActiveShip.CapacitorPercentage, 0)}] Min required cap Perc [{_repairerMinCapPerc}]");
                            return true;
                        }
                        **/

                        //if (DirectEve.ActiveShip.CapacitorPercentage < 31 && DirectEve.ActiveShip.ShieldPercentage > 90)
                        //    return true;

                        /**
                        if (DirectEve.ActiveShip.CapacitorPercentage < _repairerMinCapPerc) //5%?
                        {
                            if (ESCache.Instance.ActiveShip.IsShieldTanked)
                            {
                                if (DirectEve.ActiveShip.ShieldPercentage > 95 && ESCache.Instance.ActiveShip.GroupId != (int)Group.Cruiser)
                                {
                                    Log($"Turning off Shield Booster due to cap too low. C% [{Math.Round(DirectEve.ActiveShip.CapacitorPercentage, 2)}] Min required cap Perc [{_repairerMinCapPerc}]");
                                    return true;
                                }
                            }
                            else if (ESCache.Instance.ActiveShip.IsArmorTanked)
                            {
                                if (DirectEve.ActiveShip.ArmorPercentage > 95 && ESCache.Instance.ActiveShip.GroupId != (int)Group.Cruiser)
                                {
                                    Log($"Turning off Rep due to cap too low. C% [{Math.Round(DirectEve.ActiveShip.CapacitorPercentage, 2)}] Min required cap Perc [{_repairerMinCapPerc}]");
                                    return true;
                                }
                            }
                        }
                        **/

                        if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                        {
                            if (DirectEve.ActiveShip.ArmorPercentage > 99 && Time.Instance.SecondsSinceLastSessionChange > 30 && ESCache.Instance.Modules.Any(i => i.IsArmorRepairModule))
                            {
                                var ArmorrepairModule = ESCache.Instance.Modules.FirstOrDefault(i => i.IsArmorRepairModule && i.DamagePercent > Combat.WeaponOverloadDamageAllowed);
                                if (ArmorrepairModule != null)
                                {
                                    Log($"Turning off Rep: No NPCs and Armor % [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "] DamagePercent [" + ArmorrepairModule.DamagePercent + "] > [65%]");
                                    return true;
                                }
                            }
                        }
                    }
                    else if (Combat.CombatShipName.ToLower() != ESCache.Instance.ActiveShip.GivenName.ToLower() || ESCache.Instance.ActiveShip.IsFrigate)
                    {
                        if (ESCache.Instance.ActiveShip.ArmorPercentage > 99)
                            return true;
                    }

                    if (DirectEve.ActiveShip.CapacitorPercentage < _repairerMinCapPerc)
                    {
                        Log($"Turning off Rep due to cap too low. Perc [{DirectEve.ActiveShip.CapacitorPercentage}] Min required cap Perc [{_repairerMinCapPerc}]");
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

        internal bool boolShouldWeActivateCapInjector
        {
            get
            {
                if (!DirectEve.Session.IsAbyssalDeadspace)
                {
                    if (!ESCache.Instance.EntitiesNotSelf.Any(i => i.IsPlayer && i.IsAttacking))
                        return false;
                }

                if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure))
                    return false;

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.Name.Contains("Lucifer Cynabal")) >= 2)
                    {
                        if (60 > ESCache.Instance.ActiveShip.CapacitorPercentage)
                            return true;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.Name.Contains("Vedmak")) > 2)
                    {
                        if (55 > ESCache.Instance.ActiveShip.CapacitorPercentage)
                            return true;
                    }
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LeshakBSSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count(i => i.Name.Contains("Leshak")) > 2)
                    {
                        if (55 > ESCache.Instance.ActiveShip.CapacitorPercentage)
                            return true;
                    }
                }

                if (ESCache.Instance.ActiveShip.CapacitorPercentage > _capacitorInjectorInjectCapPerc)
                    return false;

                return true;
            }
        }

        internal bool boolShouldWeDeactivateCapInjector
        {
            get
            {
                if (DirectEve.Session.IsAbyssalDeadspace)
                {
                    if (_capacitorInjectorWaitCapPerc > DirectEve.ActiveShip.CapacitorPercentage)
                    {
                        Log("Turning off Cap Injector. Capacitor% [" + DirectEve.ActiveShip.CapacitorPercentage + "] > [" + _capacitorInjectorWaitCapPerc + "]");
                        return false;
                    }

                    return true;
                }

                return false;
            }
        }

        internal bool ManageHardeners()
        {
            if (!ESCache.Instance.Modules.Any())
                return true;

            foreach (var hardener in ESCache.Instance.Modules.Where(e => e.IsShieldHardener || e.IsArmorHardener))
            {
                //Deactivate if needed
                if (hardener.IsActive)
                {
                    if (boolShouldWeDeactivateHardener)
                    {
                        if (!hardener.IsInLimboState)
                        {
                            if (DirectEve.Interval(800, 1500, hardener.ItemId.ToString()))
                            {
                                if (hardener.Click())
                                {
                                    Log("Deactivating Hardener [" + hardener.TypeName + "]");
                                    return true;
                                }

                                continue;
                            }
                        }
                        else
                        {
                            if (DebugConfig.DebugDefense) Log("[" + hardener.TypeName + "] IsInLimboState [" + hardener.IsInLimboState + "] IsOnline [" + hardener.IsOnline + "] IsActivatable [" + hardener.IsActivatable + "] IsDeactivating [" + hardener.IsDeactivating + "] IsReloadingAmmo [" + hardener.IsReloadingAmmo + "] IsBeingRepaired [" + hardener._module.IsBeingRepaired + "] EffectActivating [" + hardener._module.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(hardener._module) + "] ReactivationDelay [" + hardener._module.ReactivationDelay + " > 0]");
                            continue;
                        }
                    }
                }

                //Cancel Repair if needed
                if (!AllRatsHaveBeenCleared)
                {
                    if (hardener._module.IsBeingRepaired)
                    {
                        // cancel repair
                        if (hardener._module.CancelRepair())
                        {
                            if (!AllRatsHaveBeenCleared)
                            {
                                Log($"Canceling repair on [{hardener.TypeName}] due to AllRatsHaveBeenCleared [false]");
                            }

                            return true;
                        }
                    }
                }

                //Repair if needed
                if (boolShouldWeDeactivateHardener && !hardener.IsActive)
                {
                    if (!hardener._module.IsBeingRepaired && hardener._module.HeatDamagePercent > 5)
                    {
                        var shipsCargo = ESCache.Instance.DirectEve.GetShipsCargo();

                        if (shipsCargo != null)
                        {
                            if (shipsCargo.Items.Any(i => i.TypeId == _naniteRepairPasteTypeId))
                            {
                                if (!ShouldWeRepair)
                                    continue;

                                // repair
                                if (hardener._module.Repair())
                                {
                                    Log($"Repairing [{hardener.TypeName}].");
                                    return true;
                                }
                            }

                            if (!shipsCargo.Items.Any(i => i.TypeId == _naniteRepairPasteTypeId))
                            {
                                DirectEve.IntervalLog(60000, 60000,
                                    "No nanite repair paste found in cargo, can't repair.");
                            }
                        }
                    }
                }

                if (hardener.IsActive)
                    continue;

                if (hardener.IsInLimboState)
                {
                    if (DebugConfig.DebugDefense) Log("[" + hardener.TypeName + "] IsInLimboState [" + hardener.IsInLimboState + "] IsOnline [" + hardener.IsOnline + "] IsActivatable [" + hardener.IsActivatable + "] IsDeactivating [" + hardener.IsDeactivating + "] IsReloadingAmmo [" + hardener.IsReloadingAmmo + "] IsBeingRepaired [" + hardener._module.IsBeingRepaired + "] EffectActivating [" + hardener._module.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(hardener._module) + "] ReactivationDelay [" + hardener._module.ReactivationDelay + " > 0]");
                    continue;
                }

                //if (!anyTargets)
                //    continue;

                if (_moduleMinCapPerc >= ESCache.Instance.ActiveShip.CapacitorPercentage)
                    continue;

                if (!ESCache.Instance.InAbyssalDeadspace)
                {
                    if (Time.Instance.LastInWarp.AddMilliseconds(10) > DateTime.UtcNow)
                        continue;
                }

                if (!Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) && Time.Instance.SecondsSinceLastSessionChange > 30 && ESCache.Instance.Modules.Any(i => (i.IsShieldHardener || i.IsArmorHardener) && i.DamagePercent > 0))
                {
                    if (DirectEve.Interval(5000)) Log($"Leaving hardener [" + hardener.TypeName + "] off: due to no NPCs left: So we can try to repair the module");
                    continue; ;
                }

                if (boolShouldWeDeactivateHardener)
                    continue;

                //Activate
                if (DirectEve.Interval(800, 1500, hardener.ItemId.ToString()))
                {
                    if (hardener.Click())
                    {
                        Log($"Activating hardener [{hardener.TypeName}]");
                        return true;
                    }

                    continue;
                }
            }


            return false;
        }

        internal bool ManageCapInjectors()
        {
            if (!ESCache.Instance.Modules.Any())
                return true;

            var anyTargets = Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure);

            var capPerc = DirectEve.ActiveShip.CapacitorPercentage;

            //CapacitorInjector
            foreach (var injector in DirectEve.Modules.Where(e => e.GroupId == (int)Group.CapacitorInjector))
            {
                if (injector.IsActive)
                {
                    if (boolShouldWeDeactivateCapInjector)
                    {
                        if (!injector.IsInLimboState)
                        {
                            Log($"Disabling [{injector.TypeName}] boolShouldWeDeactivateCapInjector [" + boolShouldWeDeactivateCapInjector + "] C% [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "] S% [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) + "]");
                            if (DirectEve.Interval(800, 1500, injector.ItemId.ToString()))
                            {
                                injector.Click();
                                return true;
                            }
                        }
                        else
                        {
                            if (DebugConfig.DebugDefense) Log("[" + injector.TypeName + "] IsInLimboState [" + injector.IsInLimboState + "] IsOnline [" + injector.IsOnline + "] IsActivatable [" + injector.IsActivatable + "] IsDeactivating [" + injector.IsDeactivating + "] IsReloadingAmmo [" + injector.IsReloadingAmmo + "] IsBeingRepaired [" + injector.IsBeingRepaired + "] EffectActivating [" + injector.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(injector) + "] ReactivationDelay [" + injector.ReactivationDelay + " > 0]");
                            continue;
                        }
                    }
                }

                if (DirectEve.Me.IsInAbyssalSpace() && anyTargets || _anyOverheat || !IsOurShipWithintheAbyssBounds())
                {
                    if (injector.IsBeingRepaired)
                    {
                        // cancel repair
                        if (injector.CancelRepair())
                        {
                            Log($"Canceling repair on [" + injector.TypeName + "] due to: anyTargets [" + anyTargets + "] _anyOverheat [" + _anyOverheat + "] !IsOurShipWithintheAbyssBounds [" + IsOurShipWithintheAbyssBounds() + "]");
                            return true;
                        }
                    }
                }

                if (DirectEve.Me.IsInAbyssalSpace() && !anyTargets && IsOurShipWithintheAbyssBounds())
                {
                    if (!injector.IsBeingRepaired && injector.HeatDamagePercent > 5)
                    {
                        var shipsCargo = ESCache.Instance.DirectEve.GetShipsCargo();

                        if (shipsCargo != null)
                        {
                            if (!shipsCargo.Items.Any(i => i.TypeId == _naniteRepairPasteTypeId))
                            {
                                if (!ShouldWeRepair)
                                    continue;

                                // repair
                                if (injector.Repair())
                                {
                                    Log($"Repairing TypeName[{injector.TypeName}].");
                                    return true;
                                }
                            }

                            if (!shipsCargo.Items.Any(i => i.TypeId == _naniteRepairPasteTypeId))
                            {
                                DirectEve.IntervalLog(60000, 60000, "No nanite repair paste found in cargo, can't repair.");
                                continue;
                            }
                        }
                    }
                }

                if (injector.IsActive)
                    continue;

                if (injector.IsInLimboState)
                {
                    if (DebugConfig.DebugDefense) Log("[" + injector.TypeName + "] IsInLimboState [" + injector.IsInLimboState + "] IsOnline [" + injector.IsOnline + "] IsActivatable [" + injector.IsActivatable + "] IsDeactivating [" + injector.IsDeactivating + "] IsReloadingAmmo [" + injector.IsReloadingAmmo + "] IsBeingRepaired [" + injector.IsBeingRepaired + "] EffectActivating [" + injector.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(injector) + "] ReactivationDelay [" + injector.ReactivationDelay + " > 0]");
                    continue;
                }

                if (injector.Charge == null)
                {
                    if (injector.CanBeReloaded && !injector.IsReloadingAmmo && !injector.IsInLimboState)
                    {
                        var thisStackOfCapBoosters = ESCache.Instance.CheckCargoForItem(_capBoosterTypeId, 1);
                        if (thisStackOfCapBoosters != null)
                        {
                            injector.ReloadAmmo(thisStackOfCapBoosters);
                            continue;
                        }
                    }
                }

                if (injector.Charge == null)
                    continue;

                if (boolShouldWeActivateCapInjector)
                {
                    if (!DirectEve.Interval(800, 1500, injector.ItemId.ToString()))
                        return true;

                    Log($"Activating [{injector.TypeName}]");
                    injector.Click();
                    return true;
                }
            }

            return false;
        }

        //this eventually needs to handle ancillary boosters and armor repairers: right now we only do shields
        internal IEnumerable<DirectUIModule> allRepairModules
        {
            get
            {
                return shieldRepairModules.Concat(armorRepairModules).Distinct().ToList();
            }
        }

        internal IEnumerable<DirectUIModule> shieldRepairModules
        {
            get
            {
                return DirectEve.Modules.Where(e => (e.GroupId == (int)Group.ShieldBoosters || e.GroupId == (int)Group.AncillaryShieldBooster) && e.IsOnline);
            }
        }

        internal IEnumerable<DirectUIModule> armorRepairModules
        {
            get
            {
                return DirectEve.Modules.Where(e => (e.GroupId == (int)Group.ArmorRepairer || e.GroupId == (int)Group.AncillaryArmorBooster) && e.IsOnline);
            }
        }

        internal bool ManageRepairers()
        {
            try
            {
                if (!ESCache.Instance.Modules.Any())
                    return true;

                if (DebugConfig.DebugDefense) Log("DebugDefense: ManageRepairers");

                var capPerc = DirectEve.ActiveShip.CapacitorPercentage;
                var currentShieldPct = DirectEve.ActiveShip.ShieldPercentage;
                var currentArmorPct = DirectEve.ActiveShip.ArmorPercentage;
                var currentStructurePct = DirectEve.ActiveShip.StructurePercentage;

                // Check if we received armor or structure damage since last capture
                var armorOrStrucDecreased = currentArmorPct < _lastArmorPerc || currentStructurePct < _lastStructurePerc;

                _lastArmorPerc = currentArmorPct;
                _lastStructurePerc = currentStructurePct;

                ManageShieldRepairers();
                ManageArmorRepairers();

                return false;
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
                return false;
            }
        }

        internal bool ManageShieldRepairers()
        {
            // shield boosters
            if (shieldRepairModules.Any())
            {
                foreach (var repairer in shieldRepairModules.OrderBy(x => x.TypeName.Contains("Gistum B-Type")).ThenBy(x => x.TypeName.Contains("Pithum C-Type")).ThenBy(x => x.ItemId))
                {
                    if (repairer.IsActive)
                    {
                        if (DebugConfig.DebugDefense) Log("if (repairer.IsActive)");
                        //2nd shield booster
                        if (shieldRepairModules.Count() >= 2)
                        {
                            if (DebugConfig.DebugDefense) Log("if (repairModules >= 2)");
                            if (shieldRepairModules.Count(e => e.IsActive) >= 2)
                            {
                                if (DebugConfig.DebugDefense) Log("if (repairModules && e.IsActive) >= 2)");
                                if (boolShouldWeDeactivateSecondaryShieldRep)
                                {
                                    if (DebugConfig.DebugDefense) Log("boolShouldWeDeactivateSecondRep [" + boolShouldWeDeactivateSecondaryShieldRep + "]");
                                    if (!repairer.IsInLimboState)
                                    {
                                        Log($"Disabling [{repairer.TypeName}] boolShouldWeDeactivateSecondRep [" + boolShouldWeDeactivateSecondaryShieldRep + "] C% [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "] S% [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) + "] A% [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0) + "]");
                                        if (DirectEve.Interval(800, 1500, repairer.ItemId.ToString()))
                                        {
                                            if (repairer.Click())
                                            {
                                                return true;
                                            }

                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        if (DebugConfig.DebugDefense) Log("[" + repairer.TypeName + "] IsInLimboState [" + repairer.IsInLimboState + "] IsOnline [" + repairer.IsOnline + "] IsActivatable [" + repairer.IsActivatable + "] IsDeactivating [" + repairer.IsDeactivating + "] IsReloadingAmmo [" + repairer.IsReloadingAmmo + "] IsBeingRepaired [" + repairer.IsBeingRepaired + "] EffectActivating [" + repairer.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(repairer) + "] ReactivationDelay [" + repairer.ReactivationDelay + " > 0]");
                                    }
                                }
                            }
                        }

                        //1st shield booster
                        if (boolShouldWeDeactivateShieldRep)
                        {
                            if (DebugConfig.DebugDefense) Log("boolShouldWeDeactivateRep [" + boolShouldWeDeactivateShieldRep + "]");
                            if (!repairer.IsInLimboState)
                            {
                                Log($"Disabling [{repairer.TypeName}] boolShouldWeDeactivateRep [" + boolShouldWeDeactivateShieldRep + "] C% [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "] S% [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) + "]");
                                if (DirectEve.Interval(800, 1500, repairer.ItemId.ToString()))
                                {
                                    if (repairer.Click())
                                    {
                                        return true;
                                    }

                                    continue;
                                }
                            }
                            else
                            {
                                if (DebugConfig.DebugDefense) Log("[" + repairer.TypeName + "] IsInLimboState [" + repairer.IsInLimboState + "] IsOnline [" + repairer.IsOnline + "] IsActivatable [" + repairer.IsActivatable + "] IsDeactivating [" + repairer.IsDeactivating + "] IsReloadingAmmo [" + repairer.IsReloadingAmmo + "] IsBeingRepaired [" + repairer.IsBeingRepaired + "] EffectActivating [" + repairer.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(repairer) + "] ReactivationDelay [" + repairer.ReactivationDelay + " > 0]");
                            }
                        }
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) || _anyOverheat || !IsOurShipWithintheAbyssBounds())
                    {
                        if (repairer.IsBeingRepaired)
                        {
                            // cancel repair
                            if (repairer.CancelRepair())
                            {
                                Log($"Canceling repair on [" + repairer.TypeName + "] due to: anyTargets [" + Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) + "] _anyOverheat [" + _anyOverheat + "] !IsOurShipWithintheAbyssBounds [" + IsOurShipWithintheAbyssBounds() + "]");
                                return true;
                            }
                        }
                    }

                    if (DirectEve.Me.IsInAbyssalSpace() && !Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) && IsOurShipWithintheAbyssBounds() && !_anyOverheat)
                    {
                        if (!repairer.IsBeingRepaired && repairer.HeatDamagePercent > 1)
                        {
                            var shipsCargo = ESCache.Instance.DirectEve.GetShipsCargo();

                            if (shipsCargo != null)
                            {
                                if (!shipsCargo.Items.Any(i => i.TypeId == _naniteRepairPasteTypeId))
                                {
                                    if (!ShouldWeRepair)
                                        continue;

                                    // repair
                                    if (repairer.Repair())
                                    {
                                        Log($"Repairing TypeName[{repairer.TypeName}].");
                                        return true;
                                    }
                                }

                                if (!shipsCargo.Items.Any(i => i.TypeId == _naniteRepairPasteTypeId))
                                {
                                    DirectEve.IntervalLog(60000, 60000, "No nanite repair paste found in cargo, can't repair.");
                                    continue;
                                }
                            }
                        }
                    }

                    if (repairer.IsActive)
                        continue;

                    if (repairer.IsInLimboState)
                    {
                        if (DebugConfig.DebugDefense) Log("[" + repairer.TypeName + "] IsInLimboState [" + repairer.IsInLimboState + "] IsOnline [" + repairer.IsOnline + "] IsActivatable [" + repairer.IsActivatable + "] IsDeactivating [" + repairer.IsDeactivating + "] IsReloadingAmmo [" + repairer.IsReloadingAmmo + "] IsBeingRepaired [" + repairer.IsBeingRepaired + "] EffectActivating [" + repairer.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(repairer) + "] ReactivationDelay [" + repairer.ReactivationDelay + " > 0]");
                        continue;
                    }

                    if (shieldRepairModules.Count() >= 2)
                    {
                        //This repairer isnt active, but we have one that is...
                        if (!repairer.IsActive && shieldRepairModules.Any(e => e.IsActive))
                        {
                            if (DebugConfig.DebugDefense) Log("if (!repairer.IsActive && DirectEve.Modules.Any(e => e.IsActive))");
                            if (boolShouldWeActivateSecondaryShieldRep)
                            {
                                if (DebugConfig.DebugDefense) Log("boolShouldWeActivateSecondShieldBooster [" + boolShouldWeActivateSecondaryShieldRep + "]");
                                if (!repairer.IsInLimboState)
                                {
                                    Log("Activating [" + repairer.TypeName + "] 2nd repairer - ItemID [" + repairer.ItemId + "] S%  [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "] A%  [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "] Cap% [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 2) + "]");
                                    if (repairer.Click())
                                    {
                                        return true;
                                    }

                                    continue;
                                }
                                else
                                {
                                    if (DebugConfig.DebugDefense) Log("[" + repairer.TypeName + "] IsInLimboState [" + repairer.IsInLimboState + "] IsOnline [" + repairer.IsOnline + "] IsActivatable [" + repairer.IsActivatable + "] IsDeactivating [" + repairer.IsDeactivating + "] IsReloadingAmmo [" + repairer.IsReloadingAmmo + "] IsBeingRepaired [" + repairer.IsBeingRepaired + "] EffectActivating [" + repairer.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(repairer) + "] ReactivationDelay [" + repairer.ReactivationDelay + " > 0]");
                                }
                            }

                            continue;
                        }
                    }

                    if (boolShouldWeActivateShieldRep)
                    {
                        if (!repairer.IsInLimboState)
                        {
                            if (DirectEve.Interval(800, 1500, repairer.ItemId.ToString()))
                            {
                                Log("Activating [" + repairer.TypeName + "] ItemID [" + repairer.ItemId + "] S%  [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "] A%  [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "] Cap% [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 2) + "]");
                                if (repairer.Click())
                                {
                                    return true;
                                }

                                continue;
                            }
                        }
                        else
                        {
                            if (DebugConfig.DebugDefense) Log("[" + repairer.TypeName + "] IsInLimboState [" + repairer.IsInLimboState + "] IsOnline [" + repairer.IsOnline + "] IsActivatable [" + repairer.IsActivatable + "] IsDeactivating [" + repairer.IsDeactivating + "] IsReloadingAmmo [" + repairer.IsReloadingAmmo + "] IsBeingRepaired [" + repairer.IsBeingRepaired + "] EffectActivating [" + repairer.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(repairer) + "] ReactivationDelay [" + repairer.ReactivationDelay + " > 0]");
                        }
                    }
                }
            }

            return true;
        }

        internal bool ManageArmorRepairers()
        {
            // armor repair modules
            if (armorRepairModules.Any())
            {
                foreach (var repairer in armorRepairModules.OrderBy(x => x.ItemId))
                {
                    if (repairer.IsActive)
                    {
                        if (DebugConfig.DebugDefense) Log("if (repairer.IsActive)");
                        //2nd shield booster (or armor rep: crazy fits?)
                        if (armorRepairModules.Count() >= 2)
                        {
                            if (DebugConfig.DebugDefense) Log("if (repairModules >= 2)");
                            if (armorRepairModules.All(i => i.IsArmorRepairModule) && armorRepairModules.Count(e => e.IsActive) >= 2)
                            {
                                if (DebugConfig.DebugDefense) Log("if (repairModules && e.IsActive) >= 2)");
                                if (boolShouldWeDeactivateSecondaryArmorRep)
                                {
                                    if (DebugConfig.DebugDefense) Log("boolShouldWeDeactivateSecondRep [" + boolShouldWeDeactivateSecondaryArmorRep + "]");
                                    if (!repairer.IsInLimboState)
                                    {
                                        Log($"Disabling [{repairer.TypeName}] boolShouldWeDeactivateSecondRep [" + boolShouldWeDeactivateSecondaryArmorRep + "] C% [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "] S% [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) + "] A% [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 0) + "]");
                                        if (DirectEve.Interval(800, 1500, repairer.ItemId.ToString()))
                                        {
                                            if (repairer.Click())
                                            {
                                                return true;
                                            }

                                            continue;
                                        }
                                    }
                                    else
                                    {
                                        if (DebugConfig.DebugDefense) Log("[" + repairer.TypeName + "] IsInLimboState [" + repairer.IsInLimboState + "] IsOnline [" + repairer.IsOnline + "] IsActivatable [" + repairer.IsActivatable + "] IsDeactivating [" + repairer.IsDeactivating + "] IsReloadingAmmo [" + repairer.IsReloadingAmmo + "] IsBeingRepaired [" + repairer.IsBeingRepaired + "] EffectActivating [" + repairer.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(repairer) + "] ReactivationDelay [" + repairer.ReactivationDelay + " > 0]");
                                    }
                                }
                            }
                        }

                        //1st armor rep
                        if (boolShouldWeDeactivateArmorRep)
                        {
                            if (DebugConfig.DebugDefense) Log("boolShouldWeDeactivateRep [" + boolShouldWeDeactivateArmorRep + "]");
                            if (!repairer.IsInLimboState)
                            {
                                Log($"Disabling [{repairer.TypeName}] boolShouldWeDeactivateRep [" + boolShouldWeDeactivateArmorRep + "] C% [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 0) + "] S% [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 0) + "]");
                                if (DirectEve.Interval(800, 1500, repairer.ItemId.ToString()))
                                {
                                    if (repairer.Click())
                                    {
                                        return true;
                                    }

                                    continue;
                                }
                            }
                            else
                            {
                                if (DebugConfig.DebugDefense) Log("[" + repairer.TypeName + "] IsInLimboState [" + repairer.IsInLimboState + "] IsOnline [" + repairer.IsOnline + "] IsActivatable [" + repairer.IsActivatable + "] IsDeactivating [" + repairer.IsDeactivating + "] IsReloadingAmmo [" + repairer.IsReloadingAmmo + "] IsBeingRepaired [" + repairer.IsBeingRepaired + "] EffectActivating [" + repairer.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(repairer) + "] ReactivationDelay [" + repairer.ReactivationDelay + " > 0]");
                            }
                        }
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) || _anyOverheat || !IsOurShipWithintheAbyssBounds())
                    {
                        if (repairer.IsBeingRepaired)
                        {
                            // cancel repair
                            if (repairer.CancelRepair())
                            {
                                Log($"Canceling repair on [" + repairer.TypeName + "] due to: anyTargets [" + Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) + "] _anyOverheat [" + _anyOverheat + "] !IsOurShipWithintheAbyssBounds [" + IsOurShipWithintheAbyssBounds() + "]");
                                return true;
                            }
                        }
                    }

                    if (DirectEve.Me.IsInAbyssalSpace() && !Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure) && IsOurShipWithintheAbyssBounds())
                    {
                        if (!repairer.IsBeingRepaired && repairer.HeatDamagePercent > 1)
                        {
                            var shipsCargo = ESCache.Instance.DirectEve.GetShipsCargo();

                            if (shipsCargo != null)
                            {
                                if (!shipsCargo.Items.Any(i => i.TypeId == _naniteRepairPasteTypeId))
                                {
                                    if (!ShouldWeRepair)
                                        continue;

                                    // repair
                                    if (repairer.Repair())
                                    {
                                        Log($"Repairing TypeName[{repairer.TypeName}].");
                                        return true;
                                    }
                                }

                                if (!shipsCargo.Items.Any(i => i.TypeId == _naniteRepairPasteTypeId))
                                {
                                    DirectEve.IntervalLog(60000, 60000, "No nanite repair paste found in cargo, can't repair.");
                                    continue;
                                }
                            }
                        }
                    }

                    if (repairer.IsActive)
                        continue;

                    if (repairer.IsInLimboState)
                    {
                        if (DebugConfig.DebugDefense) Log("[" + repairer.TypeName + "] IsInLimboState [" + repairer.IsInLimboState + "] IsOnline [" + repairer.IsOnline + "] IsActivatable [" + repairer.IsActivatable + "] IsDeactivating [" + repairer.IsDeactivating + "] IsReloadingAmmo [" + repairer.IsReloadingAmmo + "] IsBeingRepaired [" + repairer.IsBeingRepaired + "] EffectActivating [" + repairer.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(repairer) + "] ReactivationDelay [" + repairer.ReactivationDelay + " > 0]");
                        continue;
                    }

                    if (armorRepairModules.Count() >= 2)
                    {
                        //This repairer isnt active, but we have one that is...
                        if (!repairer.IsActive && armorRepairModules.Any(e => e.IsActive))
                        {
                            if (DebugConfig.DebugDefense) Log("if (!repairer.IsActive && DirectEve.Modules.Any(e => e.IsActive))");
                            if (boolShouldWeActivateSecondaryArmorRep)
                            {
                                if (DebugConfig.DebugDefense) Log("boolShouldWeActivateSecondShieldBooster [" + boolShouldWeActivateSecondaryArmorRep + "]");
                                if (!repairer.IsInLimboState)
                                {
                                    Log("Activating [" + repairer.TypeName + "] 2nd repairer - ItemID [" + repairer.ItemId + "] S%  [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "] A%  [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "] Cap% [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 2) + "]");
                                    if (repairer.Click())
                                    {
                                        return true;
                                    }

                                    continue;
                                }
                                else
                                {
                                    if (DebugConfig.DebugDefense) Log("[" + repairer.TypeName + "] IsInLimboState [" + repairer.IsInLimboState + "] IsOnline [" + repairer.IsOnline + "] IsActivatable [" + repairer.IsActivatable + "] IsDeactivating [" + repairer.IsDeactivating + "] IsReloadingAmmo [" + repairer.IsReloadingAmmo + "] IsBeingRepaired [" + repairer.IsBeingRepaired + "] EffectActivating [" + repairer.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(repairer) + "] ReactivationDelay [" + repairer.ReactivationDelay + " > 0]");
                                }
                            }

                            continue;
                        }
                    }

                    if (boolShouldWeActivateArmorRep)
                    {
                        if (!repairer.IsInLimboState)
                        {
                            if (DirectEve.Interval(800, 1500, repairer.ItemId.ToString()))
                            {
                                Log("Activating [" + repairer.TypeName + "] ItemID [" + repairer.ItemId + "] S%  [" + Math.Round(ESCache.Instance.ActiveShip.ShieldPercentage, 2) + "] A%  [" + Math.Round(ESCache.Instance.ActiveShip.ArmorPercentage, 2) + "] Cap% [" + Math.Round(ESCache.Instance.ActiveShip.CapacitorPercentage, 2) + "]");
                                if (repairer.Click())
                                {
                                    return true;
                                }

                                continue;
                            }
                        }
                        else
                        {
                            if (DebugConfig.DebugDefense) Log("[" + repairer.TypeName + "] IsInLimboState [" + repairer.IsInLimboState + "] IsOnline [" + repairer.IsOnline + "] IsActivatable [" + repairer.IsActivatable + "] IsDeactivating [" + repairer.IsDeactivating + "] IsReloadingAmmo [" + repairer.IsReloadingAmmo + "] IsBeingRepaired [" + repairer.IsBeingRepaired + "] EffectActivating [" + repairer.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(repairer) + "] ReactivationDelay [" + repairer.ReactivationDelay + " > 0]");
                        }
                    }
                }
            }

            return true;
        }

        internal bool boolShouldWeActivateAssaultDamageControl
        {
            get
            {
                //
                // this assumes shield tank!
                //
                var capPerc = DirectEve.ActiveShip.CapacitorPercentage;
                var currentShieldPct = DirectEve.ActiveShip.ShieldPercentage;
                var currentArmorPct = DirectEve.ActiveShip.ArmorPercentage;
                var currentStructurePct = DirectEve.ActiveShip.StructurePercentage;

                var armorOrStrucDecreased = currentArmorPct < _lastArmorPerc || currentStructurePct < _lastStructurePerc;

                _lastArmorPerc = currentArmorPct;
                _lastStructurePerc = currentStructurePct;

                if (ESCache.Instance.ActiveShip.IsAssaultShip)
                {
                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                        return false;

                    if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                    {
                        if (37 > DirectEve.ActiveShip.ShieldPercentage)
                        {
                            Log("S [" + Math.Round(DirectEve.ActiveShip.ShieldPercentage, 0) + "] is less than 37: return true");
                            return true;
                        }

                        return false;
                    }

                    if (52 > DirectEve.ActiveShip.ShieldPercentage)
                    {
                        Log("S [" + Math.Round(DirectEve.ActiveShip.ShieldPercentage, 0) + "] is less than 52: return true");
                        return true;
                    }

                    return false;
                }

                if (currentShieldPct < 30 && currentArmorPct < 20)
                {
                    Log("if (currentShieldPct [" + currentShieldPct + "] < 30 && currentArmorPct [" + currentArmorPct + " ] < 20)");
                    return true;
                }

                if (currentShieldPct < 30 && currentArmorPct < 99 && armorOrStrucDecreased)
                {
                    Log("if (currentShieldPct [" + currentShieldPct + "] < 30 && currentArmorPct [" + currentArmorPct + " ] < 99 && armorOrStrucDecreased [" + armorOrStrucDecreased + "])");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                {
                    if (boolShouldWeBeSpeedTanking)
                    {
                        if (Combat.PotentialCombatTargets.Count(i => i.Name.Contains("Lucifer Cynabal") && 40000 > i.Distance) >= 2)
                        {
                            if (currentShieldPct < 40)
                            {
                                Log("LuciferSpawn: if (currentShieldPct [" + currentShieldPct + "] < 40");
                                return true;
                            }
                        }
                    }
                }

                if (ESCache.Instance.Entities.Any(e => e.IsPlayer && e.IsAttacking))
                {
                    Log("We are being attacked by players! Count [" + ESCache.Instance.Entities.Count(e => e.IsPlayer && e.IsAttacking) + "]");
                    foreach (var playerEntity in ESCache.Instance.Entities.Where(e => e.IsPlayer && e.IsAttacking))
                    {
                        Log("Attacker [" + playerEntity.Name + "][" + playerEntity.TypeName + "][" + playerEntity.Nearest1KDistance + "] Velocity [" + Math.Round(playerEntity.Velocity, 0) + "] EWar [" + playerEntity.stringEwarTypes + "]");
                    }

                    return true;
                }

                return false;
            }
        }


        internal bool ManageAssaultDamageControl()
        {
            if (!ESCache.Instance.Modules.Any())
                return true;

            var anyTargets = Combat.PotentialCombatTargets.Any(i => i.BracketType != BracketType.Large_Collidable_Structure);

            var capPerc = DirectEve.ActiveShip.CapacitorPercentage;
            var currentShieldPct = DirectEve.ActiveShip.ShieldPercentage;
            var currentArmorPct = DirectEve.ActiveShip.ArmorPercentage;
            var currentStructurePct = DirectEve.ActiveShip.StructurePercentage;

            // Check if we received armor or structure damage since last capture
            var armorOrStrucDecreased = currentArmorPct < _lastArmorPerc || currentStructurePct < _lastStructurePerc;

            _lastArmorPerc = currentArmorPct;
            _lastStructurePerc = currentStructurePct;

            if (ESCache.Instance.Modules.Any(e => e.GroupId == (int)Group.AssaultDamageControl && e.IsOnline && e._module.EffectId == 7012 && e.IsActivatable)) // assault damage control effect
            {

                if (boolShouldWeActivateAssaultDamageControl)
                {
                    var module = ESCache.Instance.Modules.FirstOrDefault(e => e.GroupId == (int)Group.AssaultDamageControl && e._module.EffectId == 7012 && e.IsActivatable);

                    if (module.IsInLimboState)
                        return false;

                    if (module.IsDeactivating)
                        return false;

                    if (module.IsActive)
                        return false;

                    if (DirectEve.Interval(5000, 6000, module.ItemId.ToString()))
                    {
                        Log($"Activating assault damage control.");
                        module.Click();
                        return true;
                    }
                }
            }

            return false;
        }

        internal bool ManageModules()
        {
            if (DebugConfig.DebugAbyssalDeadspaceBehavior) Log("ManageModules");

            if (DirectEve.Session.IsInDockableLocation)
                return false;

            if (DirectEve.Session.InJump)
                return false;

            if (DirectEve.Me.IsJumpCloakActive)
                return false;

            if (ESCache.Instance.DirectEve.Me.IsInvuln)
            {
                if (DebugConfig.DebugDefense && DirectEve.Interval(2000))
                    Log($"Not managing modules during invul phases.");
                return false;
            }

            if (!ESCache.Instance.Modules.Any())
                return true;

            if (!DirectEve.Me.IsInAbyssalSpace() && ESCache.Instance.InSpace && AbyssalNeedRepair() && !IsAnyOtherNonFleetPlayerOnGrid)
            {
                DirectBookmark fbmx = null;

                if (ListOfFilamentBookmarks.Any())
                {
                    if (ESCache.Instance.EveAccount.UseFleetMgr)
                        fbmx = ListOfFilamentBookmarks.FirstOrDefault();
                    else
                        fbmx = ListOfFilamentBookmarks.OrderBy(i => Guid.NewGuid()).FirstOrDefault();
                }

                if (fbmx == null)
                {
                    Log("Missing bookmark for filament spot: no bookmark with [" + _filamentSpotBookmarkName.ToLower() + "] found in local");
                    return false;
                }

                if (ESCache.Instance.DirectEve.ActiveShip != null && fbmx.DistanceTo(ESCache.Instance.DirectEve.ActiveShip.Entity) <= 15000)
                {
                    // we don't want to manage modules on the abyss spot if we need repair and no other player is near
                    return false;
                }
            }

            ManageCapInjectors();

            ManageAssaultDamageControl();

            ManageHardeners();

            ManageRepairers();

            return false;
        }

        internal bool ShouldWeDeactivateWeapons
        {
            get
            {
                try
                {
                    if (!ESCache.Instance.Weapons.Any())
                        return false;

                    if (!ESCache.Instance.Targets.Any(x => x.GroupId != (int)Group.AssaultShip))
                        return false;

                    var currentTarget = Combat.PotentialCombatTargets.FirstOrDefault(t => t.Id == ESCache.Instance.Weapons.FirstOrDefault(i => i.IsActive).TargetId);

                    if (currentTarget == null) //gun should deactivate itself in this case?
                    {
                        if (DebugConfig.DebugActivateWeapons) Log("if (currentTarget == null) //gun should deactivate itself in this case?");
                        return false;
                    }

                    if (currentTarget.Distance > Combat.MaxWeaponRange)
                    {
                        Log("currentTarget.Distance [" + Math.Round(currentTarget.Distance, 0) + "] > Combat.MaxWeaponRange [" + Combat.MaxWeaponRange + "]");
                        return true;
                    }

                    if (ESCache.Instance.Targets.Any(x => x.GroupId != (int)Group.AssaultShip) && ESCache.Instance.Targets.Any(i => i.GroupId != (int)Group.AssaultShip && i.Id != currentTarget.Id && i._directEntity.AbyssalTargetPriority < currentTarget._directEntity.AbyssalTargetPriority))
                    {
                        var higherPriorityTarget = ESCache.Instance.Targets.FirstOrDefault(i => i.GroupId != (int)Group.AssaultShip && i.Id != currentTarget.Id && i._directEntity.AbyssalTargetPriority < currentTarget._directEntity.AbyssalTargetPriority && i.IsReadyToShoot);
                        if (higherPriorityTarget != null)
                        {
                            Log("higherPriorityTarget [" + higherPriorityTarget.TypeName + "] @ [" + higherPriorityTarget.Nearest1KDistance + "] AbyssalTargetPriority [" + higherPriorityTarget._directEntity.AbyssalTargetPriority + "]");
                            return true;
                        }
                    }

                    if (!Drones.UseDrones && Combat.PotentialCombatTargets.Any(i => i.IsTarget && currentTarget != null && currentTarget.Id != i.Id &&
                            currentTarget._directEntity.AbyssalTargetPriority == i._directEntity.AbyssalTargetPriority &&
                            i.ShieldPct != 1 &&
                            currentTarget.ShieldPct >= i.ShieldPct && currentTarget.ArmorPct >= i.ArmorPct &&
                            i.IsReadyToShoot))
                    {
                        if (ESCache.Instance.ActiveShip.TypeId != (int)TypeID.StormBringer)
                        {
                            Log("currentTarget [" + currentTarget.TypeName + "] @ [" + currentTarget.Nearest1KDistance + "] but another target has more damage: deactivate weapons");
                            return true;
                        }
                    }

                    if (!allDronesInSpace.Any())
                        return false;

                    var currentDroneTargets = allDronesInSpace.Where(e => e._directEntity.FollowEntity != null).Select(e => e._directEntity.FollowEntity)
                    .Where(e => e.Distance < Combat.MaxWeaponRange && e.IsTarget && !e.IsAbyssalDeadspaceTriglavianExtractionNode && !e.IsWreck).OrderBy(e => e.Distance)
                    .ToList();

                    if (currentDroneTargets.Any(e => e.Distance <= Combat.MaxWeaponRange) && // if we have any drone targets on grid below weapon range
                       !currentDroneTargets.Any(e => e.Id == currentTarget.Id)) // if the current target is not a drone target
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

        private static int _weaponNumber = 0;

        private static bool ShouldWeActivateOverloadOnWeapon(ModuleCache moduleToOverload, int moduleDamagePercentageAllowed)
        {
            //if highRackHeatStatus > HighRackHeatDisableThreshold: wait
            if (ESCache.Instance.ActiveShip.HighHeatRackState() > HighRackHeatDisableThreshold)
            {
                if (DirectEve.Interval(5000)) Log("if (highRackHeatStatus [" + ESCache.Instance.ActiveShip.HighHeatRackState() + "] > HighRackHeatDisableThreshold [" + HighRackHeatDisableThreshold + "])");
                return false;
            }

            //if (DebugConfig.DebugOverLoadWeapons) Log("OverloadModules: 9");

            if (ESCache.Instance.Modules.Where(x => x.IsHighSlotModule && !x.IsWeapon).Any(i => i.DamagePercent > moduleDamagePercentageAllowed))
            {
                if (DirectEve.Interval(5000)) Log("if (ESCache.Instance.Modules.Where(x => x.IsHighSlotModule).Any(i => i.DamagePercent > moduleDamagePercentageAllowed [" + moduleDamagePercentageAllowed + "]))");
                return false;
            }

            //if (DebugConfig.DebugOverLoadWeapons) Log("OverloadModules: 10");

            //If overload is not active, only activate it if the rack heat is below the highRackHeatEnableThreshold
            if (!moduleToOverload.IsOverloaded && ESCache.Instance.ActiveShip.HighHeatRackState() > HighRackHeatDisableThreshold)
            {
                if (DirectEve.Interval(5000)) Log("if (!moduleToOverload.IsOverloaded && highRackHeatStatus [" + ESCache.Instance.ActiveShip.HighHeatRackState() + "] > HighRackHeatDisableThreshold [" + HighRackHeatDisableThreshold + "])");
                return false;
            }

            //if (DebugConfig.DebugOverLoadWeapons) Log("OverloadModules: 11");

            //
            // do not overload if we are outside range
            //
            //if (myTarget != null && targetDistanceToUnOverload != 99999 && targetDistanceToUnOverload > myTarget.Distance)
            //{
            //    if (DirectEve.Interval(5000)) Log("[" + myTarget.TypeName + "] targetDistanceToUnOverload [" + targetDistanceToUnOverload + "] > myTarget.Distance [" + myTarget.Distance + "])");
            //    continue;
            //}

            //if (DebugConfig.DebugOverLoadWeapons) Log("OverloadModules: 12");

            if (moduleToOverload.DamagePercent >= moduleDamagePercentageAllowed)
            {
                if (DirectEve.Interval(5000)) Log("if (moduleToOverload.DamagePercent [" + moduleToOverload.DamagePercent + "] >= moduleDamagePercentageAllowed [" + moduleDamagePercentageAllowed + "])");
                return false;
            }

            if (ESCache.Instance.Modules.Any(i => i._module.IsBeingRepaired))
            {
                if (DirectEve.Interval(5000)) Log("if (ESCache.Instance.Modules.Any(i => i._module.IsBeingRepaired))");
                return false;
            }

            return true;
        }

        private static bool ShouldWeDeactivateOverloadOnWeapon(ModuleCache overloadedModule, int moduleDamagePercentageAllowed)
        {
            if (overloadedModule.DamagePercent > moduleDamagePercentageAllowed) //This module is damaged "too much"
            {
                if (DirectEve.Interval(10000)) Log("overloadedModule [" + overloadedModule.TypeName + "] overloadedModule.DamagePercent [" + overloadedModule.DamagePercent + "] > moduleDamagePercentageAllowed [" + moduleDamagePercentageAllowed + "]");
                return true;
            }

            if (ESCache.Instance.ActiveShip.HighHeatRackState() > HighRackHeatDisableThreshold) //the Rack is not yet damaged "too much"
            {
                if (DirectEve.Interval(10000)) Log("HighRackHeatDisableThreshold [" + HighRackHeatDisableThreshold + "] > highRackHeatStatus [" + ESCache.Instance.ActiveShip.HighHeatRackState() + "]");
                //no need to try other modules: just return
                return true;
            }

            if (ESCache.Instance.Modules.Where(x => x.IsHighSlotModule && !x.IsWeapon).Any(i => i.DamagePercent > moduleDamagePercentageAllowed)) //other highslot module is damaged "too much"
            {
                if (DirectEve.Interval(10000)) Log("if (ESCache.Instance.Modules.Where(x => x.IsHighSlotModule).All(i => moduleDamagePercentageAllowed > i.DamagePercent))");
                //no need to try other modules: just return
                return true;
            }

            return false;
        }

        public static void OverloadModules(bool allowOverLoadOfTheseModules, List<ModuleCache> modulesToTryToOverload, int moduleDamagePercentageAllowed, int targetDistanceToUnOverload = 99999, EntityCache myTarget = null)
        {
            try
            {
                //if (DebugConfig.DebugOverLoadWeapons) Log("OverloadModules: 1");

                var highRackHeatStatus = ESCache.Instance.ActiveShip.HighHeatRackState(); // medium rack heat state
                //
                // DeActivate Overload as needed
                //
                _weaponNumber = 0;
                foreach (ModuleCache overloadedModule in modulesToTryToOverload.Where(i => i.IsOverloaded && !i.IsPendingStopOverloading && !i.IsPendingOverloading).OrderByDescending(i => new Guid()))
                {
                    if (Time.Instance.LastActivatedTimeStamp != null && Time.Instance.LastActivatedTimeStamp.ContainsKey(overloadedModule.ItemId))
                    {
                        if (Time.Instance.LastActivatedTimeStamp[overloadedModule.ItemId].AddMilliseconds(2000) > DateTime.UtcNow)
                        {
                            if (DirectEve.Interval(10000) && DebugConfig.DebugOverLoadWeapons) Log("if (Time.Instance.LastOverLoadedTimeStamp[moduleToOverload.ItemId].AddMilliseconds(ESCache.Instance.RandomNumber(1500, 2000)) > DateTime.UtcNow)");
                            continue;
                        }
                    }

                    _weaponNumber++;

                    //if (DebugConfig.DebugOverLoadWeapons) Log("OverloadModules: 3");

                    if (!ShouldWeDeactivateOverloadOnWeapon(overloadedModule, moduleDamagePercentageAllowed))
                        continue;

                    //if (DebugConfig.DebugOverLoadWeapons) Log("OverloadModules: 4");

                    try
                    {
                        if (DirectEve.Interval(2000) && overloadedModule.ToggleOverload())
                        {
                            Log("OverLoadModules: deactivate overload: [" + overloadedModule.TypeName + "][" + _weaponNumber + "] has [" + Math.Round(overloadedModule.DamagePercent, 1) + "%] damage: allowing [" + moduleDamagePercentageAllowed + "]% damage deactivating overload");
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("Exception [" + ex + "]");
                    }

                    continue;

                    /**
                    if (myTarget != null && myTarget.Distance != 0 && targetDistanceToUnOverload > myTarget.Distance)
                    {
                        try
                        {
                            if (overloadedModule.ToggleOverload())
                            {
                                Log("OverLoadModules: deactivate overload: [" + overloadedModule.TypeName + "][" + _weaponNumber + "] targetDistanceToUnOverload [" + Math.Round((double)targetDistanceToUnOverload / 1000, 0) + "k] > [" + Math.Round(myTarget.Distance / 1000, 0) + "]k from [" + myTarget.Name + "]: deactivating overload");
                                continue;
                            }
                        }
                        catch (Exception ex)
                        {
                            Log("Exception [" + ex + "]");
                        }

                        return;
                    }
                    **/
                }

                //if (DebugConfig.DebugOverLoadWeapons) Log("OverloadModules: 5");

                if (!allowOverLoadOfTheseModules)
                {
                    if (DebugConfig.DebugOverLoadWeapons) Log("if (!allowOverLoadOfTheseModules)");
                    return;
                }
                //
                // Activate Overload as needed
                //

                //if (DebugConfig.DebugOverLoadWeapons) Log("OverloadModules: 6");
                _weaponNumber = 0;
                //foreach (var thisWeapon in ESCache.Instance.Weapons.Where(i => !i._module.IsBeingRepaired && i._module.HeatDamagePercent > 0 && !i.IsActive).OrderByDescending(i => i._module.IsMaster).DistinctBy(x => x.TypeId).OrderByDescending(i => new Guid()))
                foreach (ModuleCache moduleToOverload in modulesToTryToOverload.Where(i => !i.IsOverloaded && !i.IsPendingStopOverloading && !i.IsPendingOverloading && !i._module.IsBeingRepaired).OrderByDescending(i => i._module.IsMaster).DistinctBy(x => x.TypeId).OrderByDescending(i => new Guid()))
                {
                    if (Time.Instance.LastOverLoadedTimeStamp != null && Time.Instance.LastOverLoadedTimeStamp.ContainsKey(moduleToOverload.ItemId))
                        if (Time.Instance.LastOverLoadedTimeStamp[moduleToOverload.ItemId].AddMilliseconds(ESCache.Instance.RandomNumber(1500, 2000)) > DateTime.UtcNow)
                        {
                            if (DirectEve.Interval(10000) && DebugConfig.DebugOverLoadWeapons) Log("if (Time.Instance.LastOverLoadedTimeStamp[moduleToOverload.ItemId].AddMilliseconds(ESCache.Instance.RandomNumber(1500, 2000)) > DateTime.UtcNow)!!");
                            continue;
                        }

                    _weaponNumber++;
                    //if (DebugConfig.DebugOverLoadWeapons) Log("OverloadModules: 7");

                    //
                    //
                    //
                    if (moduleToOverload.IsActive) //and not already overloading
                    {
                        //
                        // only overload these modules if they arent active (these modules give range when overloaded)
                        //
                        if (moduleToOverload.GroupId == (int)Group.StasisWeb)
                        {
                            if (DirectEve.Interval(5000) && DebugConfig.DebugOverLoadWeapons) Log("if (moduleToOverload.GroupId == (int)Group.StasisWeb)");
                            continue;
                        }
                    }

                    //if (DebugConfig.DebugOverLoadWeapons) Log("OverloadModules: 8");

                    if (!ShouldWeActivateOverloadOnWeapon(moduleToOverload, moduleDamagePercentageAllowed))
                        continue;

                    //if (DebugConfig.DebugOverLoadWeapons) Log("OverloadModules: 13");

                    try
                    {
                        if (DirectEve.Interval(2000) && moduleToOverload.ToggleOverload())
                        {
                            if (Time.Instance.LastOverLoadedTimeStamp != null)
                                Time.Instance.LastOverLoadedTimeStamp.AddOrUpdate(moduleToOverload.ItemId, DateTime.UtcNow);
                            Log("OverLoadModules: activate overload: [" + moduleToOverload.TypeName + "][" + _weaponNumber + "]: activating overload");
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("Exception [" + ex + "]");
                    }

                    continue;
                }

                //if (DebugConfig.DebugOverLoadWeapons) Log("OverloadModules: 14");
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        public static bool boolOverLoadWeapons
        {
            get
            {
                if (!AllowOverloadOfWeapons)
                    return false;

                if (ESCache.Instance.Modules.Where(i => i.IsHighSlotModule).Any(e => e._module.HeatDamagePercent >= Combat.WeaponOverloadDamageAllowed))
                {
                    return false;
                }

                //Stormbringer can overheat all the time
                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                    return true;

                if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Gila)
                    return true;

                if (ESCache.Instance.EveAccount.UseFleetMgr)
                {
                    if (ESCache.Instance.ActiveShip.IsAssaultShip)
                    {
                        if (DirectEve.Interval(60000)) Log("UseFleetMgr: IsAssaultShip: return true");
                        return true;
                    }

                    if (ESCache.Instance.ActiveShip.IsFrigOrDestroyerWithDroneBonuses)
                    {
                        if (DirectEve.Interval(60000)) Log("UseFleetMgr: IsFrigOrDestroyerWithDroneBonuses: return true");
                        return true;
                    }
                }

                if (ESCache.Instance.EveAccount.UseFleetMgr && AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.Name == "Lucifer Cynabal"))
                        return true;

                    if (Combat.PotentialCombatTargets.Any(i => i.Name == "Elite Lucifer Cynabal"))
                        return true;


                    if (DirectEve.Interval(60000)) Log("LuciferSpawn: UseFleetMgr: no Cynabals: return false");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LuciferSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.Name == "Lucifer Cynabal" && i.IsReadyToShoot))
                        return true;

                    if (Combat.PotentialCombatTargets.Any(i => i.Name == "Elite Lucifer Cynabal" && i.IsReadyToShoot))
                        return true;

                    if (DirectEve.Interval(60000)) Log("LuciferSpawn: no Cynabals: return false");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count() > 12)
                        return true;

                    int lanceOverloadThreshold = 8;
                    if (ESCache.Instance.ActiveShip.IsAssaultShip || ESCache.Instance.ActiveShip.IsFrigate)
                        lanceOverloadThreshold = 3;

                    if (Combat.PotentialCombatTargets.Where(i => i.Name.ToLower().Contains("lance".ToLower())).Count() >= lanceOverloadThreshold)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                            return true;
                    }

                    if (DirectEve.Interval(60000)) Log("DroneFrigateSpawn: lees than [" + lanceOverloadThreshold + "] frigs: return false");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DamavikFrigateSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count() > 12)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                            return true;
                    }

                    if (DirectEve.Interval(60000)) Log("DamavikFrigateSpawn: lees than [ 12 ] frigs: return false");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.All(i => i.IsNPCCruiser))
                        return true;

                    if (Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                        return true;

                    if (DirectEve.Interval(15000)) Log("EphialtesCruiserSpawn: nothing ready to shoot: return false");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.ConcordSpawn)
                {
                    return true;
                }


                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn)
                {
                    if (ESCache.Instance.Targets.All(i => i.IsNPCCruiser))
                        return true;

                    if (Combat.CurrentWeaponTarget() != null && Combat.CurrentWeaponTarget().IsNPCCruiser)
                        return true;

                    if (DirectEve.Interval(15000)) Log("ConcordSpawn: no Cruisers ready to shoot: return false");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn)
                {
                    if (Combat.PotentialCombatTargets.Count() > 12)
                    {
                        if (Combat.PotentialCombatTargets.Any(i => i.IsReadyToShoot))
                            return true;
                    }

                    if (DirectEve.Interval(15000)) Log("LucidFrigateSpawn: less than 12 frigates: return false");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DrekavacBattleCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                    {
                        if (ESCache.Instance.Targets.All(i => i.IsNPCBattlecruiser))
                            return true;

                        if (Combat.CurrentWeaponTarget() != null)
                        {
                            if (Combat.CurrentWeaponTarget().IsNPCBattlecruiser)
                                return true;
                        }

                        if (DirectEve.Interval(15000)) Log("DrekavacBattleCruiserSpawn: no BCs ready to shoot: return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
                    {
                        if (ESCache.Instance.Targets.All(i => i.IsNPCDestroyer))
                            return true;

                        if (Combat.CurrentWeaponTarget() != null)
                        {
                            if (Combat.CurrentWeaponTarget().IsNPCDestroyer)
                                return true;
                        }

                        if (DirectEve.Interval(15000)) Log("DrekavacBattleCruiserSpawn: no Destroyers ready to shoot: return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsWebbingMe) >= 2)
                    {
                        if (Combat.CurrentWeaponTarget() != null)
                        {
                            if (Combat.CurrentWeaponTarget().IsWebbingMe)
                                return true;
                        }

                        if (DirectEve.Interval(15000)) Log("DrekavacBattleCruiserSpawn: being webbed but not yet shooting webs: return false");
                        return false;
                    }

                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KikimoraDestroyerSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
                    {
                        if (ESCache.Instance.Targets.All(i => i.IsNPCDestroyer))
                            return true;

                        if (Combat.CurrentWeaponTarget() != null)
                        {
                            if (Combat.CurrentWeaponTarget().IsNPCDestroyer)
                                return true;
                        }

                        if (DirectEve.Interval(15000)) Log("KikimoraDestroyerSpawn: no Destroyers ready to shoot: return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsWebbingMe) >= 2)
                    {
                        if (Combat.CurrentWeaponTarget() != null)
                        {
                            if (Combat.CurrentWeaponTarget().IsWebbingMe)
                                return true;
                        }

                        if (DirectEve.Interval(15000)) Log("KikimoraDestroyerSpawn: being webbed but not yet shooting webs: return false");
                        return false;
                    }

                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.VedmakCruiserSpawn)
                {
                    if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.StormBringer)
                    {
                        return true;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCDestroyer))
                    {
                        if (ESCache.Instance.Targets.All(i => i.IsNPCDestroyer))
                            return true;

                        if (Combat.CurrentWeaponTarget() != null)
                        {
                            if (Combat.CurrentWeaponTarget().IsNPCDestroyer)
                                return true;
                        }

                        if (DirectEve.Interval(15000)) Log("VedmakCruiserSpawn: no Destroyers ready to shoot: return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCCruiser))
                    {
                        if (ESCache.Instance.Targets.All(i => i.IsNPCCruiser))
                            return true;

                        if (Combat.CurrentWeaponTarget() != null)
                        {
                            if (Combat.CurrentWeaponTarget().IsNPCCruiser)
                                return true;
                        }

                        if (DirectEve.Interval(15000)) Log("VedmakCruiserSpawn: no Cruisers ready to shoot: return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsWebbingMe) >= 2)
                    {
                        if (Combat.CurrentWeaponTarget() != null)
                        {
                            if (Combat.CurrentWeaponTarget().IsWebbingMe)
                                return true;
                        }

                        if (DirectEve.Interval(15000)) Log("VedmakCruiserSpawn: being webbed but not yet shooting webs: return false");
                        return false;
                    }

                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.HighAngleDroneBattleCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattlecruiser))
                    {
                        if (ESCache.Instance.Targets.All(i => i.IsNPCBattlecruiser))
                            return true;

                        if (Combat.CurrentWeaponTarget() != null)
                        {
                            if (Combat.CurrentWeaponTarget().IsNPCBattlecruiser)
                                return true;
                        }

                        if (DirectEve.Interval(15000)) Log("HighAngleDroneBattleCruiserSpawn: no BCs ready to shoot: return false");
                        return false;
                    }

                    if (Combat.PotentialCombatTargets.Count(i => i.IsWebbingMe) >= 2)
                    {
                        if (Combat.CurrentWeaponTarget() != null)
                        {
                            if (Combat.CurrentWeaponTarget().IsWebbingMe)
                                return true;
                        }

                        if (DirectEve.Interval(15000)) Log("HighAngleDroneBattleCruiserSpawn: being webbed but not yet shooting webs: return false");
                        return false;
                    }

                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                {
                    if (DirectEve.Interval(15000)) Log("KarybdisTyrannosSpawn: return true");
                    return true;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidDeepwatcherBSSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                    {
                        if (ESCache.Instance.Targets.All(i => i.IsNPCBattleship))
                            return true;

                        if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship && i.IsReadyToShoot && i.IsCurrentTarget))
                            return true;
                    }

                    if (DirectEve.Interval(15000)) Log("LucidDeepwatcherBSSpawn: no BSs ready to shoot: return false");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.IsNPCBattleship))
                    {
                        if (ESCache.Instance.Targets.All(i => i.IsNPCBattleship))
                            return true;

                        if (Combat.CurrentWeaponTarget() != null && Combat.CurrentWeaponTarget().IsNPCBattleship)
                            return true;
                    }

                    if (DirectEve.Interval(15000)) Log("LucidDeepwatcherBSSpawn: no BSs ready to shoot: return false");
                    return false;
                }

                //should we add other spawns here?
                return false;
            }
        }

        public static void HandleOverloadWeapons()
        {
            try
            {
                if (ESCache.Instance.Weapons.Count == 0) return;

                OverloadModules(boolOverLoadWeapons, ESCache.Instance.Weapons, Combat.WeaponOverloadDamageAllowed);
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        public bool FocusFire
        {
            get
            {
                if (!ESCache.Instance.EveAccount.UseFleetMgr)
                {
                    if (DebugConfig.DebugFocusFire) Log("UseFleetMgr [false] return true");
                    return true;
                }

                if (ESCache.Instance.Targets.Count == 0)
                {
                    if (DebugConfig.DebugFocusFire) Log("if (ESCache.Instance.Targets.Count == 0) return true");
                    return true;
                }

                if (ESCache.Instance.Targets.Count == 1)
                {
                    if (DebugConfig.DebugFocusFire) Log("if (ESCache.Instance.Targets.Count == 0) return true");
                    return true;
                }

                if (DebugConfig.DebugFocusFire) Log("DetectSpawn [" + AbyssalSpawn.DetectSpawn + "]");

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.EphialtesCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.NpcHasALotOfRemoteRepair))
                    {
                        if (DebugConfig.DebugFocusFire) Log("if (Combat.PotentialCombatTargets.Any(i => i.NpcHasALotOfRemoteRepair))");
                        return true;
                    }

                    if (DebugConfig.DebugFocusFire) Log("return false");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidCruiserSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.NpcHasALotOfRemoteRepair))
                    {
                        if (DebugConfig.DebugFocusFire) Log("if (Combat.PotentialCombatTargets.Any(i => i.NpcHasALotOfRemoteRepair))");
                        return true;
                    }

                    if (DebugConfig.DebugFocusFire) Log("return false");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.LucidFrigateSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.NpcHasALotOfRemoteRepair))
                    {
                        if (DebugConfig.DebugFocusFire) Log("if (Combat.PotentialCombatTargets.Any(i => i.NpcHasALotOfRemoteRepair))");
                        return true;
                    }

                    if (DebugConfig.DebugFocusFire) Log("return false");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.KarybdisTyrannosSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.NpcHasALotOfRemoteRepair))
                    {
                        if (DebugConfig.DebugFocusFire) Log("if (Combat.PotentialCombatTargets.Any(i => i.NpcHasALotOfRemoteRepair))");
                        return true;
                    }

                    if (DebugConfig.DebugFocusFire) Log("return false");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.AbyssalOvermindDroneBSSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.NpcHasALotOfRemoteRepair))
                    {
                        if (DebugConfig.DebugFocusFire) Log("if (Combat.PotentialCombatTargets.Any(i => i.NpcHasALotOfRemoteRepair))");
                        return true;
                    }

                    if (DebugConfig.DebugFocusFire) Log("return false");
                    return false;
                }

                if (AbyssalSpawn.DetectSpawn == AbyssalSpawn.AbyssalSpawnType.DroneFrigateSpawn)
                {
                    if (Combat.PotentialCombatTargets.Any(i => i.NpcHasALotOfRemoteRepair))
                    {
                        if (DebugConfig.DebugFocusFire) Log("if (Combat.PotentialCombatTargets.Any(i => i.NpcHasALotOfRemoteRepair))");
                        return true;
                    }

                    if (DebugConfig.DebugFocusFire) Log("return false");
                    return false;
                }

                return true;
            }
        }

        public IEnumerable<EntityCache> sortedListOfTargets
        {
            get
            {
                if (!ESCache.Instance.Targets.Any())
                    return new List<EntityCache>().OrderBy(i => i.Id);

                IEnumerable<EntityCache> _sortedListOfTargets = new List<EntityCache>();

                if (FocusFire)
                {
                    _sortedListOfTargets = ESCache.Instance.Targets.Where(i => i.GroupId != (int)Group.AssaultShip)
                    .OrderByDescending(e => e.Id ^ (long.MaxValue - 1337))
                    .ThenBy(k => k._directEntity.AbyssalTargetPriority)
                    .ThenByDescending(x => x.IsReadyToShoot)
                    .ThenByDescending(i => i.IsInOptimalRange)
                    .ThenByDescending(j => j.IsTrackable)
                    .ThenBy(l => l.Id);

                    return _sortedListOfTargets ?? new List<EntityCache>();
                }

                int HowManyToSkip = 0;
                foreach (var fm in ESCache.Instance.DirectEve.FleetMembers.OrderBy(i => i.CharacterId))
                {
                    if (fm.CharacterId == ESCache.Instance.DirectEve.Session.CharacterId)
                        break;

                    HowManyToSkip++;
                }

                _sortedListOfTargets = ESCache.Instance.Targets.Where(i => i.GroupId != (int)Group.AssaultShip)
                    .OrderByDescending(e => e.Id ^ (long.MaxValue - 1337))
                    .ThenBy(k => k._directEntity.AbyssalTargetPriority)
                    .ThenByDescending(x => x.IsReadyToShoot)
                    .ThenByDescending(i => i.IsInOptimalRange)
                    .ThenByDescending(j => j.IsTrackable)
                    .ThenBy(l => l.Id);

                if (HowManyToSkip == 0)
                {
                    return _sortedListOfTargets;
                }

                if (_sortedListOfTargets.Any() && _sortedListOfTargets.Count() > HowManyToSkip)
                {
                    _sortedListOfTargets = _sortedListOfTargets.Skip(HowManyToSkip);
                    if (DebugConfig.DebugFocusFire && DirectEve.Interval(10000))
                    {
                        Log("FocusFire [false]: HowManyToSkip [" + HowManyToSkip + "] _sortedListOfTargets [" + _sortedListOfTargets.Count() + "] ");
                        int intCount = 0;
                        foreach (var pct in _sortedListOfTargets)
                        {
                            intCount++;
                            Log("[" + intCount + "][" + pct.TypeName + "] Priority [" + pct._directEntity.AbyssalTargetPriority + "][" + pct.Nearest1KDistance + "k] Velocity [" + Math.Round(pct.Velocity, 0) + "m/s] Optimal [" + pct.OptimalRange + "] Falloff [" + pct._directEntity.AccuracyFalloff + "] IsInOptimalRangeOfMe [" + pct._directEntity.IsInNPCsOptimalRange + "] ID [" + pct.MaskedId + "] S[" + Math.Round(pct.ShieldPct * 100, 0) + "%] A[" + Math.Round(pct.ArmorPct * 100, 0) + "%] H[" + Math.Round(pct.StructurePct * 100, 0) + "%] S[" + Math.Round(pct.ShieldCurrentHitPoints, 0) + "] A[" + Math.Round(pct.ArmorCurrentHitPoints, 0) + "] H[" + Math.Round(pct.StructureCurrentHitPoints, 0) + "] Locked [" + pct.IsTarget + "] IsAttacking [" + pct._directEntity.IsAttacking + "] IsTargetedBy [" + pct._directEntity.IsTargetedBy + "] IsYellowBoxing [" + pct._directEntity.IsYellowBoxing + "] IsInSpeedCloud [" + pct._directEntity.IsInSpeedCloud + "][" + pct.stringEwarTypes + "] IsNPCFrigate [" + pct.IsNPCFrigate + "] IsNPCDestroyer [" + pct.IsNPCDestroyer + "] IsNPCCruiser [" + pct.IsNPCCruiser + "] IsNPCBattlecruiser [" + pct.IsNPCBattlecruiser + "] IsNPCBattleship [" + pct.IsNPCBattleship + "] IsTooCloseToSmallDeviantAutomataSuppressor [" + pct._directEntity.IsTooCloseToSmallDeviantAutomataSuppressor + "] IsTooCloseToMediumDeviantAutomataSuppressor [" + pct._directEntity.IsTooCloseToMediumDeviantAutomataSuppressor + "]");
                        }
                    }
                }

                return _sortedListOfTargets;
            }
        }

        internal bool ManageWeapons()
        {
            try
            {
                if (DebugConfig.DebugCombat)
                    Log("ManageWeapons()");

                if (ESCache.Instance.InWarp)
                    return true;

                if (!ESCache.Instance.Weapons.Any())
                {
                    if (DebugConfig.DebugCombat) Log("if (!ESCache.Instance.Weapons.Any())");
                    return true;
                }

                HandleOverloadWeapons();

                // we assume the weapon is grouped (we auto group during travel) -- so code below works only if we have 1 weapon!
                if (!DirectEve.Me.IsInAbyssalSpace())
                {
                    if (DebugConfig.DebugCombat) Log("if (!DirectEve.Me.IsInAbyssalSpace())");

                    return false;
                }

                //Cancel Repair if needed
                if (ESCache.Instance.Entities.Any(i => i.BracketType == BracketType.Large_Collidable_Structure || i.IsPotentialCombatTarget))
                {
                    if (ESCache.Instance.Weapons.Any(i => i._module.IsBeingRepaired))
                    {
                        foreach (var thisWeapon in ESCache.Instance.Weapons.Where(i => i._module.IsBeingRepaired))
                        {
                            if (thisWeapon._module.CancelRepair())
                            {
                                Log("Canceling repair on [" + thisWeapon.TypeName + "] due enemies are on grid.");
                                continue;
                            }

                            continue;
                        }

                        return true;
                    }
                }
                else
                {
                    //Repair if needed
                    if (ESCache.Instance.DirectEve.GetShipsCargo() != null)
                    {
                        if (ESCache.Instance.DirectEve.GetShipsCargo().Items.Any(i => i.TypeId == _naniteRepairPasteTypeId))
                        {
                            foreach (var thisWeapon in ESCache.Instance.Weapons.Where(i => !i._module.IsBeingRepaired && i._module.HeatDamagePercent > 0 && !i.IsActive).OrderByDescending(i => i._module.IsMaster).DistinctBy(x => x.TypeId).OrderByDescending(i => new Guid()))
                            {
                                if (!ShouldWeRepair)
                                    continue;

                                // repair
                                if (thisWeapon._module.Repair())
                                {
                                    Log($"Repairing [{thisWeapon.TypeName}].");
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            DirectEve.IntervalLog(60000, 60000,
                                "No nanite repair paste found in cargo, can't repair.");
                        }
                    }

                }

                var weapons = ESCache.Instance.DirectEve.Weapons;

                if (ESCache.Instance.ActiveShip.TypeId != (int)TypeID.Retribution)
                {
                    if (!weapons.Any(x => x.IsVortonProjector) && !weapons.Any(_ => _.IsMaster))
                    {
                        if (DebugConfig.DebugCombat) Log("if (!weapons.Any(x => x.IsVortonProjector) && !weapons.Any(_ => _.IsMaster))");

                        return false;
                    }
                }


                // if we have a current target
                if (weapons.Any(w => w.IsActive && !w.IsInLimboState))
                {
                    try
                    {
                        // if target is not within distance anymore -> stop
                        var tempShouldWeDeactivateWeapons = ShouldWeDeactivateWeapons;
                        if (tempShouldWeDeactivateWeapons)
                        {
                            if (ESCache.Instance.Weapons.Any(i => i.IsEnergyWeapon))
                            {
                                foreach (var thisWeapon in weapons.Where(w => w.IsActive))
                                {
                                    if (thisWeapon.IsInLimboState)
                                    {
                                        if (DebugConfig.DebugCombat) Log("[" + thisWeapon.TypeName + "] IsInLimboState [" + thisWeapon.IsInLimboState + "] IsOnline [" + thisWeapon.IsOnline + "] IsActivatable [" + thisWeapon.IsActivatable + "] IsDeactivating [" + thisWeapon.IsDeactivating + "] IsReloadingAmmo [" + thisWeapon.IsReloadingAmmo + "] IsBeingRepaired [" + thisWeapon.IsBeingRepaired + "] EffectActivating [" + thisWeapon.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(thisWeapon) + "] ReactivationDelay [" + thisWeapon.ReactivationDelay + " > 0]");
                                        continue;
                                    }

                                    if (DirectEve.Interval(1000, 1500, thisWeapon.ItemId.ToString()))
                                    {
                                        if (thisWeapon.Deactivate())
                                        {
                                            continue;
                                        }
                                    }
                                }

                                return true;
                            }

                            if (weapons.Any(x => x.IsActive && !x.IsInLimboState))
                            {
                                if (DirectEve.Interval(1000, 1500) && weapons.FirstOrDefault(x => x.IsActive && !x.IsInLimboState).Deactivate())
                                {
                                    Log($"Deactivating weapon: ShouldWeDeactivateWeapons [" + tempShouldWeDeactivateWeapons + "]");
                                    return true;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log("Exception [" + ex + "]");
                    }
                }

                if (weapons.All(w => w.IsActive))
                {
                    if (DebugConfig.DebugCombat) Log("if (weapons.All(w => w.IsActive))");

                    return false;
                }

                var shipsCargo = DirectEve.GetShipsCargo();
                if (shipsCargo == null)
                {
                    if (DebugConfig.DebugCombat) Log("if (shipsCargo == null)");

                    return false;
                }

                if (ESCache.Instance.Weapons.All(i => !i.IsEnergyWeapon))
                {
                    // reload weapon needed - this should be being done in AmmoManagementBehavior
                    if (weapons.Any(w => w.ChargeQty == 0 || w.ChargeQty > w.MaxCharges) && !weapons.Any(i => i.IsInLimboState))
                    {
                        Log("if (weapons.Any(w => w.CurrentCharges == 0 || w.CurrentCharges > w.MaxCharges))");
                        if (weapons.FirstOrDefault().ChangeAmmo(Combat.UsableAmmoInCargo.FirstOrDefault()))
                        {
                            return false;
                        }
                    }
                }


                // here we attack the target
                var weapon = weapons.Where(i => !i.IsActive && !i.IsInLimboState).FirstOrDefault();

                if (weapon == null)
                {
                    if (DebugConfig.DebugCombat) Log("weapon == null");
                    return false;
                }


                // Shoot bioadaptive caches that are in range first
                if (ESCache.Instance.Targets.Any(e => e.IsAbyssalBioAdaptiveCache))
                {
                    if (DebugConfig.DebugCombat) Log("ESCache.Instance.Targets.Any(e => e.IsAbyssalBioAdaptiveCache)");
                    var BioAdaptiveCache = ESCache.Instance.Targets.Where(e => e.IsReadyToShoot && e.IsAbyssalBioAdaptiveCache).ToList().FirstOrDefault();
                    if (BioAdaptiveCache != null)
                    {
                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                        {
                            foreach (var thisWeapon in weapons.Where(w => w.ChargeQty > 0 && !w.IsActive))
                            {
                                if (thisWeapon.IsInLimboState)
                                {
                                    if (DebugConfig.DebugCombat) Log("[" + thisWeapon.TypeName + "] IsInLimboState [" + thisWeapon.IsInLimboState + "] IsOnline [" + thisWeapon.IsOnline + "] IsActivatable [" + thisWeapon.IsActivatable + "] IsDeactivating [" + thisWeapon.IsDeactivating + "] IsReloadingAmmo [" + thisWeapon.IsReloadingAmmo + "] IsBeingRepaired [" + thisWeapon.IsBeingRepaired + "] EffectActivating [" + thisWeapon.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(thisWeapon) + "] ReactivationDelay [" + thisWeapon.ReactivationDelay + " > 0]");
                                    continue;
                                }

                                if (DirectEve.Interval(400, 400, weapon.ItemId.ToString()))
                                {
                                    if (thisWeapon.Activate(BioAdaptiveCache.Id))
                                    {
                                        continue;
                                    }
                                }
                            }
                        }

                        if (DirectEve.Interval(400, 400, weapon.ItemId.ToString()))
                        {
                            if (DebugConfig.DebugCombat) Log("Attempt to Activate weapon [" + weapon.TypeName + "][" + weapon.ItemId + "] on [" + BioAdaptiveCache.TypeName + "]");
                            if (weapon.Activate(BioAdaptiveCache.Id))
                            {
                                if (DebugConfig.DebugCombat) Log("Activate weapon [" + weapon.TypeName + "][" + weapon.ItemId + "] on [" + BioAdaptiveCache.TypeName + "].");
                                return true;
                            }
                        }
                    }
                }

                // focus targets which are attacked by drones and are within weapon rage
                if (ESCache.Instance.ActiveShip.HasDroneBay)
                {
                    if (DebugConfig.DebugCombat) Log("if (ESCache.Instance.ActiveShip.HasDroneBay)");
                    var currentDroneTargets = allDronesInSpace.Where(e => e._directEntity.FollowEntity != null)
                    .Select(e => e._directEntity.FollowEntity)
                    .Where(e => e.Distance < Combat.MaxWeaponRange && e.IsTarget && !e.IsAbyssalDeadspaceTriglavianExtractionNode && !e.IsWreck)
                    .OrderBy(k => k.AbyssalTargetPriority)
                    .ThenByDescending(x => x.IsReadyToShoot)
                    //.ThenByDescending(i => i.IsInOptimalRange)
                    //.ThenByDescending(j => j.IsTrackable)
                    .ThenBy(x => x.StructurePct)
                    .ThenBy(y => y.ArmorPct)
                    .ThenBy(z => z.ShieldPct)
                    .ToList();

                    if (currentDroneTargets != null && currentDroneTargets.Any())
                    {
                        if (DebugConfig.DebugCombat) Log("if (currentDroneTargets != null && currentDroneTargets.Any())");
                        if (DirectEve.Interval(400, 400, weapon.ItemId.ToString()))
                        {
                            if (DebugConfig.DebugCombat) Log("Activate weapon on [" + currentDroneTargets.FirstOrDefault().TypeName + "].");
                            if (weapon.Activate(currentDroneTargets.OrderBy(i => i.StructurePct).ThenBy(i => i.ArmorPct).ThenBy(i => i.ShieldPct).FirstOrDefault().Id))
                            {
                                return true;
                            }
                        }
                    }
                }

                // if our current target is below maxRange we shoot that regardless
                // else we choose based on distance
                if (ESCache.Instance.Targets.Any(i => i.GroupId != (int)Group.AssaultShip))
                {
                    if (DebugConfig.DebugCombat) Log("if (ESCache.Instance.Targets.Any(i => i.GroupId != (int)Group.AssaultShip))");
                    EntityCache target = null;
                    if (sortedListOfTargets.Any())
                    {
                        if (DebugConfig.DebugCombat) Log("if (sortedListOfTargets.Any())");
                        if (Combat.CurrentWeaponTarget() != null)
                        {
                            if (DebugConfig.DebugCombat) Log("if (Combat.CurrentWeaponTarget() != null)");
                            if (sortedListOfTargets.Any(i => Combat.CurrentWeaponTarget().HealthPct > i.HealthPct))
                            {
                                if (DebugConfig.DebugCombat) Log("if (sortedListOfTargets.Any(i => Combat.CurrentWeaponTarget().HealthPct > i.HealthPct))");
                                //switch to the target with lower health
                                target = sortedListOfTargets.Where(i => i.IsReadyToShoot)
                                        .OrderByDescending(e => e.Id ^ (long.MaxValue - 1337))
                                        .OrderBy(k => k._directEntity.AbyssalTargetPriority)
                                        .ThenByDescending(x => x.IsReadyToShoot)
                                        .ThenByDescending(i => i.IsInOptimalRange)
                                        .ThenByDescending(j => j.IsTrackable)
                                        .ThenBy(x => x.StructurePct)
                                        .ThenBy(y => y.ArmorPct)
                                        .ThenBy(z => z.ShieldPct)
                                        .FirstOrDefault();
                                if (target == null)
                                {
                                    target = sortedListOfTargets
                                        .OrderByDescending(e => e.Id ^ (long.MaxValue - 1337))
                                        .OrderBy(k => k._directEntity.AbyssalTargetPriority)
                                        .ThenByDescending(x => x.IsReadyToShoot)
                                        .ThenByDescending(i => i.IsInOptimalRange)
                                        .ThenByDescending(j => j.IsTrackable)
                                        .ThenBy(x => x.StructurePct)
                                        .ThenBy(y => y.ArmorPct)
                                        .ThenBy(z => z.ShieldPct)
                                        .FirstOrDefault();
                                }

                            }
                            else
                            {
                                if (DebugConfig.DebugCombat) Log("!if (sortedListOfTargets.Any(i => Combat.CurrentWeaponTarget().HealthPct > i.HealthPct))");
                                //our currenttarget has the lowest health!
                                target = sortedListOfTargets
                                        .OrderBy(a => Combat.CurrentWeaponTarget() != null && a.Id == Combat.CurrentWeaponTarget().Id)
                                        .ThenBy(k => k._directEntity.AbyssalTargetPriority)
                                        .ThenByDescending(x => x.IsReadyToShoot)
                                        .ThenByDescending(i => i.IsInOptimalRange)
                                        .ThenByDescending(j => j.IsTrackable)
                                        .ThenBy(x => x.StructurePct)
                                        .ThenBy(y => y.ArmorPct)
                                        .ThenBy(z => z.ShieldPct)
                                        .FirstOrDefault();
                            }
                        }
                        else
                        {
                            if (DebugConfig.DebugCombat) Log("!if (Combat.CurrentWeaponTarget() != null)");
                            //we dont have a currenttarget yet switch to the target with lower health
                            target = sortedListOfTargets
                                        .OrderByDescending(e => e.Id ^ (long.MaxValue - 1337))
                                        .OrderBy(k => k._directEntity.AbyssalTargetPriority)
                                        .ThenByDescending(x => x.IsReadyToShoot)
                                        .ThenByDescending(i => i.IsInOptimalRange)
                                        .ThenByDescending(j => j.IsTrackable)
                                        .ThenBy(x => x.StructurePct)
                                        .ThenBy(y => y.ArmorPct)
                                        .ThenBy(z => z.ShieldPct)
                                        .FirstOrDefault();
                        }
                    }

                    if (target != null && target.Distance <= Combat.MaxWeaponRange)
                    {
                        if (DebugConfig.DebugCombat) Log("if (target != null && target.Distance <= Combat.MaxWeaponRange)");

                        if (ESCache.Instance.ActiveShip.TypeId == (int)TypeID.Retribution)
                        {
                            foreach (var thisWeapon in weapons.Where(w => w.ChargeQty > 0 && !w.IsActive))
                            {
                                if (thisWeapon.IsInLimboState)
                                {
                                    if (DebugConfig.DebugCombat) Log("[" + thisWeapon.TypeName + "] IsInLimboState [" + thisWeapon.IsInLimboState + "] IsOnline [" + thisWeapon.IsOnline + "] IsActivatable [" + thisWeapon.IsActivatable + "] IsDeactivating [" + thisWeapon.IsDeactivating + "] IsReloadingAmmo [" + thisWeapon.IsReloadingAmmo + "] IsBeingRepaired [" + thisWeapon.IsBeingRepaired + "] EffectActivating [" + thisWeapon.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(thisWeapon) + "] ReactivationDelay [" + thisWeapon.ReactivationDelay + " > 0]");
                                    continue;
                                }

                                if (DirectEve.Interval(300, 400, thisWeapon.ItemId.ToString()))
                                {
                                    if (thisWeapon.Activate(target.Id))
                                    {
                                        continue;
                                    }
                                }
                            }

                            return true;
                        }

                        if (DirectEve.Interval(400, 400, weapon.ItemId.ToString()))
                        {
                            if (DebugConfig.DebugCombat) Log("Activate weapon on ![" + target.TypeName + "]!");
                            if (weapon.Activate(target.Id))
                            {
                                return true;
                            }

                            return false;
                        }
                    }
                    else
                    {
                        if (DebugConfig.DebugCombat) Log("!if (target != null && target.Distance <= Combat.MaxWeaponRange)");
                        // pick the closest one
                        var closestTarget = GetSortedTargetList(ESCache.Instance.Targets.Where(i => i.GroupId != (int)Group.AssaultShip)).OrderBy(e => e.Distance)
                            .FirstOrDefault();
                        if (closestTarget != null && closestTarget.Distance <= Combat.MaxWeaponRange)
                        {
                            if (ESCache.Instance.Weapons.Any(i => i.IsEnergyWeapon))
                            {
                                foreach (var thisWeapon in weapons.Where(w => w.ChargeQty > 0 && !w.IsActive))
                                {
                                    if (thisWeapon.IsInLimboState)
                                    {
                                        if (DebugConfig.DebugCombat) Log("[" + thisWeapon.TypeName + "] IsInLimboState [" + thisWeapon.IsInLimboState + "] IsOnline [" + thisWeapon.IsOnline + "] IsActivatable [" + thisWeapon.IsActivatable + "] IsDeactivating [" + thisWeapon.IsDeactivating + "] IsReloadingAmmo [" + thisWeapon.IsReloadingAmmo + "] IsBeingRepaired [" + thisWeapon.IsBeingRepaired + "] EffectActivating [" + thisWeapon.EffectActivating + "] IsEffectActivating(this) [" + DirectEve.IsEffectActivating(thisWeapon) + "] ReactivationDelay [" + thisWeapon.ReactivationDelay + " > 0]");
                                        continue;
                                    }

                                    if (DirectEve.Interval(400, 400, thisWeapon.ItemId.ToString()))
                                    {
                                        if (thisWeapon.Activate(closestTarget.Id))
                                        {
                                            continue;
                                        }
                                    }
                                }

                                return true;
                            }

                            if (DebugConfig.DebugCombat) Log("if (closestTarget != null && closestTarget.Distance <= Combat.MaxWeaponRange)");
                            if (DirectEve.Interval(400, 400, weapon.ItemId.ToString()))
                            {
                                if (weapon.Activate(closestTarget.Id))
                                {
                                    return true;
                                }

                                return false;
                            }
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

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }
    }
}