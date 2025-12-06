using System.Diagnostics;
using System.Runtime.InteropServices;

namespace statictime;

public partial class MainWindow
{
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    private const int WH_MOUSE_LL = 14;
    private const int WM_MOUSEWHEEL = 0x020A;
    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]

    private static extern IntPtr GetModuleHandle(string lpModuleName);
    private const int VK_LBUTTON = 0x01;
    private const int VK_RBUTTON = 0x02;
    private const int VK_MENU = 0x12; 
    private const int VK_TAB = 0x09;

    private IntPtr set_hook(LowLevelMouseProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule!)
        {
            return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName!), 0);
        }
    }

    private IntPtr hook_callback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_MOUSEWHEEL)
        {
            MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
            short delta = (short)((hookStruct.mouseData >> 16) & 0xFFFF);
            if (delta != 0)
            {
                _tracker.data.total_scroll += 5;
            }
        }
        return CallNextHookEx(_hook_id, nCode, wParam, lParam);
    }

    private async Task input_loop(CancellationToken token)
    {
        bool was_lmb = false;
        bool was_rmb = false;
        bool alt_tab_triggered = false;
        while (!token.IsCancellationRequested)
        {
            bool is_lmb = (GetAsyncKeyState(VK_LBUTTON) & 0x8000) != 0;
            if (is_lmb && !was_lmb) _tracker.data.total_lmb++;
            was_lmb = is_lmb;
            bool is_rmb = (GetAsyncKeyState(VK_RBUTTON) & 0x8000) != 0;
            if (is_rmb && !was_rmb) _tracker.data.total_rmb++;
            was_rmb = is_rmb;
            bool is_alt = (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;
            bool is_tab = (GetAsyncKeyState(VK_TAB) & 0x8000) != 0;
            if (is_alt && is_tab)
            {
                if (!alt_tab_triggered)
                {
                    _tracker.data.total_altt++;
                    alt_tab_triggered = true;
                }
            }
            else if (!is_alt)
            {
                alt_tab_triggered = false;
            }
            await Task.Delay(50);
        }
    }
}
