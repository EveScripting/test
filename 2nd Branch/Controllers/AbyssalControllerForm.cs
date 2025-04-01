using System.Windows.Forms;

namespace EVESharpCore.Controllers
{
    public partial class AbyssalControllerForm : Form
    {
        #region Fields

        private AbyssalController _controller;

        #endregion Fields

        #region Constructors

        public AbyssalControllerForm(AbyssalController c)
        {
            this._controller = c;
            InitializeComponent();
        }

        #endregion Constructors

        #region Methods

        public DataGridView GetDataGridView1 => this.dataGridView1;

        public Label IskPerHLabel => this.label2;

        private void ModifyButtons(bool enabled = false)
        {
            foreach (var b in Controls)
                if (b is Button)
                    ((Button)b).Enabled = enabled;
        }

        #endregion Methods
    }
}