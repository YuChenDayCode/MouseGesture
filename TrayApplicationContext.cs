namespace MouseGesture;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly MouseGestureHook _hook;
    private readonly NotifyIcon _tray;
    private readonly Icon _icon;

    public TrayApplicationContext()
    {
        var enable = new ToolStripMenuItem("启用手势") { Checked = true, CheckOnClick = true };
        var menu = new ContextMenuStrip();
        menu.Items.Add(enable);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(new ToolStripMenuItem("右键按住上滑  →  Ctrl+Home") { Enabled = false });
        menu.Items.Add(new ToolStripMenuItem("右键按住下滑  →  Ctrl+End") { Enabled = false });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("退出", null, (_, _) => ExitThread());

        // 第一个 Control 会装上 WinForms 同步上下文，钩子回调才能 Post 回 UI 线程。
        var sync = SynchronizationContext.Current
            ?? throw new InvalidOperationException("缺少 WindowsFormsSynchronizationContext");

        _hook = new MouseGestureHook(sync);
        enable.CheckedChanged += (_, _) => _hook.Enabled = enable.Checked;
        _hook.Install();

        _icon = LoadIcon();
        _tray = new NotifyIcon
        {
            Icon = _icon,
            Visible = true,
            Text = "鼠标手势：按住右键上滑 Ctrl+Home，下滑 Ctrl+End",
            ContextMenuStrip = menu
        };
        _tray.DoubleClick += (_, _) => enable.PerformClick();
    }

    private static Icon LoadIcon()
    {
        var path = Environment.ProcessPath ?? Application.ExecutablePath;
        return Icon.ExtractAssociatedIcon(path) ?? (Icon)SystemIcons.Application.Clone();
    }

    protected override void ExitThreadCore()
    {
        _hook.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _icon.Dispose();
        base.ExitThreadCore();
    }
}
