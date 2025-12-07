using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace statictime;

public class tracker
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);
    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);
    [DllImport("shell32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr ExtractIcon(IntPtr hInst, string lpszExeFileName, int nIconIndex);
    [DllImport("user32.dll")]
    private static extern bool DestroyIcon(IntPtr hIcon);
    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }
    public save_data data { get; set; } = new();
    private string save_path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "statictime_data.json");
    private const int afk_threshold_seconds = 60;
    private const string app_name_for_autostart = "statictime";
    public tracker()
    {
        load();
        save();
    }

    public bool is_user_afk()
    {
        var lii = new LASTINPUTINFO();
        lii.cbSize = (uint)Marshal.SizeOf(lii);
        if (GetLastInputInfo(ref lii))
        {
            var idle_time = (uint)Environment.TickCount - lii.dwTime;
            return (idle_time / 1000) > afk_threshold_seconds;
        }
        return false;
    }

    public string get_active_proc()
    {
        try
        {
            IntPtr hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return "";
            GetWindowThreadProcessId(hwnd, out uint pid);
            var proc = Process.GetProcessById((int)pid);
            string path = proc.MainModule?.FileName ?? "";
            return path;
        }
        catch
        {
            return "";
        }
    }
    
    public bool is_blacklisted(string path)
    {
        path = path.ToLower().Trim();
        foreach (var blocked in data.blacklist)
        {
            if (path == blocked) return true;
            if (path.EndsWith("\\" + blocked)) return true;
        }
        return false;
    }

    public void add_time(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        path = path.ToLower().Trim(); 
        if (!File.Exists(path)) return;
        if (is_blacklisted(path)) return;
        if (is_user_afk())
        {
            data.total_afk++;
            save();
            return;
        }
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        if (!data.history.ContainsKey(today))
            data.history[today] = new Dictionary<string, long>();
        if (!data.history[today].ContainsKey(path))
            data.history[today][path] = 0;
        data.history[today][path]++;
        save();
    }
    
    public void add_word(string word)
    {
        if (string.IsNullOrWhiteSpace(word) || word.Length < 3) return;
        word = word.ToLower();
        if (!data.word_stats.ContainsKey(word))
            data.word_stats[word] = 0;
        data.word_stats[word]++;
    }

    public void save()
    {
        try
        {
            string json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(save_path, json);
        }
        catch { }
    }

    public void load()
    {
        if (File.Exists(save_path))
        {
            try
            {
                data = JsonSerializer.Deserialize<save_data>(File.ReadAllText(save_path)) ?? new save_data();
            }
            catch 
            {
                data = new save_data();
            }
        }
    }

    private bool has_embedded_icon(string path)
    {
        try
        {
            IntPtr hIcon = ExtractIcon(IntPtr.Zero, path, 0);
            if (hIcon == IntPtr.Zero || hIcon == new IntPtr(1))
            {
                return false;
            }
            DestroyIcon(hIcon);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public ImageSource? get_icon(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;
            if (!has_embedded_icon(path)) return null;
            using var ico = System.Drawing.Icon.ExtractAssociatedIcon(path);
            if (ico == null) return null;
            var image = Imaging.CreateBitmapSourceFromHIcon(
                ico.Handle,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());
            image.Freeze();
            return image;
        }
        catch
        {
            return null;
        }
    }

    public void autostart(bool enable)
    {
        string key = @"Software\Microsoft\Windows\CurrentVersion\Run";
        using var rk = Registry.CurrentUser.OpenSubKey(key, true);
        if (enable)
        {
            using var cur = Process.GetCurrentProcess();
            string app_path = cur.MainModule?.FileName ?? System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (app_path.EndsWith(".dll")) app_path = Path.ChangeExtension(app_path, ".exe");
            rk?.SetValue(app_name_for_autostart, $"\"{app_path}\" --silent");
            data.autostart_enabled = true;
        }
        else
        {
            rk?.DeleteValue(app_name_for_autostart, false);
            data.autostart_enabled = false;
        }
    }

    public bool autostart_enabled()
    {
        string key = @"Software\Microsoft\Windows\CurrentVersion\Run";
        using var rk = Registry.CurrentUser.OpenSubKey(key, false);
        return rk?.GetValue(app_name_for_autostart) != null;
    }
    
    public void reset_data()
    {
        data = new save_data();
        save();
    }
}
