using SharpHook;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpHook.Native;

namespace Anthropic.SDK.ComputerUse.Inputs
{
    public class MouseController
    {
        public static (int virtualX, int virtualY) GetVirtualCoordinates(int screenIndex, int x, int y)
        {
            // Get all screens
            var screens = System.Windows.Forms.Screen.AllScreens;

            // Validate the screen index
            if (screenIndex < 0 || screenIndex >= screens.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(screenIndex), "Invalid screen index.");
            }

            var screen = screens[screenIndex];
            var monitorBounds = screen.Bounds;

            // Validate coordinates
            if (x < 0 || x >= monitorBounds.Width || y < 0 || y >= monitorBounds.Height)
            {
                throw new ArgumentOutOfRangeException("Coordinates are out of bounds for the specified monitor.");
            }

            // Convert to virtual screen coordinates
            int virtualX = (int)(monitorBounds.X + x);
            int virtualY = (int)(monitorBounds.Y + y);

            return (virtualX, virtualY);
        }

        // Method to set the cursor position on a specific monitor
        public static void SetCursorPositionOnMonitor(int monitorIndex, int x, int y)
        {
            var (virtualX, virtualY) = GetVirtualCoordinates(monitorIndex, x, y);

            IEventSimulator simulator = new EventSimulator();

            // Move the cursor to the specified position
            simulator.SimulateMouseMovement(Convert.ToInt16(virtualX), Convert.ToInt16(virtualY));
        }

        // Method to perform a left-click at the current cursor position
        public static void LeftClick()
        {
            IEventSimulator simulator = new EventSimulator();
            simulator.SimulateMousePress(MouseButton.Button1);
            simulator.SimulateMouseRelease(MouseButton.Button1);

        }

        // Method to move the cursor and perform a left-click at specified coordinates on a specific monitor
        public static void ClickAtPositionOnMonitor(int monitorIndex, int x, int y)
        {
            SetCursorPositionOnMonitor(monitorIndex, x, y);
            System.Threading.Thread.Sleep(50); // Optional delay to ensure the cursor has moved
            LeftClick();
        }


    }
}
