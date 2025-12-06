using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Diagnostics;
using Forms = System.Windows.Forms;

namespace statictime;

public partial class MainWindow
{
    private void apply_settings()
    {
        check_autostart.IsChecked = _tracker.autostart_enabled();
        check_showexe.IsChecked = _tracker.data.show_exe_in_list;
        check_fulltime.IsChecked = _tracker.data.show_full_time;
        check_topmost.IsChecked = _tracker.data.top_most;
        Topmost = _tracker.data.top_most;
        int index = 0;
        switch(_tracker.data.refresh_rate)
        {
            case 1: index = 0; break;
            case 5: index = 1; break;
            case 10: index = 2; break;
            case 30: index = 3; break;
            case 60: index = 4; break;
        }
        refresh.SelectedIndex = index;
    }

    private void check_args()
    {
        string[] args = Environment.GetCommandLineArgs();
        foreach (var arg in args)
        {
            if (arg == "--silent")
            {
                Hide();
                ShowInTaskbar = false;
            }
        }
    }
    
    private void validate_state(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
        }
    }

    private void tray()
    {
        try 
        {
            _notify = new Forms.NotifyIcon();
            var loc = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(loc) && File.Exists(loc))
            {
                _notify.Icon = System.Drawing.Icon.ExtractAssociatedIcon(loc);
            }
            _notify.Visible = true;
            _notify.Text = "StaticTime";
            var ctx = new Forms.ContextMenuStrip();
            ctx.Items.Add("открыть", null, (s, e) => show_window());
            ctx.Items.Add("выйти", null, (s, e) => exit(null, null));
            _notify.ContextMenuStrip = ctx;
            _notify.MouseClick += (s, e) => 
            {
                if (e.Button == Forms.MouseButtons.Left) show_window();
            };
        }
        catch {}
    }

    private void show_window()
    {
        Show();
        WindowState = WindowState.Normal;
        ShowInTaskbar = true;
        Activate();
    }

    private void drag_window(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left) DragMove();
    }

    private void close(object? sender, RoutedEventArgs? e)
    {
        Hide();
        ShowInTaskbar = false;
    }

    private void exit(object? sender, RoutedEventArgs? e)
    {
        _is_exit = true;
        _cts.Cancel();
        if (_notify != null) _notify.Dispose();
        UnhookWindowsHookEx(_hook_id);
        _tracker.save();
        System.Windows.Application.Current.Shutdown();
    }

    private void menu(object? sender, RoutedEventArgs? e)
    {
        if (!_is_menu_open)
        {
            open_menu();
        }
        else
        {
            close_menu();
        }
    }

    private void open_menu()
    {
        _is_menu_open = true;
        overlay.Visibility = Visibility.Visible;
        var anim = new DoubleAnimation(0, 1, TimeSpan.FromSeconds(0.2));
        overlay.BeginAnimation(OpacityProperty, anim);
        var slide = new DoubleAnimation(-220, 0, TimeSpan.FromSeconds(0.2));
        slide.EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut };
        transform.BeginAnimation(TranslateTransform.XProperty, slide);
    }

    private void close_menu()
    {
        _is_menu_open = false;
        var anim = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.2));
        anim.Completed += (s, e) => overlay.Visibility = Visibility.Collapsed;
        overlay.BeginAnimation(OpacityProperty, anim);
        var slide = new DoubleAnimation(0, -220, TimeSpan.FromSeconds(0.2));
        transform.BeginAnimation(TranslateTransform.XProperty, slide);
    }

    private void click_overlay(object sender, MouseButtonEventArgs e)
    {
        close_menu();
    }
    
    private void prevent_close(object sender, MouseButtonEventArgs e)
    {
        e.Handled = true;
    }

    private void show_time_categories(object sender, RoutedEventArgs e)
    {
        if (categories.IsOpen) 
            categories.IsOpen = false;
        else 
            categories.IsOpen = true;
    }
    
    private void close_popup(object sender, MouseButtonEventArgs e)
    {
        categories.IsOpen = false;
    }

    private void apply_filter(string filter)
    {
        _current_filter = filter;
        categories.IsOpen = false;
        update_data_for_current_filter();
    }

    private void all_time(object sender, RoutedEventArgs e) => apply_filter("all");
    private void today(object sender, RoutedEventArgs e) => apply_filter("today");
    private void yesterday(object sender, RoutedEventArgs e) => apply_filter("yesterday");
    private void week(object sender, RoutedEventArgs e) => apply_filter("week");
    private void month(object sender, RoutedEventArgs e) => apply_filter("month");
    private void year(object sender, RoutedEventArgs e) => apply_filter("year");
    private void home(object sender, RoutedEventArgs e)
    {
        close_menu();
        panel_main.Visibility = Visibility.Visible;
        panel_stats.Visibility = Visibility.Collapsed;
        panel_settings.Visibility = Visibility.Collapsed;
        update_data_for_current_filter();
    }

    private void settings(object sender, RoutedEventArgs e)
    {
        close_menu();
        panel_settings.Visibility = Visibility.Visible;
        panel_main.Visibility = Visibility.Collapsed;
        panel_stats.Visibility = Visibility.Collapsed;
    }
    
    private void stats(object sender, RoutedEventArgs e)
    {
        close_menu();
        panel_stats.Visibility = Visibility.Visible;
        panel_main.Visibility = Visibility.Collapsed;
        panel_settings.Visibility = Visibility.Collapsed;
        update_stats_ui();
    }

    private void open_github(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo("https://github.com/just-blaing/StaticTime-v2") { UseShellExecute = true });
        }
        catch {}
    }

    private void close_settings(object sender, RoutedEventArgs e)
    {
        panel_settings.Visibility = Visibility.Collapsed;
        panel_main.Visibility = Visibility.Visible;
        update_data_for_current_filter();
    }
    
    private void close_stats(object sender, RoutedEventArgs e)
    {
        panel_stats.Visibility = Visibility.Collapsed;
        panel_main.Visibility = Visibility.Visible;
        update_data_for_current_filter();
    }

    private void autostart(object sender, RoutedEventArgs e)
    {
        bool val = check_autostart.IsChecked == true;
        _tracker.autostart(val); 
        _tracker.save();
    }
    
    private void showexe(object sender, RoutedEventArgs e)
    {
        _tracker.data.show_exe_in_list = check_showexe.IsChecked == true;
        _tracker.save();
    }
    
    private void fulltime(object sender, RoutedEventArgs e)
    {
        _tracker.data.show_full_time = check_fulltime.IsChecked == true;
        _tracker.save();
    }

    private void topmost(object sender, RoutedEventArgs e)
    {
        bool val = check_topmost.IsChecked == true;
        _tracker.data.top_most = val;
        Topmost = val;
        _tracker.save();
    }

    private void change_rrate(object sender, SelectionChangedEventArgs e)
    {
        int val = 1;
        switch (refresh.SelectedIndex)
        {
            case 0: val = 1; break;
            case 1: val = 5; break;
            case 2: val = 10; break;
            case 3: val = 30; break;
            case 4: val = 60; break;
        }
        _tracker.data.refresh_rate = val;
        _timer.Interval = TimeSpan.FromSeconds(val);
        _tracker.save();
    }

    private void reset(object sender, RoutedEventArgs e)
    {
        if (System.Windows.MessageBox.Show("точно сбросить все данные?", "StaticTime", System.Windows.MessageBoxButton.YesNo) == System.Windows.MessageBoxResult.Yes)
        {
            _tracker.reset_data();
            apply_settings();
            update_data_for_current_filter();
        }
    }
}
