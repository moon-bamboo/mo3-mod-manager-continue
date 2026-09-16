using System;
using System.Windows;

namespace Mo3ModManager
{
    /// <summary>
    /// A small, always-on-top, non-modal status window shown while a game is
    /// running (Windows 8+ path only, see MainWindow.RunButton_Click). Its
    /// purpose is purely a manual, last-resort escape hatch: normally this
    /// window is simply closed automatically once the game's exit is
    /// detected (see ModProcessManager.RunAsync / RunWorkerCompleted). It is
    /// deliberately NOT closable by the user via the title bar (Closing is
    /// suppressed) or a timer -- exit detection failing is an edge case, not
    /// the expected outcome, so this window should not go away just because
    /// the user got impatient; only the explicit "force unlock" action (which
    /// carries its own confirmation) can end it early.
    /// </summary>
    public partial class GameRunningWindow : Window
    {
        /// <summary>
        /// Raised when the user clicks "Force Unlock" and confirms the
        /// follow-up warning. The caller is responsible for actually
        /// terminating the game process tree (see
        /// ModProcessManager.TryForceTerminate) and for closing this window
        /// once that's done -- this event only signals the *request*.
        /// </summary>
        public event EventHandler ForceUnlockRequested;

        // Set by the caller once the normal exit-detection path succeeds (or
        // this window is otherwise no longer needed), to allow this window to
        // close itself programmatically without going through the
        // "prevent user from closing it" guard in Window_Closing.
        private bool allowClose = false;

        public GameRunningWindow(Window owner)
        {
            this.InitializeComponent();
            this.Owner = owner;
            this.ApplyLocalizedText();
        }

        private void ApplyLocalizedText()
        {
            this.Title = Properties.Resources.GameRunningWindow_Title;
            this.StatusText.Text = Properties.Resources.GameRunningWindow_Status;
            this.HintText.Text = Properties.Resources.GameRunningWindow_Hint;
            this.ForceUnlockButtonText.Text = Properties.Resources.GameRunningWindow_ForceUnlockButton;
        }

        /// <summary>
        /// Closes this window from application code (as opposed to the user
        /// clicking a close button, which is suppressed -- see
        /// Window_Closing). Safe to call from any thread's perspective in the
        /// sense that it's a no-op if the window is already gone; callers
        /// must still marshal the actual call onto the UI thread themselves.
        /// </summary>
        public void CloseProgrammatically()
        {
            this.allowClose = true;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!this.allowClose)
            {
                // Ignore the title bar's close button (Alt+F4, etc.): this
                // window must stay open until either the game's exit is
                // detected normally or the user explicitly force-unlocks via
                // the button below, which carries its own confirmation.
                e.Cancel = true;
            }
        }

        private void ForceUnlockButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(this,
                Properties.Resources.GameRunningWindow_ConfirmForceUnlock,
                Properties.Resources.Dialog_Title_Warning,
                MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (result != MessageBoxResult.Yes) return;

            var handler = this.ForceUnlockRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
