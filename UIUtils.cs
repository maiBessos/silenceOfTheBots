using System.Drawing;
using System.Windows.Forms;
namespace GoE.Utils
{
    public static class UIUtils
    { 
        /// <summary>
        /// refreshes GUI control immediately
        /// </summary>
        /// <param name="c"></param>
        public static void updateControl(Control c)
        {
            c.Invalidate();
            c.Update();
            c.Refresh();
            Application.DoEvents();
        }
        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }
    }
}