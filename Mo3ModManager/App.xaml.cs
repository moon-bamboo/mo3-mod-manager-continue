using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading;
using System.Windows;

namespace Mo3ModManager
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        // A unique name for the single-instance mutex. Using a fixed GUID (rather
        // than e.g. the assembly name) avoids any realistic chance of colliding
        // with an unrelated application's named mutex.
        private const string SingleInstanceMutexName = "Mo3ModManager-SingleInstance-B36F2F1A-6C7A-4E3B-9A6D-2E6F8A0B9C4D";

        private Mutex singleInstanceMutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            // Running two instances at once is unsafe: both would share the same
            // fixed "Game" working directory used to stage hard links while the
            // game runs, so a second instance could corrupt the first instance's
            // in-progress run. Enforce single instance via a named Mutex, which is
            // the standard way to detect "another instance is already running"
            // across separate processes on Windows.
            //
            // Note: the "initiallyOwned: true" argument only takes effect if this
            // call actually creates the mutex. If it already existed (i.e. another
            // instance is running, isNewInstance == false), this thread does NOT
            // own it, and must never call ReleaseMutex() on it -- doing so throws
            // ApplicationException. That's why the field is only kept (for release
            // in OnExit) when isNewInstance is true.
            bool isNewInstance;
            var mutex = new Mutex(true, SingleInstanceMutexName, out isNewInstance);
            if (!isNewInstance)
            {
                // Apply the saved language first so this message is shown in the
                // user's preferred language rather than always in English.
                LocalizationManager.LoadAndApplySavedLanguage();

                // Note: must use the fully-qualified "Mo3ModManager.Properties"
                // here rather than just "Properties" -- System.Windows.Application
                // (which this class derives from) already declares an instance
                // property named "Properties" (an IDictionary), which would
                // otherwise shadow the "Mo3ModManager.Properties" namespace.
                MessageBox.Show(Mo3ModManager.Properties.Resources.AnotherInstanceRunning, Mo3ModManager.Properties.Resources.Dialog_Title_Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Shutdown();
                return;
            }
            this.singleInstanceMutex = mutex;

            // Apply the user's saved language preference (if any) before
            // MainWindow is constructed, so all UI text is correct from the start.
            LocalizationManager.LoadAndApplySavedLanguage();

            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (this.singleInstanceMutex != null)
            {
                try
                {
                    this.singleInstanceMutex.ReleaseMutex();
                }
                catch (Exception ex)
                {
                    // Releasing the mutex is best-effort cleanup: the OS releases
                    // it automatically when the process exits regardless, so a
                    // failure here should never prevent shutdown or mask whatever
                    // exception (if any) triggered this shutdown in the first place.
                    System.Diagnostics.Trace.WriteLine("[Warn] Failed to release single-instance mutex: " + ex.Message);
                }
                this.singleInstanceMutex.Dispose();
                this.singleInstanceMutex = null;
            }
            base.OnExit(e);
        }
    }
}
