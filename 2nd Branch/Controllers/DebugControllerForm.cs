extern alias SC;

using EVESharpCore.Cache;
using EVESharpCore.Controllers.Debug;
using EVESharpCore.Questor.BackgroundTasks;
using EVESharpCore.Traveller;
using SC::SharedComponents.Utility;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace EVESharpCore.Controllers
{
    public partial class DebugControllerForm : Form
    {
        #region Fields

        private DebugController _debugController;

        #endregion Fields

        #region Constructors

        public DebugControllerForm(DebugController debugController)
        {
            _debugController = debugController;
            InitializeComponent();
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

        private void ButtonStopMyShip_Click(object sender, EventArgs e)
        {
            Logging.Log.WriteLine("Button: StopMyShip");
            NavigateOnGrid.StopMyShip("Button: StopMyShip");
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
            //ButtonApproachPointInSpaceUp_Click
        }

        private void ButtonCloseEVE_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                return Invoke(new Action(() =>
                {
                    Logging.Log.WriteLine("Button: CloseEVE);");
                    ESCache.Instance.CloseEveReason = "Button: CloseEve";
                    ESCache.Instance.BoolCloseEve = true;
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

        private void buttonExitStation_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                return Invoke(new Action(() =>
                {
                    Logging.Log.WriteLine("Button: Exit Station);");
                    TravelerDestination.Undock();
                }));
            });
        }

        private void button8_Click(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                return Invoke(new Action(() =>
                {
                    Logging.Log.WriteLine("Button: RestartEVE);");
                    ESCache.Instance.CloseEveReason = "Button: RestartEve";
                    ESCache.Instance.BoolRestartEve = true;
                }));
            });
        }
    }
}