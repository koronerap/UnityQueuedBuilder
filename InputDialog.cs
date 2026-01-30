using System.Windows;

namespace UnityQueuedBuilder
{
    public partial class InputDialog : Window
    {
        public string InputText { get; private set; }

        public InputDialog(string question, string title)
        {
            Title = title;
            Width = 300;
            Height = 150;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(30, 30, 30));
            Foreground = System.Windows.Media.Brushes.White;

            var stack = new System.Windows.Controls.StackPanel { Margin = new Thickness(10) };
            stack.Children.Add(new System.Windows.Controls.TextBlock { Text = question, Foreground = System.Windows.Media.Brushes.White, Margin = new Thickness(0, 0, 0, 10) });

            var txt = new System.Windows.Controls.TextBox { Padding = new Thickness(5) };
            stack.Children.Add(txt);

            var btn = new System.Windows.Controls.Button
            {
                Content = "OK",
                Margin = new Thickness(0, 10, 0, 0),
                Padding = new Thickness(5),
                IsDefault = true
            };
            btn.Click += (s, e) => { InputText = txt.Text; DialogResult = true; };
            stack.Children.Add(btn);

            Content = stack;
        }
    }
}