using System.IO;
using System.Windows;
using System.Windows.Input;

namespace statictime;

public partial class bpicker : Window
{
    private readonly tracker _tracker;
    public string? selected_path { get; private set; }

    public bpicker(tracker t)
    {
        InitializeComponent();
        _tracker = t;
        load_known_apps();
    }

    private void load_known_apps()
    {
        var items = new List<app_item>();
        foreach (var path in _tracker.get_all_known_apps())
        {
            if (!File.Exists(path)) continue;
            items.Add(new app_item
            {
                path = path,
                icon = _tracker.get_icon(path),
                show_exe = _tracker.data.show_exe_in_list
            });
        }
        list_known_apps.ItemsSource = items;
    }

    private void browse_file(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Filter = "приложения (*.exe)|*.exe|все файлы (*.*)|*.*",
            Title = "выбери приложение"
        };
        if (dialog.ShowDialog() == true)
        {
            selected_path = dialog.FileName.ToLower().Trim();
            DialogResult = true;
        }
    }

    private void pick_known_app(object sender, RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.Button btn && btn.Tag is string path)
        {
            selected_path = path;
            DialogResult = true;
        }
    }

    private void drag_window(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed) DragMove();
    }

    private void close(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
