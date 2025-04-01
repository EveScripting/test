using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SharedComponents.Utility
{
    public static class FormUtil
    {
        public static Color Color { get; set; }
        public static Font Font { get; set; }

        public static void DrawItem(object sender, DrawListViewItemEventArgs e)
        {
            if (Color == null || Font == null)
                return;

            e.Graphics.SetClip(e.Bounds);
            using (var br = new SolidBrush(Color))
            {
                e.Graphics.FillRectangle(br, e.Bounds);
            }

            var timeColor = ColorTranslator.FromHtml("#666666");
            var callingMethodColor = ColorTranslator.FromHtml("#0071BC");

            var textLeft = e.Bounds.Left;
            var subItems = e.Item.Text.Split(']');
            for (var i = 0; i < subItems.Length; ++i)
            {
                if (i != subItems.Length - 1)
                    subItems[i] += "]";
                var textWidth = TextRenderer.MeasureText(subItems[i], Font).Width;
                TextRenderer.DrawText(e.Graphics, subItems[i], Font,
                    new Rectangle(textLeft, e.Bounds.Top, textWidth, e.Bounds.Height),
                    i == 0 ? timeColor : i == 1 ? callingMethodColor : e.Item.ForeColor,
                    Color.Empty,
                    TextFormatFlags.VerticalCenter | TextFormatFlags.PreserveGraphicsClipping);
                textLeft += textWidth;
            }
            e.Graphics.ResetClip();
        }

        public static void SetDoubleBuffered(Control control)
        {
            typeof(Control).InvokeMember("DoubleBuffered",
                BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
                null, control, new object[] { true });
        }
    }
}
