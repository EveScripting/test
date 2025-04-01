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
    public partial class UINavigateOnGridControllerForm : Form
    {
        #region Fields

        public List<EntityCache> EntitiesOnGrid = new List<EntityCache>();
        public UINavigateOnGridController _navigateOnGridController;

        #endregion Fields

        #region Constructors

        public UINavigateOnGridControllerForm(UINavigateOnGridController navigateOnGridController)
        {
            this._navigateOnGridController = navigateOnGridController;
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        private void BtnRefreshEntitiesOngrid_Click(object sender, EventArgs e)
        {
            btnRefreshEntitiesOngrid.Enabled = false;
            new ActionQueueAction(new Action(() =>
                {
                    Task.Run(() =>
                {
                    try
                    {
                        Invoke(new Action(() =>
                        {
                            try
                            {
                                EntitiesOnGrid = ESCache.Instance.Entities.Where(x => x.IsPotentialCombatTarget || x.IsStargate || x.IsStation || x.IsAccelerationGate).OrderBy(i => i.Name).ToList();
                                comboBox1.DataSource = EntitiesOnGrid;
                                comboBox1.DisplayMember = "Name";
                            }
                            catch (Exception exception)
                            {
                                Console.WriteLine(exception);
                            }
                        }));
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(ex.ToString());
                    }
                    });
                })).Initialize()
                .QueueAction();
        }

        private void BtnTravelToSetWaypoint_Click(object sender, EventArgs e)
        {
            Traveler.Destination = null;
            State.CurrentTravelerState = TravelerState.Idle;
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
            ));

            actionQueueAction.Initialize().QueueAction();
        }

        private void BtnTravelToBookmark_Click(object sender, EventArgs e)
        {
            DirectBookmark dest = comboBox1.SelectedItem as DirectBookmark;
            Traveler.Destination = null;
            State.CurrentTravelerState = TravelerState.Idle;

            if (dest != null)
            {
                Invoke(new Action(() => { ModifyControls(); }));

                ActionQueueAction actionQueueAction = null;
                actionQueueAction = new ActionQueueAction(new Action(() =>
                {
                    try
                    {
                        if (State.CurrentTravelerState != TravelerState.AtDestination)
                        {
                            try
                            {
                                Traveler.TravelToBookmark(dest);
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
                ));

                actionQueueAction.Initialize().QueueAction();
            }
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