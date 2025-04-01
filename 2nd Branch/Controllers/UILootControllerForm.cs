using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EVESharpCore.Framework;
using EVESharpCore.Logging;
using EVESharpCore.Questor.Actions;
using EVESharpCore.Questor.Activities;
using EVESharpCore.Cache;
using EVESharpCore.Questor.States;
using EVESharpCore.Questor.Traveller;
using Action = System.Action;

namespace EVESharpCore.Controllers
{
    public partial class UILootControllerForm : Form
    {
        public UILootController uiLootController;

        public UILootControllerForm(UILootController uiLootController)
        {
            this.uiLootController = uiLootController;
            InitializeComponent();
        }

        private String GetTypeNameForTypeId(int typeId)
        {
            return QCache.Instance.DirectEve.GetInvType(typeId).TypeName;
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            new ActionQueueAction.Actions.Base.ActionQueueAction(new Action(() =>
                {
                    try
                    {
                        Invoke(new Action(() =>
                        {
                            //SortedBookmarks = QCache.Instance.DirectEve.Bookmarks.OrderBy(i => i.Title).ToList();
                            //LowTierCan.DataSource = SortedBookmarks;
                            LowTierCan.DisplayMember = "Title";
                        }));
                    }
                    catch (Exception ex)
                    {
                        Log.WriteLine(ex.ToString());
                    }
                })).Initialize()
                .QueueAction();
        }

        private void button4_Click_1(object sender, EventArgs e)
        {
            var dest = LowTierCan.SelectedItem as DirectBookmark;
            Traveler.Destination = null;
            _States.CurrentTravelerState = TravelerState.Idle;

            if (dest != null)
            {
                Invoke(new Action(() =>
                {
                    button1.Enabled = false;
                    button4.Enabled = false;
                    LowTierCan.Enabled = false;
                }));

                ActionQueueAction.Actions.Base.ActionQueueAction actionQueueAction = null;
                actionQueueAction = new ActionQueueAction.Actions.Base.ActionQueueAction(new Action(() =>
                    {
                        try
                        {
                            if (_States.CurrentTravelerState != TravelerState.AtDestination)
                            {
                                try
                                {
                                    Traveler.TravelToBookmark(dest, "");
                                }
                                catch (Exception exception)
                                {
                                    Log.WriteLine(exception.ToString());
                                }

                                actionQueueAction.QueueAction();
                            }
                            else
                            {
                                _States.CurrentTravelerState = TravelerState.Idle;
                                Traveler.Destination = null;

                                Invoke(new Action(() =>
                                {
                                    button1.Enabled = true;
                                    button4.Enabled = true;
                                    LowTierCan.Enabled = true;
                                }));
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

        private void button5_Click(object sender, EventArgs e)
        {
            ControllerManager.Instance.ActionQueueController.RemoveAllActions();
            button1.Enabled = true;
            button4.Enabled = true;
            LowTierCan.Enabled = true;
        }

        private void LowTierCan_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void buttonRefreshInventory_Click(object sender, EventArgs e)
        {
            UnloadLoot.LootItemsInItemHangar();
        }
    }
}