

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Anthropic.SDK.ComputerUse.ScreenCapture
{
    public class WindowsScreenCapturer : IScreenCapturer
    {
        public byte[] CaptureScreen(int screenIndex)
        {
            // Get all screens
            var screens = Screen.AllScreens;

            // Validate the screen index
            if (screenIndex < 0 || screenIndex >= screens.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(screenIndex), "Invalid screen index.");
            }

            var screen = screens[screenIndex];
            var bounds = screen.Bounds;

            // Create a bitmap with the screen's dimensions
            var bitmap = new Bitmap(bounds.Width, bounds.Height);

            // Capture the screen
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
            }
            using var ms = new System.IO.MemoryStream();
            bitmap.Save(ms, ImageFormat.Jpeg);

            return ms.ToArray();
        }
    }
}
