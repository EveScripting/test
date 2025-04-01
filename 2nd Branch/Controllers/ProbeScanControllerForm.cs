using EVESharpCore.Controllers.Debug;
using System;
using System.Windows.Forms;

namespace EVESharpCore.Controllers
{
    public partial class ProbeScanControllerForm : Form
    {
        #region Fields

        private ProbeScanController _probeScanController;

        #endregion Fields

        #region Constructors

        public ProbeScanControllerForm(ProbeScanController probeScanController)
        {
            _probeScanController = probeScanController;
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        private void Button3_Click(object sender, EventArgs e)
        {
            new DebugScan().Show();
        }

        #endregion Methods
    }
}