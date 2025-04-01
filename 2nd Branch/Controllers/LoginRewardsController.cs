/*
 * Created by huehue.
 * User: duketwo
 * Date: 01.05.2017
 * Time: 18:31
 *
 */

extern alias SC;

using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Cache;
using EVESharpCore.Controllers.Base;
using EVESharpCore.Framework;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Questor.Behaviors;
using EVESharpCore.Questor.Combat;
using EVESharpCore.Questor.Stats;
using EVESharpCore.Questor.States;
using SC::SharedComponents.EVE;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Controllers
{
    public class LoginRewardsController : BaseController
    {
        #region Constructors

        public LoginRewardsController()
        {
            IgnorePause = false;
            IgnoreModal = true;
        }

        #endregion Constructors

        #region Fields

        private DateTime NextLoginRewardsControllerTimeStamp = DateTime.UtcNow;
        #endregion Fields

        #region Methods

        public void CheckWindows()
        {
            if (DateTime.UtcNow < NextLoginRewardsControllerTimeStamp)
                return;

            NextLoginRewardsControllerTimeStamp = DateTime.UtcNow.AddSeconds(3);

            if (Time.Instance.LastJumpAction.AddSeconds(5) > DateTime.UtcNow)
            {
                //if (DebugConfig.DebugCleanup) Log.WriteLine("if (DateTime.UtcNow > Time.Instance.LastJumpAction.AddSeconds(5))");
                return;
            }

            if (Time.Instance.LastUndockAction.AddSeconds(5) > DateTime.UtcNow)
            {
                //if (DebugConfig.DebugCleanup) Log.WriteLine("if (DateTime.UtcNow > Time.Instance.LastUndockAction.AddSeconds(5))");
                return;
            }

            if (Time.Instance.LastDockAction.AddSeconds(5) > DateTime.UtcNow)
            {
                //if (DebugConfig.DebugCleanup) Log.WriteLine("if (DateTime.UtcNow > Time.Instance.LastDockAction.AddSeconds(5))");
                return;
            }

            if (ESCache.Instance.InSpace) return;
            if (!ESCache.Instance.InStation) return;

            //Log("Checkmodal windows called.");
            if (ESCache.Instance.Windows.Count == 0)
            {
                if (DebugConfig.DebugCleanup) Log("CheckModalWindows: Cache.Instance.Windows returned null or empty");
                return;
            }

            if (ESCache.Instance.Paused) return;

            if (DebugConfig.DebugCleanup) Log("Checking Each window in Cache.Instance.Windows");
            //if (DirectEve.Interval(10000, 20000)) CashInReward();
        }

        public override void DoWork()
        {
            try
            {
                if (DebugConfig.DebugDisableCleanup)
                    return;

                CheckWindows();
                //CheckOmegaClone();
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