using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace SharedComponents.EVEAccountCreator.Curl
{
    public partial class CaptchaResponseForm : Form
    {
        private Byte[] ImgBytes;

        public CaptchaResponseForm()
        {
            InitializeComponent();
        }

        public CaptchaResponseForm(Byte[] imgBytes)
        {
            InitializeComponent();
            ImgBytes = imgBytes;
        }

        public String GetCaptchaResponse => textBox1.Text;

        private void button1_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void CaptchaResponseForm_Shown(object sender, EventArgs e)
        {
            try
            {
                pictureBox1.Image = (Bitmap) new ImageConverter().ConvertFrom(ImgBytes);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }
    }
}