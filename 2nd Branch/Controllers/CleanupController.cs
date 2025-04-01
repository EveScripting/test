/*
 * Created by huehue.
 * User: duketwo
 * Date: 01.05.2017
 * Time: 18:31
 *
 */

extern alias SC;

using System;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.BackgroundTasks;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Controllers
{
    public class CleanupController : BaseController
    {
        #region Constructors

        public CleanupController()
        {
            IgnorePause = true;
            IgnoreModal = true;
            RunBeforeLoggedIn = true;
            IgnoreValidSession = true;
        }

        #endregion Constructors

        #region Methods

        //private string externalIP;

        public override void DoWork()
        {
            try
            {
                if (!DirectEve.Interval(800))
                    return;

                if (DebugConfig.DebugCleanup) Log("CheckWindows: DoWork");

                if (DebugConfig.DebugDisableCleanup)
                    return;

                Cleanup.ProcessState();

                //if (string.IsNullOrEmpty(externalIP))
                //{
                //    externalIP = Cleanup.GetExternalIPAddress();
                //    if (!string.IsNullOrEmpty(externalIP)) Log("Is Windows Firewall Blocking EVE Correctly? Result [" + externalIP + "]");
                //}
            }
            catch (Exception ex)
            {
                Log("Exception [" + ex + "]");
            }
            finally
            {
                if (ESCache.Instance.DirectEve.Session.IsReady)
                {
                    LocalPulse = UTCNowAddMilliseconds(2500, 4000);
                }

                LocalPulse = UTCNowAddMilliseconds(800, 1200);
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