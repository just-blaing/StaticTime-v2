using System.IO;
using System.Windows;
using statictime;

namespace statictime;

public partial class MainWindow
{
    private void on_tick(object? sender, EventArgs e)
    {
        string active = _tracker.get_active_proc();
        if (!string.IsNullOrEmpty(active))
        {
            _tracker.add_time(active);
        }
        if (Visibility == Visibility.Visible && !_tracker.is_user_afk())
        {
            if (panel_stats.Visibility == Visibility.Visible)
            {
                update_stats_ui();
            }
            else if (panel_main.Visibility == Visibility.Visible)
            {
                update_data_for_current_filter();
            }
        }
    }
    
    private void update_data_for_current_filter()
    {
        string label = get_filter_name(_current_filter);
        time_filter.Content = label + " ▼";
        if (_current_filter == "all")
        {
            calc_all_time();
        }
        else
        {
            DateTime start = DateTime.Today;
            DateTime end = DateTime.Today;
            switch(_current_filter)
            {
                case "yesterday": start = DateTime.Today.AddDays(-1); end = DateTime.Today.AddDays(-1); break;
                case "week": start = DateTime.Today.AddDays(-7); end = DateTime.Today; break;
                case "month": start = DateTime.Today.AddMonths(-1); end = DateTime.Today; break;
                case "year": start = DateTime.Today.AddYears(-1); end = DateTime.Today; break;
            }
            calc_period(start, end);
        }
    }

    private void calc_all_time()
    {
        var total_map = new Dictionary<string, long>();
        long grand_total = 0;
        foreach (var day in _tracker.data.history)
        {
            foreach (var app in day.Value)
            {
                if (!total_map.ContainsKey(app.Key)) total_map[app.Key] = 0;
                total_map[app.Key] += app.Value;
                grand_total += app.Value;
            }
        }
        update_total_text(TimeSpan.FromSeconds(grand_total));
        update_list_source(total_map);
    }
    
    private void calc_period(DateTime start, DateTime end)
    {
        var total_map = new Dictionary<string, long>();
        long period_total = 0;
        foreach (var day_str in _tracker.data.history.Keys)
        {
            if (DateTime.TryParse(day_str, out DateTime dt))
            {
                if (dt >= start && dt <= end)
                {
                     foreach (var app in _tracker.data.history[day_str])
                     {
                        if (!total_map.ContainsKey(app.Key)) total_map[app.Key] = 0;
                        total_map[app.Key] += app.Value;
                        period_total += app.Value;
                     }
                }
            }
        }
        update_total_text(TimeSpan.FromSeconds(period_total));
        update_list_source(total_map);
    }
    private void update_total_text(TimeSpan t)
    {
        if (_tracker.data.show_full_time)
            total_hours.Text = $"{(int)t.TotalHours}ч {t.Minutes}м {t.Seconds}с";
        else
            total_hours.Text = $"{(int)t.TotalHours}ч {t.Minutes}м";
    }
    
    private string get_filter_name(string f)
    {
         switch(f)
        {
            case "today": return "сегодня";
            case "yesterday": return "вчера";
            case "week": return "неделя";
            case "month": return "месяц";
            case "year": return "год";
            default: return "за все время";
        }
    }

    private void update_list_source(Dictionary<string, long> map)
    {
        var ui_list = new List<app_item>();
        foreach (var kvp in map.OrderByDescending(x => x.Value))
        {
             ui_list.Add(new app_item 
             { 
                 name = Path.GetFileNameWithoutExtension(kvp.Key), 
                 path = kvp.Key, 
                 seconds_played = kvp.Value,
                 icon = _tracker.get_icon(kvp.Key),
                 show_exe = _tracker.data.show_exe_in_list,
                 full_time_format = _tracker.data.show_full_time
             });
        }
        list_apps.ItemsSource = ui_list;
    }

    private void update_stats_ui()
    {
        stats_alt.Text = $"вы нажали Alt + Tab: {_tracker.data.total_altt}";
        var t = TimeSpan.FromSeconds(_tracker.data.total_afk);
        stats_afk.Text = $"вы пробыли в афк: {(int)t.TotalHours}ч {t.Minutes}м {t.Seconds}с";
        stats_boot.Text = $"вы включили пк: {_tracker.data.total_launch}";
        stats_lmb.Text = $"вы нажали лкм: {_tracker.data.total_lmb}";
        stats_rmb.Text = $"вы нажали пкм: {_tracker.data.total_rmb}";
        double cm = _tracker.data.total_scroll;
        if (cm >= 100)
            stats_scroll.Text = $"вы пролистали: {(cm/100.0):F2}м";
        else
            stats_scroll.Text = $"вы пролистали: {cm}см";
    }
}
