namespace Anthropic.SDK.ComputerUse.ScreenCapture
{
    public interface IScreenCapturer
    {
        byte[] CaptureScreen(int monitorIndex);

        public (int x, int y) GetScreenSize(int screenIndex);
    }
}