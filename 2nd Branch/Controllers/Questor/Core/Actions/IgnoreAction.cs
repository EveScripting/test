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

        private static void IgnoreAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            bool clear;
            if (!bool.TryParse(action.GetParameterValue("clear"), out clear))
                clear = false;

            List<string> add = action.GetParameterValues("add");
            List<string> remove = action.GetParameterValues("remove");

            if (clear)
            {
                IgnoreTargets.Clear();
            }
            else
            {
                add.ForEach(a => IgnoreTargets.Add(a.Trim()));
                remove.ForEach(a => IgnoreTargets.Remove(a.Trim()));
            }
            Log.WriteLine("Updated ignore list");
            if (IgnoreTargets.Count > 0)
                Log.WriteLine("Currently ignoring: " + IgnoreTargets.Aggregate((current, next) => "[" + current + "][" + next + "]"));
            else
                Log.WriteLine("Your ignore list is empty");

            NextAction(myMission, myAgent, true);
        }

        #endregion Methods
    }
}