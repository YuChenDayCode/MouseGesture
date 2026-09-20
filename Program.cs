namespace MouseGesture;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        // 双开会装两套钩子，右键会被抢两次。
        using var mutex = new Mutex(true, @"Local\MouseGesture.RButton.Singleton", out bool created);
        if (!created)
        {
            MessageBox.Show("鼠标手势已经在运行。", "Mouse Gesture",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new TrayApplicationContext());
    }
}
