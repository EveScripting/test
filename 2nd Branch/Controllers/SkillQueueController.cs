extern alias SC;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Lookup;
using SC::SharedComponents.IPC;
using System;
using System.Linq;

namespace EVESharpCore.Controllers
{
    public class SkillQueueController : BaseController
    {
        #region Constructors

        public SkillQueueController()
        {
            AllowRunInSpace = false;
            AllowRunInStation = true;
            IgnorePause = false;
            IgnoreModal = false;
        }

        #endregion Constructors

        #region Methods

        public static void ProcessState()
        {
            try
            {
                if (ESCache.Instance.InAbyssalDeadspace)
                    return;

                // skill queue (note: this only fills the queue, it does not inject new skills or buy new skills, that needs to be done elsewhere!)
                SkillQueue.ProcessState();
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
                if (DateTime.UtcNow < Time.Instance.LastJumpAction.AddSeconds(7))
                {
                    if (DebugConfig.DebugSkillQueue) Log("SkillQueueController: We recently attempted to jump to another star system, waiting a few seconds before trying to do anything.");
                    return;
                }

                if (ESCache.Instance.InSpace && !ESCache.Instance.InStation)
                    if (ESCache.Instance.Stargates.Count > 0)
                        if (ESCache.Instance.ClosestStargate != null && ESCache.Instance.ClosestStargate.IsOnGridWithMe && ESCache.Instance.ClosestStargate.Distance < 10000)
                        {
                            if (DebugConfig.DebugCleanup) Log("CheckModalWindows: We are within 10k of a stargate, do nothing while we wait to jump.");
                            return;
                        }

                if (!Settings.Instance.DefaultSettingsLoaded)
                    Settings.Instance.LoadSettings_Initialize();

                ProcessState();
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
            finally
            {
                LocalPulse = UTCNowAddSeconds(2, 4);
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