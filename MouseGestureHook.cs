using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace MouseGesture;

internal sealed class MouseGestureHook : IDisposable
{
    public bool Enabled { get; set; } = true;
    public int ThresholdPixels { get; set; } = 64;

    private static MouseGestureHook? s_instance;
    private readonly SynchronizationContext _ui;
    private IntPtr _hook;
    private bool _down;
    private int _x0, _y0;

    public MouseGestureHook(SynchronizationContext ui)
    {
        _ui = ui;
        s_instance = this;
    }

    public unsafe void Install()
    {
        if (_hook != IntPtr.Zero) return;
        // x64 上 INPUT 必须是 40 字节，对不齐 SendInput 会乱发键。
        if (sizeof(INPUT) != 40)
            throw new InvalidOperationException($"INPUT 大小 {sizeof(INPUT)}，期望 40");

        _hook = Native.SetWindowsHookEx(Native.WH_MOUSE_LL, &OnHook, Native.GetModuleHandle(null), 0);
        if (_hook == IntPtr.Zero)
            throw new Win32Exception(Marshal.GetLastPInvokeError(), "安装鼠标钩子失败");
    }

    // 钩子回调必须是静态非托管函数；实例状态走 s_instance。
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static IntPtr OnHook(int nCode, IntPtr wParam, IntPtr lParam)
    {
        var self = s_instance;
        if (self is null) return Native.CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
        try { return self.Handle(nCode, wParam, lParam); }
        catch { return Native.CallNextHookEx(self._hook, nCode, wParam, lParam); }
    }

    private unsafe IntPtr Handle(int nCode, IntPtr wParam, IntPtr lParam)
    {
        IntPtr Pass() => Native.CallNextHookEx(_hook, nCode, wParam, lParam);
        if (nCode < 0) return Pass();

        var m = *(MSLLHOOKSTRUCT*)lParam;
        if (Native.IsInjected(in m) || !Enabled) return Pass();

        switch (wParam.ToInt32())
        {
            case Native.WM_RBUTTONDOWN:
                _down = true;
                _x0 = m.pt.X;
                _y0 = m.pt.Y;
                return 1; // 先吞掉按下；松手再决定是单击还是手势

            case Native.WM_RBUTTONUP:
            case Native.WM_RBUTTONDBLCLK:
                if (!_down) return Pass();
                _down = false;
                Recognize(m.pt.X, m.pt.Y);
                return 1;

            default:
                return Pass();
        }
    }

    private void Recognize(int x, int y)
    {
        int dx = x - _x0, dy = y - _y0, t = ThresholdPixels;
        // 位移不够：回放普通右键。钩子里不能直接 SendInput，Post 到 UI 线程。
        if (dx * dx + dy * dy < t * t)
        {
            _ui.Post(_ => Native.RightClick(), null);
            return;
        }
        if (Math.Abs(dy) <= Math.Abs(dx)) return; // 左右滑：不触发、也不弹菜单
        _ui.Post(_ => { if (dy > 0) Native.CtrlEnd(); else Native.CtrlHome(); }, null);
    }

    public void Dispose()
    {
        if (ReferenceEquals(s_instance, this)) s_instance = null;
        if (_hook == IntPtr.Zero) return;
        Native.UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
    }
}
