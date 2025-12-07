using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace statictime;

public partial class MainWindow
{
    private delegate IntPtr LowLevelProc(int nCode, IntPtr wParam, IntPtr lParam);
    private const int WH_MOUSE_LL = 14;
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_MOUSEWHEEL = 0x020A;
    private const int WM_KEYDOWN = 0x0100;

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
    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }
    [DllImport("user32.dll")]
    public static extern short GetAsyncKeyState(int vKey);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
    
    [DllImport("user32.dll")]
    private static extern int ToUnicode(uint wVirtKey, uint wScanCode, byte[] lpKeyState, [Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pwszBuff, int cchBuff, uint wFlags);

    [DllImport("user32.dll")]
    private static extern bool GetKeyboardState(byte[] lpKeyState);
    
    private const int VK_LBUTTON = 0x01;
    private const int VK_RBUTTON = 0x02;
    private const int VK_MENU = 0x12; 
    private const int VK_TAB = 0x09;
    private StringBuilder _word_buffer = new StringBuilder();
    private IntPtr set_mouse_hook(LowLevelProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule!)
        {
            return SetWindowsHookEx(WH_MOUSE_LL, proc, GetModuleHandle(curModule.ModuleName!), 0);
        }
    }
    
    private IntPtr set_kb_hook(LowLevelProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule!)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName!), 0);
        }
    }

    private IntPtr mouse_hook_callback(int nCode, IntPtr wParam, IntPtr lParam)
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
        return CallNextHookEx(_mouse_hook_id, nCode, wParam, lParam);
    }
    
    private IntPtr kb_hook_callback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            KBDLLHOOKSTRUCT kb = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
            if (kb.vkCode == 0x20 || kb.vkCode == 0x0D) // Space or Enter
            {
                if (_word_buffer.Length > 0)
                {
                    _tracker.add_word(_word_buffer.ToString());
                    _word_buffer.Clear();
                }
            }
            else
            {
                byte[] keys = new byte[256];
                if (GetKeyboardState(keys))
                {
                    StringBuilder buf = new StringBuilder(2);
                    if (ToUnicode(kb.vkCode, kb.scanCode, keys, buf, buf.Capacity, 0) > 0)
                    {
                        char c = buf[0];
                        if (char.IsLetterOrDigit(c) || c == '-')
                        {
                            _word_buffer.Append(c);
                        }
                        else if (_word_buffer.Length > 0)
                        {
                             _tracker.add_word(_word_buffer.ToString());
                             _word_buffer.Clear();
                        }
                    }
                }
            }
        }
        return CallNextHookEx(_kb_hook_id, nCode, wParam, lParam);
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
