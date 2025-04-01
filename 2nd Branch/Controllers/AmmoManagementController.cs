extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Lookup;
using System;
using SC::SharedComponents.EVE;
using EVESharpCore.Questor.Behaviors;
using System.Linq;
using SC::SharedComponents.IPC;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Combat Controller
    ///     Kill things as needed, respecting pause...
    ///     Note: will not move your ship, ever.
    /// </summary>
    public class AmmoManagementController : BaseController
    {
        #region Constructors

        public AmmoManagementController()
        {
            IgnorePause = false;
            IgnoreModal = true;
            AllowRunInStation = false;
        }

        #endregion Constructors

        #region Methods

        public static void ProcessState()
        {
            if (DebugConfig.DebugDisableAmmoManagement)
                return;

            if (ESCache.Instance.Paused)
            {
                if (DebugConfig.DebugAmmoManagement) Log("IsPaused");
                return;
            }

            if (IsWormholePVPHappening)
            {
                if (DebugConfig.DebugAmmoManagement) Log("PVP happening? if (ESCache.Instance.EntitiesNotSelf.Any(i => i.IsPlayer && i.TypeId != ESCache.Instance.ActiveShip.Entity.TypeId && i.GroupId != (int)Group.Marauder && i.GroupId != (int)Group.CommandShip && i.GroupId != (int)Group.Logistics))");
                return;
            }

            AmmoManagementBehavior.ProcessState();
        }

        private static bool IsWormholePVPHappening
        {
            get
            {
                if (ESCache.Instance.EntitiesNotSelf.Any(i => i.IsOnGridWithMe && i.IsPlayer))
                {
                    foreach (var player in ESCache.Instance.EntitiesNotSelf.Where(i => i.IsOnGridWithMe && i.IsPlayer))
                    {
                        if (player.TypeId != ESCache.Instance.ActiveShip.Entity.TypeId)
                        {
                            if (player.GroupId != (int)Group.Marauder && player.GroupId != (int)Group.Battleship)
                            {
                                if (player.GroupId != (int)Group.CommandShip)
                                {
                                    if (player.GroupId != (int)Group.Logistics)
                                    {
                                        if (player.TypeId != (int)TypeID.Loki)
                                        {
                                            if (player.TypeId != (int)TypeID.Talos)
                                            {
                                                if (player.TypeId != (int)TypeID.Bifrost)
                                                {
                                                    if (player.TypeId != (int)TypeID.Stork && player.TypeId != (int)TypeID.Mastodon)
                                                    {
                                                        if (player.GroupId != (int)Group.Dreadnaught)
                                                        {
                                                            return true;
                                                        }

                                                        //Dreadnaught are assumed to be in our fleet - safe?
                                                        continue;
                                                    }

                                                    //Stork are assumed to be in our fleet - safe?
                                                    continue;
                                                }

                                                //Bifrost are assumed to be in our fleet - safe?
                                                continue;
                                            }

                                            //Talos are assumed to be in our fleet - safe?
                                            continue;
                                        }

                                        //Loki are assumed to be in our fleet - safe?
                                        continue;
                                    }

                                    //Logistics are assumed to be in our fleet - safe?
                                    continue;
                                }

                                //CommandShip is assumed to be in our fleet - safe?
                                continue;
                            }

                            //Maurauder or BS is assumed to be in our fleet - safe?
                            continue;
                        }

                        continue;
                    }

                    return false;
                }

                return false;
            }
        }
        public override void DoWork()
        {
            try
            {
                if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                {
                    if (Time.Instance.LastInWarp.AddSeconds(1) > DateTime.UtcNow)
                        return;

                    if (ESCache.Instance.Stargates.Count > 0 && Time.Instance.LastInWarp.AddSeconds(10) > DateTime.UtcNow)
                        if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.IsOnGridWithMe && ESCache.Instance.ClosestStargate.Distance < (int)Distances.JumpRange)
                        {
                            if (DebugConfig.DebugAmmoManagement) Log("We are within [" + Distances.JumpRange + "] of a stargate, do nothing while we wait to jump.");
                            return;
                        }
                }

                //if (!Settings.Instance.DefaultSettingsLoaded)
                //    Settings.Instance.LoadSettings_Initialize();

                if (DebugConfig.DebugCombatController) Log("AmmoManagementController.DoWork()");

                ProcessState();
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
            finally
            {
                if (ESCache.Instance.Weapons.Any(i => i.IsEnergyWeapon))
                {
                    LocalPulse = UTCNowAddMilliseconds(500, 1000);
                }
                else LocalPulse = UTCNowAddMilliseconds(1500, 2000);
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            //if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalController)) // do not run it while an abyss controller is running
            //{
            //    return false;
            //}

            return true;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}