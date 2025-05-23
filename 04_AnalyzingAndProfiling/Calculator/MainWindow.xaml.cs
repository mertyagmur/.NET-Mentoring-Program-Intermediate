using System.Data;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Calculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button)
            {
                string input = button.Content.ToString();
                string currentText = tb.Text;

                if (IsOperator(input))
                {
                    if (string.IsNullOrEmpty(currentText))
                    {
                        // Prevent starting with an operator
                        return;
                    }

                    if (IsOperator(currentText[^1].ToString()))
                    {
                        // Prevent consecutive operators
                        return;
                    }
                }

                tb.Text += input;
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            tb.Text = string.Empty;
        }
        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(tb.Text))
            {
                tb.Text = tb.Text.Substring(0, tb.Text.Length - 1);
            }
        }

        private void Off_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Result_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string expression = tb.Text;

                if (string.IsNullOrWhiteSpace(expression))
                {
                    return;
                }

                if (IsOperator(expression[^1].ToString()))
                {
                    tb.Text += "=Error";
                    return;
                }

                if (Regex.IsMatch(expression, @"[\+\-\*/]{2,}"))
                {
                    tb.Text += "=Error";
                    return;
                }

                var result = new DataTable().Compute(expression, null);
                tb.Text += "=" + result.ToString();
            }
            catch
            {
                tb.Text += "=Error";
            }
        }
        private bool IsOperator(string input)
        {
            return input == "+" || input == "-" || input == "*" || input == "/";
        }
    }
}