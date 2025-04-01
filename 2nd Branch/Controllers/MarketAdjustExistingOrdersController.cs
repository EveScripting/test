extern alias SC;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Lookup;
using SC::SharedComponents.IPC;
using System;
using System.Linq;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Description of AdjustExistingMarketOrdersController.
    /// </summary>
    public class MarketAdjustExistingOrdersController : BaseController
    {
        #region Constructors

        public MarketAdjustExistingOrdersController()
        {
            IgnorePause = false;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Methods

        public static void ProcessState()
        {
            try
            {
                // MarketAdjustBehavior
                MarketAdjustBehavior.ProcessState();
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
                if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                    if (ESCache.Instance.Stargates.Count > 0)
                        if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.IsOnGridWithMe && ESCache.Instance.ClosestStargate.Distance < 10000)
                        {
                            if (DebugConfig.DebugCleanup) Log("CheckModalWindows: We are within 10k of a stargate, do nothing while we wait to jump.");
                            return;
                        }

                if (!Settings.Instance.DefaultSettingsLoaded)
                    Settings.Instance.LoadSettings_Initialize();

                //if (DebugConfig.DebugDefenseController) Log("CombatController.DoWork()");

                ProcessState();
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
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