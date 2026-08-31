using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace Mo3ModManager
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            // Apply the user's saved language preference (if any) before
            // MainWindow is constructed, so all UI text is correct from the start.
            LocalizationManager.LoadAndApplySavedLanguage();

            base.OnStartup(e);
        }
    }
}
