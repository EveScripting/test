using EVESharpCore.Cache;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Stats;

//using Action = EVESharpCore.Questor.Actions.Base.Action;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void LogWhatIsOnGridAction(DirectAgentMission myMission, DirectAgent myAgent)
        {
            Log.WriteLine("Log Entities on Grid.");
            if (!Statistics.EntityStatistics(ESCache.Instance.EntitiesOnGrid)) return;
            NextAction(myMission, myAgent, true);
        }

        #endregion Methods
    }
}