using EVESharpCore.Cache;
using EVESharpCore.Controllers.ActionQueue.Actions.Base;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Traveller;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using EVESharpCore.Questor.States;

namespace EVESharpCore.Controllers
{
    public partial class UITravellerControllerForm : Form
    {
        #region Fields

        public List<DirectBookmark> SortedBookmarks = new List<DirectBookmark>();
        public UITravellerController uiTravellerController;

        #endregion Fields

        #region Constructors

        public UITravellerControllerForm(UITravellerController uiTravellerController)
        {
            this.uiTravellerController = uiTravellerController;
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        private void BtnTravelToSetWaypoint_Click(object sender, EventArgs e)
        {
            Traveler.Destination = null;
            State.CurrentTravelerState = TravelerState.Idle;
            ControllerManager.Instance.SetPause(true);
            Invoke(new Action(() => { ModifyControls(false); }));

            ActionQueueAction actionQueueAction = null;
            actionQueueAction = new ActionQueueAction(new Action(() =>
            {
                try
                {
                    if (State.CurrentTravelerState != TravelerState.AtDestination)
                    {
                        try
                        {
                            Traveler.TravelToSetWaypoint();
                        }
                        catch (Exception exception)
                        {
                            Log.WriteLine(exception.ToString());
                        }

                        actionQueueAction.QueueAction();
                    }
                    else
                    {
                        State.CurrentTravelerState = TravelerState.Idle;
                        Traveler.Destination = null;
                        Invoke(new Action(() => { ModifyControls(true); }));
                    }
                }
                catch (Exception ex)
                {
                    Log.WriteLine(ex.ToString());
                }
                }
            ), Traveler.BoolRunEveryFrame);

            actionQueueAction.Initialize().QueueAction();
        }

        private void BtnStopAllActions_Click(object sender, EventArgs e)
        {
            ControllerManager.Instance.GetController<ActionQueueController>().RemoveAllActions();
            Traveler.Destination = null;
            State.CurrentTravelerState = TravelerState.Idle;
            ModifyControls(true);
        }

        private void ModifyControls(bool enabled = false)
        {
            Invoke(new Action(() =>
            {
                foreach (object b in panel1.Controls)
                {
                    if (b is Button button && button != btnStopAllActions)
                        button.Enabled = enabled;
                    if (b is ComboBox cb)
                        cb.Enabled = enabled;
                }
            }));
        }

        #endregion Methods
    }
}