extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Debug;
using EVESharpCore.Questor.BackgroundTasks;
using SC::SharedComponents.Utility;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVESharpCore.Controllers
{
    public partial class MiscActionsForm : Form
    {
        #region Fields

        #endregion Fields

        #region Constructors

        public MiscActionsForm()
        {
            //InitializeComponent();
        }

        #endregion Constructors

        private void DebugAssetsButton_Click(object sender, EventArgs e)
        {
            new DebugWindows().Show();
        }

        #region Methods

        private void Button1_Click(object sender, EventArgs e)
        {
            new DebugEntities().Show();
        }

        private void Button2_Click(object sender, EventArgs e)
        {
            new DebugSkills().Show();
        }

        private void Button3_Click(object sender, EventArgs e)
        {
            new DebugScan().Show();
        }

        private void Button4_Click(object sender, EventArgs e)
        {
            new DebugModules().Show();
        }

        private void Button5_Click(object sender, EventArgs e)
        {
            new DebugChannels().Show();
        }

        private void Button6_Click(object sender, EventArgs e)
        {
            new DebugMap().Show();
        }

        private void Button7_Click(object sender, EventArgs e)
        {
            new DebugWindows().Show();
        }

        #endregion Methods

        private void Button8_Click(object sender, EventArgs e)
        {
            //Button8_Click
        }

        private void ButtonStopMyShip_Click(object sender, EventArgs e)
        {
            //Task.Run(() =>
            //{
            //    return Invoke(new Action(() =>
            //    {
                    Logging.Log.WriteLine("Button: StopMyShip");
                    NavigateOnGrid.StopMyShip("Button: StopMyShip");
            //    }));
            //});
        }

        private void ButtonApproachPointInSpaceDown_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                return Invoke(new Action(() =>
                {
                    Logging.Log.WriteLine("Button: PointInSpaceDirectlyDown);");
                    if (ESCache.Instance.MyShipEntity._directEntity.PointInSpaceDirectlyDown != null)
                    {
                        ESCache.Instance.ActiveShip.MoveTo((Vec3)ESCache.Instance.MyShipEntity._directEntity.PointInSpaceDirectlyDown);
                    }
                }));
            });
        }

        private void ButtonApproachPointInSpaceUp_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                return Invoke(new Action(() =>
                {
                    Logging.Log.WriteLine("Button: PointInSpaceDirectlyUp);");
                    if (ESCache.Instance.MyShipEntity._directEntity.PointInSpaceDirectlyUp != null)
                    {
                        ESCache.Instance.ActiveShip.MoveTo((Vec3)ESCache.Instance.MyShipEntity._directEntity.PointInSpaceDirectlyUp);
                    }
                }));
            });
        }

        private void ButtonApproachPointInSpaceNorth_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                return Invoke(new Action(() =>
                {
                    Logging.Log.WriteLine("Button: PointInSpaceDirectlyNorth);");
                    if (ESCache.Instance.MyShipEntity._directEntity.PointInSpaceDirectlyNorth != null)
                    {
                        ESCache.Instance.ActiveShip.MoveTo((Vec3)ESCache.Instance.MyShipEntity._directEntity.PointInSpaceDirectlyNorth);
                    }
                }));
            });
        }

        private void ButtonApproachPointInSpaceSouth_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                return Invoke(new Action(() =>
                {
                    Logging.Log.WriteLine("Button: PointInSpaceDirectlySouth);");
                    if (ESCache.Instance.MyShipEntity._directEntity.PointInSpaceDirectlySouth != null)
                    {
                        ESCache.Instance.ActiveShip.MoveTo((Vec3)ESCache.Instance.MyShipEntity._directEntity.PointInSpaceDirectlySouth);
                    }
                }));
            });
        }

        private void ButtonApproachPointInSpaceEast_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                return Invoke(new Action(() =>
                {
                    Logging.Log.WriteLine("Button: PointInSpaceDirectlyEast);");
                    if (ESCache.Instance.MyShipEntity._directEntity.PointInSpaceDirectlyEast != null)
                    {
                        ESCache.Instance.ActiveShip.MoveTo((Vec3)ESCache.Instance.MyShipEntity._directEntity.PointInSpaceDirectlyEast);
                    }
                }));
            });
        }

        private void ButtonApproachPointInSpaceWest_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                return Invoke(new Action(() =>
                {
                    Logging.Log.WriteLine("Button: PointInSpaceDirectlyWest);");
                    if (ESCache.Instance.MyShipEntity._directEntity.PointInSpaceDirectlyWest != null)
                    {
                        ESCache.Instance.ActiveShip.MoveTo((Vec3)ESCache.Instance.MyShipEntity._directEntity.PointInSpaceDirectlyWest);
                    }
                }));
            });
        }

        private void DebugCreateNewFleet_Click(object sender, EventArgs e)
        {
            if (!ESCache.Instance.DirectEve.Session.InFleet)
                ESCache.Instance.DirectEve.FormNewFleet();
        }

        private void ChkboxSalvagerAllowStealingOfLoot_CheckedChanged(object sender, EventArgs e)
        {
            /// if (chkboxSalvagerAllowStealingOfLoot.Checked)
            ///    Salvage.IgnoreLootRights = true;
            ///
            /// Salvage.IgnoreLootRights = false;
        }

        private void ChkboxSalvagerAllowStealingOfLoot_Click(object sender, EventArgs e)
        {
            //ChkboxSalvagerAllowStealingOfLoot_Click
        }
    }
}