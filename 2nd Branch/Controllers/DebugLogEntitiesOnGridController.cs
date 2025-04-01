extern alias SC;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.Stats;
using SC::SharedComponents.IPC;
using System;
using System.Linq;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Description of DefenseController.
    /// </summary>
    public class DebugLogEntitiesOnGridController : BaseController
    {
        #region Constructors

        public DebugLogEntitiesOnGridController()
        {
            IgnorePause = true;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Fields

        //private static bool finished = false;

        #endregion Fields

        #region Methods

        public static void ProcessState()
        {
            Log("Log Entities on Grid.");
            if (!Statistics.EntityStatistics(ESCache.Instance.EntitiesOnGrid)) return;
            ControllerManager.Instance.RemoveController(typeof(DebugLogEntitiesOnGridController));
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