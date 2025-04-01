extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using System;
using System.Linq;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;
using EVESharpCore.Framework.Lookup;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Salvage Controller
    ///     Tractor and Salvage as needed, ignoring pause...
    ///     Note: will not move your ship, ever.
    /// </summary>
    public class SalvageController : BaseController
    {
        #region Constructors

        public SalvageController() : base()
        {
            AllowRunInStation = false;
            IgnorePause = true;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Methods

        public static void ProcessState()
        {
            try
            {
                switch (ESCache.Instance.SelectedController)
                {
                    case "CombatDontMoveController":
                    case "WspaceSiteController":
                    case "SalvageGridController":
                        if (ESCache.Instance.Paused)
                            return;

                        break;
                }

                // salvage
                if (DebugConfig.DebugSalvage) Log("SalvageState is [" + State.CurrentSalvageState + "]");
                Salvage.ProcessState();
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        private bool ShouldWeSalvage
        {
            get
            {
                if (Time.Instance.Started_DateTime.AddSeconds(10) > DateTime.UtcNow)
                {
                    LocalPulse = UTCNowAddSeconds(10, 12);
                    return false;
                }

                if (ESCache.Instance.InAbyssalDeadspace)
                    return true;

                if (ESCache.Instance.EntitiesOnGrid.Count == 0)
                {
                    LocalPulse = UTCNowAddSeconds(10, 12);
                    return false;
                }

                // Nothing to salvage in stations
                if (ESCache.Instance.InStation || !ESCache.Instance.InSpace || ESCache.Instance.InWarp)
                {
                    if (DebugConfig.DebugSalvage) Log("if (ESCache.Instance.InStation || !ESCache.Instance.InSpace)");

                    LocalPulse = UTCNowAddSeconds(10, 12);
                    return false;
                }

                // When in warp there's nothing we can do, so ignore everything
                if (ESCache.Instance.InSpace && ESCache.Instance.InWarp)
                {
                    if (DebugConfig.DebugSalvage) Log("if (ESCache.Instance.InSpace && ESCache.Instance.InWarp)");
                    LocalPulse = UTCNowAddSeconds(10, 12);
                    return false;
                }

                // There is no salving when cloaked -
                // why not? seems like we might be able to ninja-salvage with a covert-ops hauler with some additional coding (someday?)
                if (ESCache.Instance.ActiveShip.Entity.IsCloaked)
                {
                    //ShouldWeDecloak();
                    if (DebugConfig.DebugSalvage) Log("if (ESCache.Instance.ActiveShip.Entity.IsCloaked)");
                    LocalPulse = UTCNowAddSeconds(10, 12);
                    return false;
                }

                if (ESCache.Instance.Wrecks.Count == 0)
                {
                    if (DebugConfig.DebugSalvage) Log("if (ESCache.Instance.Wrecks.Count == 0 && ESCache.Instance.UnlootedContainers.Count == 0)");
                    LocalPulse = UTCNowAddMilliseconds(4000, 4500);
                    return false;
                }

                return true;
            }
        }

        public override void DoWork()
        {
            try
            {
                if (DebugConfig.DebugDisableSalvage)
                    return;

                if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                {
                    if (Time.Instance.LastInWarp.AddSeconds(2) > DateTime.UtcNow)
                        return;

                    if (ESCache.Instance.Stargates.Count > 0 && Time.Instance.LastInWarp.AddSeconds(30) > DateTime.UtcNow)
                        if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.IsOnGridWithMe && ESCache.Instance.ClosestStargate.Distance < (int)Distances.JumpRange)
                        {
                            if (DebugConfig.DebugSalvage) Log("We are within [" + Distances.JumpRange + "] of a stargate, do nothing while we wait to jump.");
                            return;
                        }
                }

                if (!Settings.Instance.DefaultSettingsLoaded)
                    Settings.Instance.LoadSettings_Initialize();

                if (!ShouldWeSalvage) return;

                ProcessState();
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
            finally
            {
                if (DateTime.UtcNow > LocalPulse)
                    LocalPulse = UTCNowAddMilliseconds(4000, 4500);

                if (Salvage.TractorBeams.Count > 0 && (ESCache.Instance.Wrecks.Any(i => Salvage.TractorBeamRange > i.Distance) || ESCache.Instance.UnlootedContainers.Any(i => Salvage.TractorBeamRange > i.Distance)))
                {
                    LocalPulse = UTCNowAddMilliseconds(1500, 2500);
                }

                if (Salvage.Salvagers.Count > 0 && (ESCache.Instance.Wrecks.Any(i => Salvage.SalvagerRange > i.Distance) || ESCache.Instance.UnlootedContainers.Any(i => Salvage.SalvagerRange > i.Distance)))
                {
                    LocalPulse = UTCNowAddMilliseconds(1500, 2500);
                }

                if (ESCache.Instance.Wrecks.Any(i => (double)Distances.ScoopRange > i.Distance + 5000) || ESCache.Instance.UnlootedContainers.Any(i => (double)Distances.ScoopRange > i.Distance + 5000))
                {
                    LocalPulse = UTCNowAddMilliseconds(500, 800);
                }
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            //if (ESCache.Instance.SelectedController == nameof(EveAccount.AvailableControllers.AbyssalIshtarController)) // do not run it while an abyss controller is running
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