using System.Windows.Media;
using System.IO;
using System.Diagnostics;

namespace statictime;

public class app_item
{
    private string _cached_name = "";
    public string path { get; set; } = "";
    public long seconds_played { get; set; }
    public ImageSource? icon { get; set; }
    public bool show_exe { get; set; } = false;
    public bool full_time_format { get; set; } = false;
    public string display_name => resolve_display_name();
    public string time => format_time(seconds_played, full_time_format);
    private string resolve_display_name()
    {
        if (string.IsNullOrEmpty(_cached_name))
        {
            try 
            {
                var info = FileVersionInfo.GetVersionInfo(path);
                if (!string.IsNullOrWhiteSpace(info.FileDescription)) _cached_name = info.FileDescription;
                else if (!string.IsNullOrWhiteSpace(info.ProductName)) _cached_name = info.ProductName;
                else _cached_name = Path.GetFileNameWithoutExtension(path);
            }
            catch 
            {
                _cached_name = Path.GetFileNameWithoutExtension(path);
            }
        }
        return show_exe ? $"{_cached_name} ({Path.GetFileName(path)})" : _cached_name;
    }

    public static string format_time(long s, bool full)
    {
        var t = TimeSpan.FromSeconds(s);
        if (full)
        {
            var parts = new List<string>();
            if (t.TotalHours >= 1) parts.Add($"{(int)t.TotalHours}ч");
            if (t.Minutes > 0) parts.Add($"{t.Minutes}м");
            parts.Add($"{t.Seconds}с");
            return string.Join(" ", parts);
        }
        if (t.TotalHours >= 1) return $"{(int)t.TotalHours}ч {t.Minutes}м";
        if (t.TotalMinutes >= 1) return $"{t.Minutes}м";
        return $"{t.Seconds}с";
    }
}

public class save_data
{
    public Dictionary<string, Dictionary<string, long>> history { get; set; } = new();
    public Dictionary<string, int> word_stats { get; set; } = new();
    public List<string> blacklist { get; set; } = new();
    public bool show_exe_in_list { get; set; } = false;
    public bool show_full_time { get; set; } = true;
    public bool top_most { get; set; } = false;
    public bool show_no_icon_apps { get; set; } = true;
    public bool overlay_mode { get; set; } = false;
    public int refresh_rate { get; set; } = 1;
    public string current_theme { get; set; } = "dark";
    public bool autostart_enabled { get; set; } = false;
    public long total_afk { get; set; } = 0;
    public long total_altt { get; set; } = 0;
    public long total_lmb { get; set; } = 0;
    public long total_rmb { get; set; } = 0;
    public long total_launch { get; set; } = 0;
    public long total_scroll { get; set; } = 0; 
}
