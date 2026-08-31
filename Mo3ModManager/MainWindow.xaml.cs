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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mo3ModManager
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        NodeTree NodeTree;

        public MainWindow()
        {
            this.InitializeComponent();

            this.ApplyLocalizedText();

            this.Title += " v" + System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;

            try
            {
                this.BuildTreeView();

                this.BuildProfiles();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.Dialog_Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Applies localized text to UI elements that x:Static cannot bind to
        /// (the strongly-typed Resources properties are generated as internal,
        /// so x:Static markup extension cannot access them via reflection).
        /// </summary>
        private void ApplyLocalizedText()
        {
            this.Title = Properties.Resources.MainWindow_Title;
            this.RunButtonText.Text = Properties.Resources.RunButton_Text;
            this.InstallModButtonText.Text = Properties.Resources.InstallModButton_Text;
            this.DeleteModButtonText.Text = Properties.Resources.DeleteModButton_Text;
            this.NewProfileButtonText.Text = Properties.Resources.NewProfileButton_Text;
            this.RenameProfileButtonText.Text = Properties.Resources.RenameProfileButton_Text;
            this.DeleteProfileButtonText.Text = Properties.Resources.DeleteProfileButton_Text;
            this.AboutButtonText.Text = Properties.Resources.AboutButton_Text;
            this.LanguageButtonText.Text = Properties.Resources.LanguageButton_Text;
            this.ProfilesGroupBox.Header = Properties.Resources.ProfilesGroupBox_HeaderNoSelection;
            this.ModsGroupBox.Header = Properties.Resources.ModsGroupBox_HeaderNoSelection;
        }

        private bool isCloseButtonEnabled = true;
        public bool IsCloseButtonEnabled {
            get {
                return this.isCloseButtonEnabled;
            }
            set {
                this.isCloseButtonEnabled = value;
                var hWnd = new System.Windows.Interop.WindowInteropHelper(this);
                var sysMenu = Win32.NativeMethods.GetSystemMenu(hWnd.Handle, false);
                Win32.NativeMethods.EnableMenuItem(sysMenu, Win32.NativeConstants.SC_CLOSE,
                    Win32.NativeConstants.MF_BYCOMMAND | (value ? Win32.NativeConstants.MF_ENABLED : Win32.NativeConstants.MF_GRAYED)
                    );
            }
        }

        private void AboutButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://go.mo3.club/mo3-mod-manager");
        }
        // Note: the About link and author contact info above are intentionally left untranslated (product/contact identifiers).

        private void LanguageButton_Click(object sender, RoutedEventArgs e)
        {
            this.LanguageContextMenu.Items.Clear();

            // When opened programmatically (IsOpen = true) rather than via
            // right-click, WPF does not automatically anchor the menu to the
            // clicked control, so it would otherwise appear at (0,0). Set the
            // placement target explicitly so it opens right below the button.
            this.LanguageContextMenu.PlacementTarget = this.LanguageButton;

            string currentLanguage = LocalizationManager.CurrentLanguage;

            var autoItem = new MenuItem
            {
                Header = Properties.Resources.LanguageMenu_Auto,
                IsCheckable = true,
                IsChecked = (currentLanguage == null)
            };
            autoItem.Click += (s, args) => this.SwitchLanguage(null);
            this.LanguageContextMenu.Items.Add(autoItem);

            this.LanguageContextMenu.Items.Add(new Separator());

            foreach (var languageCode in LocalizationManager.SupportedLanguages)
            {
                var menuItem = new MenuItem
                {
                    Header = LocalizationManager.GetDisplayName(languageCode),
                    IsCheckable = true,
                    IsChecked = (currentLanguage == languageCode),
                    Tag = languageCode
                };
                menuItem.Click += (s, args) => this.SwitchLanguage((string)((MenuItem)s).Tag);
                this.LanguageContextMenu.Items.Add(menuItem);
            }

            this.LanguageContextMenu.IsOpen = true;
        }

        private void SwitchLanguage(string languageCode)
        {
            LocalizationManager.ApplyLanguage(languageCode);
            LocalizationManager.SaveLanguagePreference(languageCode);

            // Refresh all localized text currently shown, including dynamic
            // headers/content that depend on the current selection.
            this.ApplyLocalizedText();
            this.Title += " v" + System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;
            this.ModTreeView_SelectedItemChanged(this, null);
            this.On_ProgProfilesListView_SelectionChanged();
        }

        private void BuildProfiles()
        {
            this.ProfilesListView.Items.Clear();

            System.IO.DirectoryInfo[] profilesFolders = new System.IO.DirectoryInfo(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles")).GetDirectories();
            foreach (var profileFolder in profilesFolders)
            {
                this.ProfilesListView.Items.Add(new ProfileItem(profileFolder));
            }

        }


        private void BuildTreeView()
        {
            this.ModTreeView.Items.Clear();

            this.NodeTree = new NodeTree();
            this.NodeTree.AddNodes(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods"));

            foreach (var rootNode in this.NodeTree.RootNodes)
            {
                this.ModTreeView.Items.Add(new ModItem(rootNode));
            }

        }

        private void UpdateRunButtonStatus()
        {
            this.RunButton.IsEnabled = (this.ModTreeView.SelectedItem != null) && (this.ModTreeView.SelectedItem as ModItem).Node.IsRunnable && (this.ProfilesListView.SelectedItem != null);
        }

        private void ModTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (this.ModTreeView.SelectedItem != null)
            {
                var selectedItem = this.ModTreeView.SelectedItem as ModItem;
                this.ModsGroupBox.Header = String.Format(Properties.Resources.ModsGroupBox_HeaderWithName, selectedItem.Title);

                this.DeleteModButton.IsEnabled = (selectedItem.Items.Count == 0);
            }
            else
            {
                this.ModsGroupBox.Header = Properties.Resources.ModsGroupBox_HeaderNoSelection;

                this.DeleteModButton.IsEnabled = false;
            }
            this.UpdateRunButtonStatus();
        }

        private void On_ProgProfilesListView_SelectionChanged()
        {
            if (this.ProfilesListView.SelectedItem != null)
            {
                var selectedItem = this.ProfilesListView.SelectedItem as ProfileItem;
                this.ProfilesGroupBox.Header = String.Format(Properties.Resources.ProfilesGroupBox_HeaderWithName, selectedItem.Name);

                this.RenameProfileButton.IsEnabled = true;
                this.DeleteProfileButton.IsEnabled = true;
            }
            else
            {
                this.ProfilesGroupBox.Header = Properties.Resources.ProfilesGroupBox_HeaderNoSelection;

                this.RenameProfileButton.IsEnabled = false;
                this.DeleteProfileButton.IsEnabled = false;
            }
            this.UpdateRunButtonStatus();
        }

        private void ProfilesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.On_ProgProfilesListView_SelectionChanged();
        }
        private void RunButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.Assert((this.ModTreeView.SelectedItem as ModItem).Node.IsRunnable);

            var arguments = new ModProcessManagerArguments
            {
                Node = (this.ModTreeView.SelectedItem as ModItem).Node,
                //random directory disabled because of the firewall setting
                //RunningDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Game-" + Guid.NewGuid().ToString().Substring(0, 8)),

                RunningDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Game"),
                //ProfileDirectory = (this.ProfilesListView.SelectedItem as ProfileItem).Directory
                ProfileDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles", (this.ProfilesListView.SelectedItem as ProfileItem).Name, (this.ModTreeView.SelectedItem as ModItem).Node.ID)
                
            };


            this.IsEnabled = false;
            this.IsCloseButtonEnabled = false;

            ModProcessManager modProcessManager = new ModProcessManager(arguments);
            modProcessManager.RunWorkerCompleted += (object worker_sender, System.ComponentModel.RunWorkerCompletedEventArgs worker_e) =>
             {
                 this.IsEnabled = true;
                 this.IsCloseButtonEnabled = true;

                 if (worker_e.Error == null)
                 {
                     System.Diagnostics.Trace.WriteLine("[Note] Game exited normally. Everything goes fine.");
                 }
                 else

                 {
                     System.Diagnostics.Trace.WriteLine("[Error] " + worker_e.Error.Message);
                     MessageBox.Show(worker_e.Error.Message, Properties.Resources.Dialog_Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                 }

                 this.BuildProfiles();
             };


            //workaround when OS<=win7
            if (System.Environment.OSVersion.Version.Major <= 5 || System.Environment.OSVersion.Version.Major == 6 && System.Environment.OSVersion.Version.Minor <= 1)
            {
                modProcessManager.RunLegacyAsync(this);
            }
            else
            {
                //OS >=Win8
                modProcessManager.RunAsync();
            }

        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start(AppDomain.CurrentDomain.BaseDirectory);
        }

        private string PurifyFileName(string Filename)
        {
            //replace invalid chars
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                Filename = Filename.Replace(c.ToString(), "_");
            }
            return Filename;
        }

        private void NewProfileButton_Click(object sender, RoutedEventArgs e)
        {
            string newProfileName = InputWindow.ShowDialog(this, Properties.Resources.NewProfile_Prompt, Properties.Resources.NewProfile_Caption);
            newProfileName = this.PurifyFileName(newProfileName);

            if (String.IsNullOrWhiteSpace(newProfileName)) return;

            string newProfilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles", newProfileName);
            try
            {
                if (System.IO.Directory.Exists(newProfilePath))
                {
                    MessageBox.Show(String.Format(Properties.Resources.ProfileAlreadyExists, newProfileName), Properties.Resources.Dialog_Title_Failure, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                var directoryInfo = System.IO.Directory.CreateDirectory(newProfilePath);
                this.ProfilesListView.Items.Add(new ProfileItem(directoryInfo));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.Dialog_Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenameProfileButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.Assert(this.ProfilesListView.SelectedItem != null);
            var selectedItem = (this.ProfilesListView.SelectedItem as ProfileItem);


            string newProfileName = InputWindow.ShowDialog(this, Properties.Resources.NewProfile_Prompt, Properties.Resources.NewProfile_Caption);
            newProfileName = this.PurifyFileName(newProfileName);

            if (String.IsNullOrWhiteSpace(newProfileName)) return;

            string newProfilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles", newProfileName);

            try
            {
                if (System.IO.Directory.Exists(newProfilePath))
                {
                    MessageBox.Show(String.Format(Properties.Resources.ProfileAlreadyExists, newProfileName), Properties.Resources.Dialog_Title_Failure, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                System.IO.Directory.Move(selectedItem.Directory, newProfilePath);
                selectedItem.ReplaceFrom(new ProfileItem(new System.IO.DirectoryInfo(newProfilePath)));
                this.On_ProgProfilesListView_SelectionChanged();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.Dialog_Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }

        }
        private void DeleteProfileButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.Assert(this.ProfilesListView.SelectedItem != null);
            var selectedItem = this.ProfilesListView.SelectedItem as ProfileItem;
            if (MessageBox.Show(String.Format(Properties.Resources.ConfirmDeleteProfile, selectedItem.Name), Properties.Resources.Dialog_Title_Warning, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                try
                {
                    System.IO.Directory.Delete(selectedItem.Directory, true);
                    this.ProfilesListView.Items.Remove(selectedItem);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Properties.Resources.Dialog_Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ProfilesListView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.ProfilesListView.SelectedItem == null) return;
            var selectedItem = this.ProfilesListView.SelectedItem as ProfileItem;
            System.Diagnostics.Process.Start(selectedItem.Directory);
        }

        private void AboutButton_MouseEnter(object sender, MouseEventArgs e)
        {
            // Mutate the existing AccessText's Text property rather than
            // replacing Button.Content with a new instance. Replacing the
            // instance would detach it from the "AboutButtonText" field used
            // by ApplyLocalizedText, causing language switches to silently
            // stop affecting this button until the next hover.
            this.AboutButtonText.Text = Properties.Resources.AboutButton_HoverText;
        }

        private void AboutButton_MouseLeave(object sender, MouseEventArgs e)
        {
            this.AboutButtonText.Text = Properties.Resources.AboutButton_Text;
        }

        private void DeleteModButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = this.ModTreeView.SelectedItem as ModItem;
            if (MessageBox.Show(String.Format(Properties.Resources.ConfirmDeleteMod, selectedItem.Name), Properties.Resources.Dialog_Title_Warning, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
            {
                try
                {
                    System.IO.Directory.Delete(selectedItem.Node.Directory, true);

                    this.NodeTree.RemoveNode(selectedItem.Node);

                    if (selectedItem.Parent == null)
                    {
                        this.ProfilesListView.Items.Remove(selectedItem);
                    }
                    else
                    {
                        selectedItem.Parent.Items.Remove(selectedItem);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Properties.Resources.Dialog_Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

        }

        private void InstallModButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog()
            {
                Filter = Properties.Resources.InstallMod_FileFilter,
                Title = Properties.Resources.InstallMod_DialogTitle
            };
            if ((bool)openFileDialog.ShowDialog())
            {
                try
                {
                    NodeTree testTree = new NodeTree(this.NodeTree);

                    //note that the elements are not copied
                    //suspose let testTree.RootNodes[0].Childs[0].MainExecutable = ""
                    //then this.NodeTree.RootNodes[0].Childs[0].MainExecutable=="" is true!
                    //be careful
                    var fastZip = new ICSharpCode.SharpZipLib.Zip.FastZip();

                    // Will always overwrite if target filenames already exist
                    fastZip.ExtractZip(
                        openFileDialog.FileName,
                        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Incoming"),
                        String.Empty);

                    testTree.AddNodes(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Incoming"));

                    if (testTree.Count() == this.NodeTree.Count()) throw new Exception("This archive doesn't contain any nodes.");

                    IO.CreateHardLinksOfFiles(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Incoming"), System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods"));

                    this.BuildTreeView();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Properties.Resources.Dialog_Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IO.ClearDirectory(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Incoming"));
                }


            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!this.IsEnabled)
            {
                var result = System.Windows.MessageBox.Show(this, Properties.Resources.ConfirmCloseWhileRunning, Properties.Resources.Dialog_Title_Warning,
                System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Exclamation);
                if (result != MessageBoxResult.Yes)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
