using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Anthropic.SDK.ComputerUse.ScreenCapture
{
    public interface IScreenCapturer
    {
        byte[] CaptureScreen(int monitorIndex);
    }
}
