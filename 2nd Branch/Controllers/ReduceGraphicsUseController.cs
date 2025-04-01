extern alias SC;
using EVESharpCore.Cache;
﻿using EVESharpCore.Controllers.Base;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using System;
using System.Linq;
using System.Diagnostics;
using SC::SharedComponents.Utility;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     ReduceGraphicLoadController
    ///     Goal of this controller is to reduce the impact we have on GPU (and to a lesser extent CPU) usage
    ///     like:
    ///     * disabling 3d
    ///     * zooming out far enough that the individual ships are no longer rendered
    ///     etc.
    /// </summary>
    public class ReduceGraphicLoadController : BaseController
    {
        #region Constructors

        public ReduceGraphicLoadController()
        {
            IgnorePause = true;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Methods

        private static Stopwatch stopwatch = new Stopwatch();

        public static void ProcessState()
        {
            try
            {
                if (DebugConfig.DebugReduceGraphicsController) Log("ReduceGraphicsController: ProcessState: DisableResourceLoad [" + Settings.Instance.DisableResourceLoad + "] ResourceLoad [" + ESCache.Instance.DirectEve.ResourceLoad + "]");
                // ReduceGraphicLoad

                ReduceGraphicLoad.ProcessState();
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
        }

        public override void DoWork()
        {
            try
            {
                stopwatch.Restart();
                ReduceGraphicLoad.IsPaused = IsPaused;
                //ReduceGraphicLoad.IsWorkDone = IsWorkDone;

                if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                {
                    if (Time.Instance.LastInWarp.AddSeconds(4) > DateTime.UtcNow)
                        return;

                    if (ESCache.Instance.Stargates.Count > 0 && Time.Instance.LastInWarp.AddSeconds(30) > DateTime.UtcNow)
                        if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.IsOnGridWithMe && ESCache.Instance.ClosestStargate.Distance < 10000)
                            return;
                }

                try
                {
                    if (!Settings.Instance.DefaultSettingsLoaded)
                        Settings.Instance.LoadSettings_Initialize();
                }
                catch (Exception ex)
                {
                    Log("Exception [" + ex + "]");
                }

                ProcessState();
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
            finally
            {
                if (!ReduceGraphicLoad.ZoomLevelAlreadyProcessed)
                    LocalPulse = UTCNowAddMilliseconds(1500, 2450);
                else
                    LocalPulse = UTCNowAddMilliseconds(14000, 16500);

                stopwatch.Stop();
                if (DebugConfig.DebugReduceGraphicsController) Log("ReduceGraphicsController Took [" + Util.ElapsedMicroSeconds(stopwatch) + "]");
            }
        }

        public override bool EvaluateDependencies(ControllerManager cm)
        {
            return true;
        }

        public override void ReceiveBroadcastMessage(BroadcastMessage broadcastMessage)
        {

        }

        #endregion Methods
    }
}