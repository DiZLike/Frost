using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace FrostLive.Helpers
{
    public static class ControlHelper
    {
        public static void SetLabelBorderColor(Label label, Color color)
        {
            var borderField = typeof(Label).GetField("borderColor",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (borderField != null)
            {
                borderField.SetValue(label, color);
                label.Invalidate();
            }
        }

        public static Color GetLabelBorderColor(Label label)
        {
            var borderField = typeof(Label).GetField("borderColor",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (borderField != null)
            {
                return (Color)borderField.GetValue(label);
            }
            return Color.Empty;
        }
    }
}