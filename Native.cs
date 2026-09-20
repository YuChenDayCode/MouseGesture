using System.Runtime.InteropServices;

namespace MouseGesture;

internal static unsafe partial class Native
{
    public const int WH_MOUSE_LL = 14;
    public const int WM_RBUTTONDOWN = 0x0204;
    public const int WM_RBUTTONUP = 0x0205;
    public const int WM_RBUTTONDBLCLK = 0x0206;

    public const uint LLMHF_INJECTED = 0x01;
    public const uint LLMHF_LOWER_IL_INJECTED = 0x02;

    // 打在 SendInput 上，钩子见到就放行，避免回放右键再次被吞。
    public static readonly UIntPtr ExtraInfo = (UIntPtr)0x6D673031;

    public static bool IsInjected(in MSLLHOOKSTRUCT m)
        => m.dwExtraInfo == ExtraInfo
           || (m.flags & (LLMHF_INJECTED | LLMHF_LOWER_IL_INJECTED)) != 0;

    public static void CtrlHome() => Chord(vk: 0x24); // VK_HOME
    public static void CtrlEnd() => Chord(vk: 0x23);  // VK_END
    public static void RightClick() => Send(Mouse(0x0008), Mouse(0x0010)); // RIGHTDOWN / RIGHTUP

    private static void Chord(ushort vk)
        => Send(Key(0x11, up: false), Key(vk, up: false), Key(vk, up: true), Key(0x11, up: true)); // VK_CONTROL

    private static void Send(params INPUT[] inputs)
    {
        fixed (INPUT* p = inputs)
            SendInput((uint)inputs.Length, p, sizeof(INPUT));
    }

    private static INPUT Key(ushort vk, bool up) => new()
    {
        type = 1,
        U = { ki = new KEYBDINPUT { wVk = vk, dwFlags = up ? 0x0002u : 0, dwExtraInfo = ExtraInfo } }
    };

    private static INPUT Mouse(uint flags) => new()
    {
        type = 0,
        U = { mi = new MOUSEINPUT { dwFlags = flags, dwExtraInfo = ExtraInfo } }
    };

    [LibraryImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
    public static partial IntPtr SetWindowsHookEx(
        int idHook, delegate* unmanaged[Stdcall]<int, IntPtr, IntPtr, IntPtr> lpfn, IntPtr hMod, uint dwThreadId);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool UnhookWindowsHookEx(IntPtr hhk);

    [LibraryImport("user32.dll")]
    public static partial IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    public static partial IntPtr GetModuleHandle(string? lpModuleName);

    [LibraryImport("user32.dll", SetLastError = true)]
    public static partial uint SendInput(uint nInputs, INPUT* pInputs, int cbSize);
}

[StructLayout(LayoutKind.Sequential)]
internal struct POINT { public int X, Y; }

[StructLayout(LayoutKind.Sequential)]
internal struct MSLLHOOKSTRUCT
{
    public POINT pt;
    public uint mouseData, flags, time;
    public UIntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct INPUT
{
    public uint type;
    public InputUnion U;
}

[StructLayout(LayoutKind.Explicit)]
internal struct InputUnion
{
    [FieldOffset(0)] public MOUSEINPUT mi;
    [FieldOffset(0)] public KEYBDINPUT ki;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MOUSEINPUT
{
    public int dx, dy;
    public uint mouseData, dwFlags, time;
    public UIntPtr dwExtraInfo;
}

[StructLayout(LayoutKind.Sequential)]
internal struct KEYBDINPUT
{
    public ushort wVk, wScan;
    public uint dwFlags, time;
    public UIntPtr dwExtraInfo;
}
