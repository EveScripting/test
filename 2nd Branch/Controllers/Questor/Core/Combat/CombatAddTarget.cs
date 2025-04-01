using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Questor.Combat
{
    public static partial class Combat
    {
        #region Methods

        public static void AddPrimaryWeaponPriorityTarget(EntityCache ewarEntity, PrimaryWeaponPriority priority, bool AddEwarTypeToPriorityTargetList = true)
        {
            try
            {
                if (ewarEntity.IsIgnored || PrimaryWeaponPriorityTargets.Any(p => p.EntityID == ewarEntity.Id))
                {
                    if (DebugConfig.DebugAddPrimaryWeaponPriorityTarget)
                        Log.WriteLine("if ((target.IsIgnored) || PrimaryWeaponPriorityTargets.Any(p => p.Id == target.Id)) continue");
                    return;
                }

                if (AddEwarTypeToPriorityTargetList)
                {
                    if (DoWeCurrentlyHaveTurretsMounted() && (ewarEntity.IsNPCFrigate || ewarEntity.IsFrigate))
                    {
                        if (!ewarEntity.IsTooCloseTooFastTooSmallToHit)
                            if (PrimaryWeaponPriorityTargets.All(e => e.EntityID != ewarEntity.Id))
                            {
                                Log.WriteLine("Adding [" + ewarEntity.Name + "] Speed [" + Math.Round(ewarEntity.Velocity, 2) + "m/s] Distance [" +
                                              Math.Round(ewarEntity.Distance / 1000, 2) + "k] [ID: " + ewarEntity.MaskedId +
                                              "] as a PrimaryWeaponPriorityTarget [" +
                                              priority + "]");
                                _primaryWeaponPriorityTargets.Add(new PriorityTarget
                                {
                                    Name = ewarEntity.Name,
                                    EntityID = ewarEntity.Id,
                                    PrimaryWeaponPriority = priority
                                });
                            }

                        return;
                    }

                    if (PrimaryWeaponPriorityTargets.All(e => e.EntityID != ewarEntity.Id))
                    {
                        Log.WriteLine("Adding [" + ewarEntity.Name + "] Speed [" + Math.Round(ewarEntity.Velocity, 2) + "m/s] Distance [" +
                                      Math.Round(ewarEntity.Distance / 1000, 2) + "] [ID: " + ewarEntity.MaskedId +
                                      "] as a PrimaryWeaponPriorityTarget [" +
                                      priority + "]");
                        _primaryWeaponPriorityTargets.Add(new PriorityTarget
                        {
                            Name = ewarEntity.Name,
                            EntityID = ewarEntity.Id,
                            PrimaryWeaponPriority = priority
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static void AddTargetsToEcmByName(string stringEntitiesToAdd)
        {
            try
            {
                EntityCache entityToAdd = ESCache.Instance.EntitiesOnGrid.Where(i => i.Name.ToLower() == stringEntitiesToAdd.ToLower()).OrderBy(j => j.Nearest5kDistance).FirstOrDefault();

                if (entityToAdd != null)
                {
                    Log.WriteLine("adding [" + entityToAdd.Name + "][" + Math.Round(entityToAdd.Distance / 1000, 0) + "k][" + entityToAdd.MaskedId +
                                  "] to the ECM List");
                    if (!ESCache.Instance.ListofEntitiesToEcm.Contains(entityToAdd.Id))
                        ESCache.Instance.ListofEntitiesToEcm.Add(entityToAdd.Id);

                    return;
                }

                Log.WriteLine("[" + stringEntitiesToAdd + "] was not found on grid");
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        public static void AddWebifierByName(string stringEntitiesToAdd, int numberToIgnore = 0, bool notTheClosest = false)
        {
            try
            {
                IEnumerable<EntityCache> entitiesToAdd =
                    ESCache.Instance.EntitiesOnGrid.Where(i => i.Name.ToLower() == stringEntitiesToAdd.ToLower()).OrderBy(j => j.Nearest5kDistance).ToList();
                if (notTheClosest)
                    entitiesToAdd = entitiesToAdd.OrderByDescending(e => e.Nearest5kDistance);

                if (entitiesToAdd.Any())
                {
                    foreach (EntityCache entityToAdd in entitiesToAdd)
                    {
                        if (numberToIgnore > 0)
                        {
                            numberToIgnore--;
                            continue;
                        }
                        Log.WriteLine("adding [" + entityToAdd.Name + "][" + Math.Round(entityToAdd.Distance / 1000, 0) + "k][" + entityToAdd.MaskedId +
                                      "] to the Webifier List");
                        if (!ESCache.Instance.ListofWebbingEntities.Contains(entityToAdd.Id))
                            ESCache.Instance.ListofWebbingEntities.Add(entityToAdd.Id);
                    }

                    return;
                }

                Log.WriteLine("[" + stringEntitiesToAdd + "] was not found on grid");
            }
            catch (Exception ex)
            {
                Log.WriteLine("Exception [" + ex + "]");
            }
        }

        #endregion Methods
    }
}