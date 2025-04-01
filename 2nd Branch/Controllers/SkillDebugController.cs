/*
 * Created by huehue.
 * User: duketwo
 * Date: 01.05.2017
 * Time: 18:31
 *
 */

extern alias SC;
using System;
using EVESharpCore.Controllers.Base;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Controllers
{
    public class SkillDebugController : BaseController
    {
        #region Constructors

        public SkillDebugController()
        {
            IgnorePause = false;
            IgnoreModal = false;
            Form = new SkillDebugControllerForm(this);
        }

        #endregion Constructors

        #region Methods

        public override void DoWork()
        {
            try
            {
                Log("SkillDebugController pulse.");
                IsPaused = true;
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