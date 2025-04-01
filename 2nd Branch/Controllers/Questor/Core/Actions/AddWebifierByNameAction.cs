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

        private static void AddWebifierByNameAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            bool notTheClosest;
            if (!bool.TryParse(action.GetParameterValue("notclosest"), out notTheClosest))
                notTheClosest = false;

            int numberToIgnore;
            if (!int.TryParse(action.GetParameterValue("numbertoignore"), out numberToIgnore))
                numberToIgnore = 0;

            List<string> targetNames = action.GetParameterValues("target");

            // No parameter? Ignore kill action
            if (!targetNames.Any())
            {
                Log.WriteLine("No targets defined in AddWebifierByName action!");
                NextAction(myMission, myAgent, true);
                return;
            }

            Combat.Combat.AddWebifierByName(targetNames.FirstOrDefault(), numberToIgnore, notTheClosest);

            //
            // this action is passive and only adds things to the WarpScramblers list )before they have a chance to scramble you, so you can target them early
            //
            NextAction(myMission, myAgent, true);
        }

        #endregion Methods
    }
}