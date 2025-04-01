
extern alias SC;

using EVESharpCore.Controllers.Base;
using SC::SharedComponents.IPC;

namespace EVESharpCore.Controllers
{
    /// <summary>
    ///     Description of ProbeScanController.
    /// </summary>
    public class ProbeScanController : BaseController
    {
        #region Constructors

        public ProbeScanController()
        {
            IgnorePause = false;
            IgnoreModal = true;
            Form = new ProbeScanControllerForm(this);
        }

        #endregion Constructors

        #region Methods

        public override void DoWork()
        {
            IsPaused = true;
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