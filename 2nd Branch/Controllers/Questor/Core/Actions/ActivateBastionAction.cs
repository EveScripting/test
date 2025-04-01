using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Lookup;
using System;
using System.Collections.Generic;
using System.Linq;
using EVESharpCore.Framework;
using Action = EVESharpCore.Questor.Actions.Base.Action;

namespace EVESharpCore.Questor.Actions
{
    public partial class ActionControl
    {
        #region Methods

        private static void ActivateBastionAction(Action action, DirectAgentMission myMission, DirectAgent myAgent)
        {
            bool _done = false;

            if (ESCache.Instance.Modules.Count > 0)
            {
                if (ESCache.Instance.ActiveShip != null && ESCache.Instance.ActiveShip.IsImmobile)
                    _done = true;
            }
            else
            {
                Log.WriteLine("no bastion modules fitted!");
                _done = true;
            }

            if (_done)
            {
                Log.WriteLine("ActivateBastion Action completed.");

                // Nothing has targeted us in the specified timeout
                _waiting = false;
                NextAction(myMission, myAgent, true);
                return;
            }

            // Default timeout is 60 seconds
            int DeactivateAfterSeconds;
            if (!int.TryParse(action.GetParameterValue("DeactivateAfterSeconds"), out DeactivateAfterSeconds))
                DeactivateAfterSeconds = 5;

            DeactivateIfNothingTargetedWithinRange = false;
            if (!bool.TryParse(action.GetParameterValue("DeactivateIfNothingTargetedWithinRange"), out DeactivateIfNothingTargetedWithinRange))
                DeactivateIfNothingTargetedWithinRange = false;

            // Start bastion mode
            Combat.Combat.ActivateBastion(DateTime.UtcNow.AddSeconds(DeactivateAfterSeconds), true);
        }

        #endregion Methods
    }
}