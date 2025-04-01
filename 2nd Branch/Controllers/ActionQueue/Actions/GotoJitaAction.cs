using EVESharpCore.Cache;
using EVESharpCore.Logging;
using EVESharpCore.Traveller;
using System;
using EVESharpCore.Questor.States;

namespace EVESharpCore.Controllers.ActionQueue.Actions
{
    public class GotoJitaAction : Base.ActionQueueAction
    {
        #region Constructors

        public GotoJitaAction()
        {
            InitializeAction = new Action(() =>
            {
                Traveler.Destination = null;
                State.CurrentTravelerState = TravelerState.Idle;
            });

            Action = new Action(() =>
            {
                try
                {
                    if (ESCache.Instance.InAbyssalDeadspace)
                    {
                        Log.WriteLine("InAbyssalDeadspace: Not Setting destination to Jita!");
                        return;
                    }

                    if (Traveler.Destination == null)
                    {
                        Log.WriteLine("Setting destination to Jita.");
                        Traveler.Destination = new StationDestination(60003760);
                        Traveler.SetStationDestination(60003760);
                    }

                    if (State.CurrentTravelerState != TravelerState.AtDestination)
                    {
                        try
                        {
                            Log.WriteLine("Traveler process state.");
                            Traveler.ProcessState();
                        }
                        catch (Exception exception)
                        {
                            Log.WriteLine(exception.ToString());
                        }

                        Log.WriteLine("Requeue action.");
                        QueueAction();
                        return;
                    }

                    if (State.CurrentTravelerState == TravelerState.AtDestination)
                    {
                        Log.WriteLine("Traveller at dest.");
                        State.CurrentTravelerState = TravelerState.Idle;
                        Traveler.Destination = null;
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
            });
        }

        #endregion Constructors
    }
}