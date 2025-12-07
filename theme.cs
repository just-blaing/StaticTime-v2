using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Color = System.Windows.Media.Color;

namespace statictime;

public partial class MainWindow
{
    private void apply_theme(string theme_name, bool animate = true)
    {
        var bg_color = Color.FromRgb(0x1e, 0x1e, 0x1e);
        var text_color = Color.FromRgb(0xdd, 0xdd, 0xdd);
        var menu_bg = Color.FromRgb(0x22, 0x22, 0x22);
        if (theme_name == "white")
        {
            bg_color = Color.FromRgb(0xf5, 0xf5, 0xf5);
            text_color = Color.FromRgb(0x22, 0x22, 0x22);
            menu_bg = Color.FromRgb(0xe0, 0xe0, 0xe0);
        }
        else if (theme_name == "amoled")
        {
            bg_color = Colors.Black;
            text_color = Colors.White;
            menu_bg = Color.FromRgb(0x11, 0x11, 0x11);
        }
        if (animate)
        {
            var pos = Mouse.GetPosition(this);
            theme_spreader.Fill = new SolidColorBrush(bg_color);
            Canvas.SetLeft(theme_spreader, pos.X);
            Canvas.SetTop(theme_spreader, pos.Y);
            var max_dim = Math.Max(ActualWidth, ActualHeight) * 3;
            var size_anim = new DoubleAnimation(0, max_dim, TimeSpan.FromSeconds(0.5))
            {
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };
            var pos_anim_x = new DoubleAnimation(pos.X, pos.X - max_dim / 2, TimeSpan.FromSeconds(0.5))
            {
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };
            var pos_anim_y = new DoubleAnimation(pos.Y, pos.Y - max_dim / 2, TimeSpan.FromSeconds(0.5))
            {
                EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };
            var color_anim = new ColorAnimation(text_color, TimeSpan.FromSeconds(0.4));
            _text_brush.BeginAnimation(SolidColorBrush.ColorProperty, color_anim);
            var bg_anim = new ColorAnimation(bg_color, TimeSpan.FromSeconds(0.5));
            root_border.Background.BeginAnimation(SolidColorBrush.ColorProperty, bg_anim);

            size_anim.Completed += (s, e) =>
            {
                finish_theme_apply(bg_color, menu_bg);
                theme_spreader.Width = 0;
                theme_spreader.Height = 0;
            };
            theme_spreader.BeginAnimation(WidthProperty, size_anim);
            theme_spreader.BeginAnimation(HeightProperty, size_anim);
            theme_spreader.BeginAnimation(Canvas.LeftProperty, pos_anim_x);
            theme_spreader.BeginAnimation(Canvas.TopProperty, pos_anim_y);
        }
        else
        {
            _text_brush.Color = text_color;
            finish_theme_apply(bg_color, menu_bg);
        }
        update_theme_buttons_state(theme_name);
    }

    private void finish_theme_apply(Color bg, Color menu)
    {
        var bg_brush = new SolidColorBrush(bg);
        var brush = new SolidColorBrush(menu);
        root_border.Background = bg_brush;
        panel.Background = brush;
        border.Background = brush;
    }

    private void update_theme_buttons_state(string current_theme)
    {
        theme_dark.IsChecked = (current_theme == "dark");
        theme_white.IsChecked = (current_theme == "white");
        theme_amoled.IsChecked = (current_theme == "amoled");
    }

    private void set_theme_dark(object sender, RoutedEventArgs e) 
    { 
        if (_tracker.data.current_theme != "dark") 
        { 
            _tracker.data.current_theme = "dark"; 
            _tracker.save(); 
            apply_theme("dark"); 
        } 
    }
    
    private void set_theme_white(object sender, RoutedEventArgs e) 
    { 
        if (_tracker.data.current_theme != "white") 
        { 
            _tracker.data.current_theme = "white"; 
            _tracker.save(); 
            apply_theme("white"); 
        } 
    }

    private void set_theme_amoled(object sender, RoutedEventArgs e) 
    { 
        if (_tracker.data.current_theme != "amoled") 
        { 
            _tracker.data.current_theme = "amoled"; 
            _tracker.save(); 
            apply_theme("amoled"); 
        } 
    }
}
