using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Mo3ModManager
{
    /// <summary>
    /// InputWindow.xaml 的交互逻辑
    /// </summary>
    public partial class InputWindow : Window
    {
        public InputWindow()
        {
            InitializeComponent();

            this.OKButton.Content = Properties.Resources.InputWindow_OK;
            this.CancelButton.Content = Properties.Resources.InputWindow_Cancel;
        }

        /// <summary>
        /// Shows the dialog modally and returns the entered text.
        /// </summary>
        /// <returns>
        /// The text box contents when the user confirms (which may legitimately
        /// be an empty string -- some node.json fields, such as
        /// main_executable, can be cleared), or <c>null</c> when the user
        /// cancels (Cancel button, Esc, or the title bar's close button).
        /// Callers that accept an empty value MUST distinguish these two cases
        /// by testing for <c>null</c>; treating String.Empty as "cancelled"
        /// would silently wipe the field whenever the user hits Cancel.
        /// </returns>
        public static string ShowDialog(Window Owner, string Text, string Caption, string DefaultText = "")
        {
            var dialog = new InputWindow() { Owner = Owner, Title = Caption };
            dialog.TextBlock.Text = Text;
            dialog.TextBox.Text = DefaultText;
            dialog.TextBox.SelectAll();

            // DialogResult is a bool?: it stays null when the window is closed
            // without going through either button (e.g. the title bar's X),
            // which must count as "cancelled" like the Cancel button. Casting
            // it to bool directly would instead throw on that path.
            bool? result = dialog.ShowDialog();
            return (result == true) ? dialog.TextBox.Text : null;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true; 
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
