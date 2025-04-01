using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Actions.Base;
using System.Collections.Generic;
using System.Linq;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void EcmAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            List<string> targetNames = action.GetParameterValues("target");

            // No parameter? Ignore ecm action
            if (!targetNames.Any())
            {
                Log.WriteLine("No targets defined in Ecm action!");
                NextAction(myMission, myAgent, true);
                return;
            }

            Combat.Combat.AddTargetsToEcmByName(targetNames.FirstOrDefault());

            //
            // this action is passive and only adds things to the targets to ecm list
            //
            NextAction(myMission, myAgent, true);
        }

        #endregion Methods
    }
}