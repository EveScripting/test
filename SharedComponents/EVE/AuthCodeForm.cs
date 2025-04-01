using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharedComponents.EVE
{
    public partial class AuthCodeForm : Form
    {
        public AuthCodeForm()
        {
            InitializeComponent();
        }
        public string Challenge => textBox1.Text;

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void AccountChallengeForm_Load(object sender, EventArgs e)
        {

        }
    }
}
