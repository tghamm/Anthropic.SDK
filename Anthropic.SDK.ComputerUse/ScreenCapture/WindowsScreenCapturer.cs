﻿using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace Anthropic.SDK.ComputerUse.ScreenCapture
{
    public class WindowsScreenCapturer : IScreenCapturer
    {
        // Define RECT structure
        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        // Define MONITORINFO structure
        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public uint cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        // Delegate for monitor enumeration callback
        private delegate bool MonitorEnumDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

        // P/Invoke declarations
        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
           MonitorEnumDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateDC(string lpszDriver, string lpszDevice,
           string lpszOutput, IntPtr lpInitData);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth,
           int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, CopyPixelOperation dwRop);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        // Class to hold monitor information
        public class MonitorInfo
        {
            public IntPtr MonitorHandle;
            public RECT MonitorArea;
        }

        // Method to get all connected monitors
        public static List<MonitorInfo> GetMonitors()
        {
            var monitors = new List<MonitorInfo>();

            bool Callback(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData)
            {
                var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO)) };
                if (GetMonitorInfo(hMonitor, ref mi))
                {
                    monitors.Add(new MonitorInfo
                    {
                        MonitorHandle = hMonitor,
                        MonitorArea = mi.rcMonitor
                    });
                }
                return true; // Continue enumeration
            }

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, Callback, IntPtr.Zero);
            return monitors;
        }

        public (int x, int y) GetScreenSize(int screenIndex)
        {
            // Get all monitors
            var monitors = GetMonitors();

            // Validate the screen index
            if (screenIndex < 0 || screenIndex >= monitors.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(screenIndex), "Invalid screen index.");
            }

            var monitor = monitors[screenIndex];
            return (monitor.MonitorArea.Right - monitor.MonitorArea.Left, monitor.MonitorArea.Bottom - monitor.MonitorArea.Top);
        }

        // Method to capture a specific monitor
        public static Bitmap CaptureMonitor(MonitorInfo monitor)
        {
            var width = monitor.MonitorArea.Right - monitor.MonitorArea.Left;
            var height = monitor.MonitorArea.Bottom - monitor.MonitorArea.Top;

            var hdcSrc = CreateDC("DISPLAY", null, null, IntPtr.Zero);
            var hdcDest = CreateCompatibleDC(hdcSrc);
            var hBitmap = CreateCompatibleBitmap(hdcSrc, width, height);
            var hOld = SelectObject(hdcDest, hBitmap);

            var success = BitBlt(hdcDest, 0, 0, width, height, hdcSrc,
                monitor.MonitorArea.Left, monitor.MonitorArea.Top, CopyPixelOperation.SourceCopy | CopyPixelOperation.CaptureBlt);

            Bitmap bmp = null;

            if (success)
            {
                bmp = Image.FromHbitmap(hBitmap);
            }

            // Clean up
            SelectObject(hdcDest, hOld);
            DeleteObject(hBitmap);
            DeleteDC(hdcDest);
            DeleteDC(hdcSrc);

            return bmp;
        }

        public byte[] CaptureScreen(int screenIndex)
        {
            // Get all monitors
            var monitors = GetMonitors();

            // Validate the screen index
            if (screenIndex < 0 || screenIndex >= monitors.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(screenIndex), "Invalid screen index.");
            }

            var monitor = monitors[screenIndex];
            using var bitmap = CaptureMonitor(monitor);

            using var ms = new System.IO.MemoryStream();
            bitmap.Save(ms, ImageFormat.Jpeg);

            return ms.ToArray();
        }
    }
}