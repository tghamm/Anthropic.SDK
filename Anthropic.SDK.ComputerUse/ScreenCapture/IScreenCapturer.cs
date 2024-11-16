namespace Anthropic.SDK.ComputerUse.ScreenCapture
{
    public interface IScreenCapturer
    {
        byte[] CaptureScreen(int monitorIndex);
    }
}
