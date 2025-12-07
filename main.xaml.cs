using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;
using System.Globalization;
using Forms = System.Windows.Forms;
using Color = System.Windows.Media.Color;

namespace statictime;

public class theme_to_bool_converter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString();
    }

    public object ConvertBack(object value, Type targetType, object culture, CultureInfo cultureInfo)
    {
        return System.Windows.Data.Binding.DoNothing;
    }
}

public partial class MainWindow : Window
{
    private tracker _tracker = new();
    private DispatcherTimer _timer = new();
    private Forms.NotifyIcon? _notify;
    private bool _is_exit = false;
    private CancellationTokenSource _cts = new();
    private string _current_filter = "today";
    private bool _is_menu_open = false;
    private SolidColorBrush _text_brush = new SolidColorBrush(Color.FromRgb(0xdd, 0xdd, 0xdd));
    private LowLevelProc _mouse_proc;
    private LowLevelProc _kb_proc;
    private IntPtr _mouse_hook_id = IntPtr.Zero;
    private IntPtr _kb_hook_id = IntPtr.Zero;
    public MainWindow()
    {
        InitializeComponent();
        this.DataContext = _tracker.data;
        this.Tag = _text_brush;
        tray();
        check_args();
        apply_settings();
        apply_theme(_tracker.data.current_theme, false);
        update_theme_buttons_state(_tracker.data.current_theme);
        _timer.Interval = TimeSpan.FromSeconds(_tracker.data.refresh_rate);
        _timer.Tick += on_tick;
        _timer.Start();
        _mouse_proc = mouse_hook_callback;
        _mouse_hook_id = set_mouse_hook(_mouse_proc);
        _kb_proc = kb_hook_callback;
        _kb_hook_id = set_kb_hook(_kb_proc);
        Task.Run(() => input_loop(_cts.Token));
        update_data_for_current_filter();
        this.Loaded += (s, e) => update_window();
    }
    
    private void change_size(object sender, SizeChangedEventArgs e)
    {
        update_window();
    }

    private void update_window()
    {
        double corner_radius_val = root_border.CornerRadius.TopLeft; 
        double margin_val = root_border.Margin.Left; 
        
        RectangleGeometry clip_geometry = new RectangleGeometry();
        clip_geometry.Rect = new Rect(0, 0, this.ActualWidth, this.ActualHeight);
        clip_geometry.RadiusX = corner_radius_val + margin_val;
        clip_geometry.RadiusY = corner_radius_val + margin_val;
        
        this.Clip = clip_geometry;
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        if (!_is_exit)
        {
            e.Cancel = true;
            close(null, null);
        }
        else
        {
            UnhookWindowsHookEx(_mouse_hook_id);
            UnhookWindowsHookEx(_kb_hook_id);
        }
        base.OnClosing(e);
    }
}
