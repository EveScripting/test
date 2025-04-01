using System;
using EVESharpCore.Cache;
using EVESharpCore.Lookup;
using EVESharpCore.Questor.States;

namespace EVESharpCore.Questor.BackgroundTasks
{
    public class LocalWatch
    {
        #region Fields

        private DateTime _lastAction;

        #endregion Fields

        #region Methods

        public void ProcessState()
        {
            if (!ESCache.Instance.DirectEve.Session.IsReady)
                return;

            switch (State.CurrentLocalWatchState)
            {
                case LocalWatchState.Idle:

                    if (DateTime.UtcNow.Subtract(_lastAction).TotalSeconds < Time.Instance.CheckLocalDelay_seconds)
                        break;

                    State.CurrentLocalWatchState = LocalWatchState.CheckLocal;
                    break;

                case LocalWatchState.CheckLocal:

                    ESCache.Instance.LocalSafe(Settings.Instance.LocalBadStandingPilotsToTolerate, Settings.Instance.LocalBadStandingLevelToConsiderBad);

                    _lastAction = DateTime.UtcNow;
                    State.CurrentLocalWatchState = LocalWatchState.Idle;
                    break;

                default:

                    State.CurrentLocalWatchState = LocalWatchState.Idle;
                    break;
            }
        }

        #endregion Methods
    }
}