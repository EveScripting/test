using System.Windows.Forms;

namespace EVESharpCore.Controllers
{
    public partial class PinataControllerForm : Form
    {
        #region Fields

        private PinataController _pinataController;

        #endregion Fields

        #region Constructors

        public PinataControllerForm(PinataController pinataController)
        {
            _pinataController = pinataController;
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        public DataGridView GetDataGridView1 { get; private set; }

        public DataGridView GetDataGridView2 { get; private set; }

        private void ModifyButtons(bool enabled = false)
        {
            foreach (object b in Controls)
                if (b is Button)
                    ((Button) b).Enabled = enabled;
        }

        #endregion Methods
    }
}