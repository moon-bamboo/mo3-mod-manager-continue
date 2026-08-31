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
        ModDisplayState DisplayState;

        // Suppresses persisting the current selection to user settings while
        // we are programmatically restoring a previously-saved selection (as
        // opposed to the user actually clicking something). Without this, the
        // transient "nothing selected" state that occurs while rebuilding the
        // list (Items.Clear() before re-adding items) would overwrite the
        // saved preference, and re-applying a saved preference would count as
        // a fresh "last selection" write, which is harmless but unnecessary.
        private bool suppressSelectionPersistence = false;

        public MainWindow()
        {
            this.InitializeComponent();

            this.ApplyLocalizedText();

            this.Title += " v" + System.Diagnostics.FileVersionInfo.GetVersionInfo(System.Reflection.Assembly.GetExecutingAssembly().Location).FileVersion;

            try
            {
                this.suppressSelectionPersistence = true;

                this.BuildTreeView();
                this.SelectModByID(Properties.Settings.Default.LastModID);

                this.BuildProfiles();
                this.SelectDefaultOrLastProfile(Properties.Settings.Default.LastProfileName);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.Dialog_Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                Environment.Exit(1);
            }
            finally
            {
                this.suppressSelectionPersistence = false;
            }
        }

        private string GetSelectedProfileName()
        {
            var item = this.ProfilesListView.SelectedItem as ProfileItem;
            return item == null ? null : item.Name;
        }

        private string GetSelectedModID()
        {
            var item = this.ModTreeView.SelectedItem as ModItem;
            return (item == null || item.Node == null) ? null : item.Node.ID;
        }

        /// <summary>
        /// Selects the profile with the given name, if it exists in the list.
        /// Does nothing (leaves the current selection, typically none) if not found.
        /// </summary>
        private void SelectProfileByName(string name)
        {
            if (String.IsNullOrEmpty(name)) return;
            foreach (ProfileItem item in this.ProfilesListView.Items)
            {
                if (item.Name == name)
                {
                    this.ProfilesListView.SelectedItem = item;
                    return;
                }
            }
        }

        /// <summary>
        /// Selects the profile with the given name if it exists; otherwise falls
        /// back to selecting the first profile in the list (if any). Used at
        /// startup so a fresh install with no saved preference still has a
        /// sensible default selection instead of requiring an extra click.
        /// </summary>
        private void SelectDefaultOrLastProfile(string preferredName)
        {
            if (!String.IsNullOrEmpty(preferredName))
            {
                foreach (ProfileItem item in this.ProfilesListView.Items)
                {
                    if (item.Name == preferredName)
                    {
                        this.ProfilesListView.SelectedItem = item;
                        return;
                    }
                }
            }
            if (this.ProfilesListView.Items.Count > 0)
            {
                this.ProfilesListView.SelectedItem = this.ProfilesListView.Items[0];
            }
        }

        /// <summary>
        /// Selects the mod with the given Node.ID anywhere in the tree, if it exists.
        /// Does nothing (leaves the current selection, typically none) if not found.
        /// </summary>
        private void SelectModByID(string id)
        {
            if (String.IsNullOrEmpty(id)) return;
            this.TrySelectModByID(this.ModTreeView.Items.Cast<ModItem>(), id);
        }

        private bool TrySelectModByID(IEnumerable<ModItem> items, string id)
        {
            foreach (var item in items)
            {
                if (item.Node.ID == id)
                {
                    item.IsSelected = true;
                    return true;
                }
                if (this.TrySelectModByID(item.Items, id))
                {
                    return true;
                }
            }
            return false;
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
            this.RefreshButtonText.Text = Properties.Resources.RefreshButton_Text;
            this.NewProfileButtonText.Text = Properties.Resources.NewProfileButton_Text;
            this.RenameProfileButtonText.Text = Properties.Resources.RenameProfileButton_Text;
            this.DeleteProfileButtonText.Text = Properties.Resources.DeleteProfileButton_Text;
            this.AboutButtonText.Text = Properties.Resources.AboutButton_Text;
            this.LanguageButtonText.Text = Properties.Resources.LanguageButton_Text;

            // The context menu is defined in Window.Resources (so it can be
            // shared across all TreeViewItems via ItemContainerStyle), which
            // means its x:Name'd children are not registered in the window's
            // namescope and can't be referenced via generated fields (unlike
            // elements in the main visual tree). Look it up via the resource
            // key instead.
            var modContextMenu = (ContextMenu)this.Resources["ModItemContextMenu"];
            var openFolderMenuItem = (MenuItem)modContextMenu.Items[0];
            openFolderMenuItem.Header = Properties.Resources.OpenModFolderMenuItem_Text;
            // Items[1] is a Separator, not a MenuItem.
            var renameMenuItem = (MenuItem)modContextMenu.Items[2];
            renameMenuItem.Header = Properties.Resources.RenameModMenuItem_Text;
            var changeIdMenuItem = (MenuItem)modContextMenu.Items[3];
            changeIdMenuItem.Header = Properties.Resources.ChangeModIdMenuItem_Text;
            // ToggleHideModMenuItem's text is set dynamically in
            // ModItemContextMenu_Opened (it depends on the selected mod's
            // hidden state), so it's intentionally not set here.
            this.ShowHiddenModsCheckBoxText.Text = Properties.Resources.ShowHiddenModsCheckBox_Text;
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

            string modsDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods");

            this.NodeTree = new NodeTree();
            this.NodeTree.AddNodes(modsDirectory);

            this.DisplayState = ModDisplayState.Load(modsDirectory);

            bool showHidden = this.ShowHiddenModsCheckBox.IsChecked == true;
            Func<Node, bool> shouldInclude = node => showHidden || !this.DisplayState.IsHidden(node.ID);
            Func<Node, bool> isHidden = node => this.DisplayState.IsHidden(node.ID);

            foreach (var rootNode in this.NodeTree.RootNodes)
            {
                if (!shouldInclude(rootNode)) continue;
                this.ModTreeView.Items.Add(new ModItem(rootNode, shouldInclude, isHidden));
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

                // Deliberately checks Node.Childs (the real underlying data),
                // not selectedItem.Items (the possibly-filtered UI tree): if a
                // child mod is hidden, it's absent from Items even though it
                // still exists on disk, and deleting this mod would orphan it.
                this.DeleteModButton.IsEnabled = (selectedItem.Node.Childs.Count == 0);
            }
            else
            {
                this.ModsGroupBox.Header = Properties.Resources.ModsGroupBox_HeaderNoSelection;

                this.DeleteModButton.IsEnabled = false;
            }
            this.UpdateRunButtonStatus();

            if (!this.suppressSelectionPersistence)
            {
                Properties.Settings.Default.LastModID = this.GetSelectedModID() ?? String.Empty;
                Properties.Settings.Default.Save();
            }
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

            if (!this.suppressSelectionPersistence)
            {
                Properties.Settings.Default.LastProfileName = this.GetSelectedProfileName() ?? String.Empty;
                Properties.Settings.Default.Save();
            }
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

                 // BuildProfiles() clears and re-adds all items (profile sizes may
                 // have changed from the game run), which would otherwise clear
                 // the selection. Preserve and restore it so the user isn't forced
                 // to re-pick the profile before they can run again.
                 string profileNameToRestore = this.GetSelectedProfileName();
                 this.suppressSelectionPersistence = true;
                 try
                 {
                     this.BuildProfiles();
                     this.SelectProfileByName(profileNameToRestore);
                 }
                 finally
                 {
                     this.suppressSelectionPersistence = false;
                 }
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

        private void ShowHiddenModsCheckBox_CheckedChanged(object sender, RoutedEventArgs e)
        {
            // Guard against firing during InitializeComponent(), before
            // suppressSelectionPersistence/DisplayState are ready, and before
            // there's anything meaningful to rebuild.
            if (this.NodeTree == null) return;

            string modIDToRestore = this.GetSelectedModID();
            this.suppressSelectionPersistence = true;
            try
            {
                this.BuildTreeView();
                this.SelectModByID(modIDToRestore);
            }
            finally
            {
                this.suppressSelectionPersistence = false;
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            // Preserve the current selection across the rebuild: BuildTreeView/
            // BuildProfiles both clear and re-add all items, which would
            // otherwise silently drop the user's current selection every time
            // they refresh.
            string modIDToRestore = this.GetSelectedModID();
            string profileNameToRestore = this.GetSelectedProfileName();

            this.suppressSelectionPersistence = true;
            try
            {
                this.BuildTreeView();
                this.SelectModByID(modIDToRestore);

                this.BuildProfiles();
                this.SelectProfileByName(profileNameToRestore);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.Dialog_Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                this.suppressSelectionPersistence = false;
            }
        }

        private void NewProfileButton_Click(object sender, RoutedEventArgs e)
        {
            string newProfileName = InputWindow.ShowDialog(this, Properties.Resources.NewProfile_Prompt, Properties.Resources.NewProfile_Caption);
            newProfileName = IO.PurifyFileName(newProfileName);

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
            newProfileName = IO.PurifyFileName(newProfileName);

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

        /// <summary>
        /// Selects the TreeViewItem under the mouse before its context menu
        /// opens. WPF's TreeView does not do this automatically on right-click
        /// (unlike ListView), so without this, right-clicking a different mod
        /// than the currently-selected one would still show the menu for the
        /// old selection.
        /// </summary>
        private void ModTreeViewItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var item = sender as TreeViewItem;
            if (item != null)
            {
                item.IsSelected = true;
                e.Handled = false;
            }
        }

        /// <summary>
        /// Refreshes the "Hide"/"Unhide" wording each time the context menu is
        /// opened, since it depends on the currently-selected mod's hidden
        /// state, which can differ each time the menu is invoked.
        /// </summary>
        private void ModItemContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            var selectedItem = this.ModTreeView.SelectedItem as ModItem;
            if (selectedItem == null) return;

            // Same namescope caveat as the rename menu item: this ContextMenu
            // lives in Window.Resources, so its x:Name'd children aren't
            // reachable via generated fields; look it up by resource key and
            // item index instead.
            var modContextMenu = (ContextMenu)sender;
            var toggleHideMenuItem = (MenuItem)modContextMenu.Items[4];

            bool isHidden = this.DisplayState.IsHidden(selectedItem.Node.ID);
            toggleHideMenuItem.Header = isHidden ? Properties.Resources.UnhideModMenuItem_Text : Properties.Resources.HideModMenuItem_Text;
        }

        private void ToggleHideModMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = this.ModTreeView.SelectedItem as ModItem;
            if (selectedItem == null) return;

            try
            {
                bool currentlyHidden = this.DisplayState.IsHidden(selectedItem.Node.ID);
                this.DisplayState.SetHidden(selectedItem.Node.ID, !currentlyHidden);
                this.DisplayState.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods"));

                // Unhiding never needs a rebuild (the mod is already visible,
                // since you can only reach this menu item on a mod that IS
                // currently shown). Hiding, though, removes this mod (and its
                // subtree) from the list -- unless "show hidden mods" is
                // checked, in which case it should stay put. Either way,
                // rebuilding is the simplest way to get a correct result.
                string modIDToRestore = currentlyHidden ? selectedItem.Node.ID : this.GetSelectedModID();
                this.suppressSelectionPersistence = true;
                try
                {
                    this.BuildTreeView();
                    this.SelectModByID(modIDToRestore);
                }
                finally
                {
                    this.suppressSelectionPersistence = false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.Dialog_Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OpenModFolderMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = this.ModTreeView.SelectedItem as ModItem;
            if (selectedItem == null) return;

            try
            {
                System.Diagnostics.Process.Start(selectedItem.Node.Directory);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.Dialog_Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RenameModMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = this.ModTreeView.SelectedItem as ModItem;
            if (selectedItem == null) return;

            string newName = InputWindow.ShowDialog(this, Properties.Resources.RenameMod_Prompt, Properties.Resources.RenameMod_Caption);
            if (String.IsNullOrWhiteSpace(newName)) return;
            newName = newName.Trim();

            try
            {
                selectedItem.Node.Name = newName;
                selectedItem.Node.Write(selectedItem.Node.Directory);

                // Update the already-loaded tree/UI in place rather than doing a
                // full BuildTreeView(): this mod's ID and position haven't
                // changed, only its display name, so there's no need to lose
                // and restore tree expansion/selection state for a full reload.
                selectedItem.Name = newName;
                if (selectedItem == this.ModTreeView.SelectedItem)
                {
                    this.ModsGroupBox.Header = String.Format(Properties.Resources.ModsGroupBox_HeaderWithName, selectedItem.Title);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.Dialog_Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChangeModIdMenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = this.ModTreeView.SelectedItem as ModItem;
            if (selectedItem == null) return;

            string oldID = selectedItem.Node.ID;
            string newID = InputWindow.ShowDialog(this, Properties.Resources.ChangeModId_Prompt, Properties.Resources.ChangeModId_Caption, oldID);
            if (String.IsNullOrWhiteSpace(newID)) return;
            newID = newID.Trim();

            if (newID == oldID) return;

            if (this.NodeTree.ContainsID(newID))
            {
                MessageBox.Show(String.Format(Properties.Resources.ModIdAlreadyExists, newID), Properties.Resources.Dialog_Title_Failure, MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Changing a mod's ID rewrites save-data folder names under every
            // profile and the "parent" field of every direct child mod (see
            // ModIdChanger for why); make sure the user understands the scope
            // of this before proceeding, since it touches files outside this
            // one mod's own folder.
            if (MessageBox.Show(String.Format(Properties.Resources.ConfirmChangeModId, selectedItem.Name, oldID, newID), Properties.Resources.Dialog_Title_Warning, MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                string profilesDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profiles");
                ModIdChanger.ChangeId(selectedItem.Node, newID, profilesDirectory);

                // Keep NodeTree.NodesDictionary consistent with the Node's new
                // ID (ModIdChanger mutates Node.ID in place but has no
                // knowledge of NodeTree's dictionary keying).
                this.NodeTree.ReKeyNode(oldID, selectedItem.Node);

                // If this mod (or one of its now-updated children) was the
                // "last selected mod" remembered in settings, that saved ID
                // is now stale; refresh it so a restart doesn't silently fail
                // to restore the selection.
                if (Properties.Settings.Default.LastModID == oldID)
                {
                    Properties.Settings.Default.LastModID = newID;
                    Properties.Settings.Default.Save();
                }

                // No tree structure changed (same parent/children, just a
                // different ID), so update the UI in place rather than doing
                // a full BuildTreeView().
                if (selectedItem == this.ModTreeView.SelectedItem)
                {
                    this.UpdateRunButtonStatus();
                }

                MessageBox.Show(Properties.Resources.ChangeModIdSucceeded, Properties.Resources.Dialog_Title_Info, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, Properties.Resources.Dialog_Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

                    // Clean up any leftover hidden-state entry for this mod so
                    // it doesn't linger in ModManagerState.json forever, and
                    // (however unlikely) can't cause a future mod that happens
                    // to reuse this ID to start out hidden unexpectedly.
                    if (this.DisplayState.IsHidden(selectedItem.Node.ID))
                    {
                        this.DisplayState.SetHidden(selectedItem.Node.ID, false);
                        this.DisplayState.Save(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods"));
                    }

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
                string incomingDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Incoming");
                try
                {
                    var fastZip = new ICSharpCode.SharpZipLib.Zip.FastZip();

                    // Will always overwrite if target filenames already exist
                    fastZip.ExtractZip(
                        openFileDialog.FileName,
                        incomingDirectory,
                        String.Empty);

                    // Read-only check: verifies the archive's mods could be merged into
                    // the current tree (no duplicate/unresolvable IDs) without actually
                    // mutating this.NodeTree or any of its existing Node instances.
                    int nodeCount = this.NodeTree.ValidateNodes(incomingDirectory);

                    if (nodeCount > 0)
                    {
                        // The archive already follows this tool's convention
                        // (top-level folder(s) each containing node.json + Files/):
                        // install as-is, same as before.
                        IO.CreateHardLinksOfFiles(incomingDirectory, System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods"));
                    }
                    else
                    {
                        // Most real-world mod archives were never packaged for this
                        // tool specifically, so they won't have a node.json at all.
                        // Let the user manually pick which folder's contents are the
                        // mod's files and fill in the node.json fields by hand,
                        // rather than just failing with "doesn't contain any nodes".
                        if (!this.RunManualInstallWizard(incomingDirectory, openFileDialog.FileName))
                        {
                            // User cancelled the wizard: nothing was installed.
                            return;
                        }
                    }

                    string modIDToRestore = this.GetSelectedModID();
                    this.suppressSelectionPersistence = true;
                    try
                    {
                        this.BuildTreeView();
                        this.SelectModByID(modIDToRestore);
                    }
                    finally
                    {
                        this.suppressSelectionPersistence = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, Properties.Resources.Dialog_Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    IO.ClearDirectory(incomingDirectory);
                }


            }
        }

        /// <summary>
        /// Shows the manual install wizard for an archive that doesn't already
        /// contain a node.json, and (if the user confirms) creates the new mod
        /// folder under Mods/ from the choices made in the wizard.
        /// Returns true if a mod was installed, false if the user cancelled.
        /// </summary>
        private bool RunManualInstallWizard(string incomingDirectory, string archiveFileName)
        {
            string archiveDisplayName = System.IO.Path.GetFileNameWithoutExtension(archiveFileName);
            var wizard = new InstallModWizard(this, incomingDirectory, archiveDisplayName, this.NodeTree.NodesDictionary.Values);
            if (wizard.ShowDialog() != true)
            {
                return false;
            }

            string modsDirectory = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Mods");
            string newModDirectory = this.CreateUniqueModDirectory(modsDirectory, wizard.ModName);

            var node = new Node
            {
                ID = wizard.ModID,
                Name = wizard.ModName,
                MainExecutable = wizard.MainExecutable,
                Arguments = wizard.Arguments,
                ParentID = wizard.ParentID,
                Compatibility = wizard.Compatibility
            };

            System.IO.Directory.CreateDirectory(System.IO.Path.Combine(newModDirectory, "Files"));
            IO.CreateHardLinksOfFiles(wizard.SelectedFolderPath, System.IO.Path.Combine(newModDirectory, "Files"));
            node.Write(newModDirectory);

            return true;
        }

        /// <summary>
        /// Picks a folder name for a new mod under Mods/, based on its name,
        /// disambiguating with a numeric suffix if a folder with that name
        /// already exists (e.g. because two different mods share a display
        /// name, or a previous mod using that folder name was deleted and
        /// re-installed). The folder name is purely a filesystem detail; the
        /// mod's actual identity is its ID, stored in node.json.
        /// </summary>
        private string CreateUniqueModDirectory(string modsDirectory, string modName)
        {
            string baseName = IO.PurifyFileName(modName);
            if (String.IsNullOrWhiteSpace(baseName)) baseName = "Mod";

            string candidate = System.IO.Path.Combine(modsDirectory, baseName);
            int suffix = 2;
            while (System.IO.Directory.Exists(candidate))
            {
                candidate = System.IO.Path.Combine(modsDirectory, baseName + " (" + suffix + ")");
                suffix++;
            }
            System.IO.Directory.CreateDirectory(candidate);
            return candidate;
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
