using System.Windows.Forms;

namespace SharedComponents.Controls
{
    public partial class ListViewItemForm : Form
    {
        public ListViewItemForm(string text)
        {
            InitializeComponent();
            textBox1.Text = text;
        }
    }
}