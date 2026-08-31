using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Mo3ModManager
{
    /// <summary>
    /// Lets the user manually configure a mod when the extracted archive
    /// doesn't already contain a "node.json" (which is the common case: most
    /// mod archives found in the wild were never packaged specifically for
    /// this tool). The user picks which folder's contents should become the
    /// mod's Files/, and fills in the node.json fields by hand.
    /// </summary>
    public partial class InstallModWizard : Window
    {
        /// <summary>Display wrapper for the "parent mod" ComboBox.</summary>
        private class ParentOption
        {
            public string Display { get; set; }
            public string ID { get; set; }
        }

        /// <summary>The folder the user selected as the mod's content root. Valid after ShowDialog() returns true.</summary>
        public string SelectedFolderPath { get; private set; }

        public string ModName { get; private set; }
        public string ModID { get; private set; }
        public string MainExecutable { get; private set; }
        public string Arguments { get; private set; }
        public string ParentID { get; private set; }
        public string Compatibility { get; private set; }

        private readonly string rootPath;

        // Default Windows compatibility layer flags: "~" marks this as a custom
        // layer (rather than an "emulate old Windows version" mode), RUNASADMIN
        // requests elevation, and HIGHDPIAWARE tells Windows this app handles its
        // own DPI scaling (avoids blurry upscaling on high-DPI displays). This
        // matches what mo3.club's own official mod packages use for their
        // Red Alert 2 engine-based executables, so it's a sensible default for
        // this game specifically -- though it may not fit every mod.
        private const string DefaultCompatibilityFlags = "~ RUNASADMIN HIGHDPIAWARE";

        public InstallModWizard(Window owner, string rootPath, string archiveDisplayName, IEnumerable<Node> existingNodes)
        {
            this.InitializeComponent();
            this.Owner = owner;
            this.rootPath = rootPath;

            this.ApplyLocalizedText();

            var rootFolderNode = FolderTreeNode.BuildTree(rootPath, archiveDisplayName);
            this.FolderTreeView.Items.Add(rootFolderNode);

            this.IdTextBox.Text = Guid.NewGuid().ToString();
            this.CompatibilityTextBox.Text = DefaultCompatibilityFlags;

            this.ParentComboBox.Items.Add(new ParentOption { Display = Properties.Resources.InstallWizard_NoParent, ID = String.Empty });
            foreach (var node in existingNodes.OrderBy(n => n.Name))
            {
                this.ParentComboBox.Items.Add(new ParentOption { Display = node.Name, ID = node.ID });
            }
            this.ParentComboBox.SelectedIndex = 0;

            // Pre-select the root folder so the executable dropdown is
            // populated even if the user never touches the tree (the common
            // case where the whole archive is the mod's content).
            this.SelectedFolderPath = rootPath;
            this.RefreshExecutableChoices(rootPath);
        }

        private void ApplyLocalizedText()
        {
            this.Title = Properties.Resources.InstallWizard_Title;
            this.IntroText.Text = Properties.Resources.InstallWizard_Intro;
            this.FolderGroupBox.Header = Properties.Resources.InstallWizard_FolderGroupHeader;
            this.DetailsGroupBox.Header = Properties.Resources.InstallWizard_DetailsGroupHeader;
            this.NameLabel.Text = Properties.Resources.InstallWizard_NameLabel;
            this.IdLabel.Text = Properties.Resources.InstallWizard_IdLabel;
            this.MainExecutableLabel.Text = Properties.Resources.InstallWizard_MainExecutableLabel;
            this.ArgumentsLabel.Text = Properties.Resources.InstallWizard_ArgumentsLabel;
            this.ParentLabel.Text = Properties.Resources.InstallWizard_ParentLabel;
            this.CompatibilityLabel.Text = Properties.Resources.InstallWizard_CompatibilityLabel;
            this.OKButton.Content = Properties.Resources.InputWindow_OK;
            this.CancelButton.Content = Properties.Resources.InputWindow_Cancel;
        }

        private void FolderTreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            var selected = this.FolderTreeView.SelectedItem as FolderTreeNode;
            if (selected == null) return;

            this.SelectedFolderPath = selected.FullPath;
            this.RefreshExecutableChoices(selected.FullPath);
        }

        /// <summary>
        /// Repopulates the "main executable" combo box with every .exe found
        /// (recursively) under the given folder, shown as paths relative to
        /// that folder -- this is exactly the format node.json's
        /// main_executable field expects.
        /// </summary>
        private void RefreshExecutableChoices(string folderPath)
        {
            string previousSelection = this.MainExecutableComboBox.Text;

            this.MainExecutableComboBox.Items.Clear();
            try
            {
                foreach (var exeFullPath in System.IO.Directory.GetFiles(folderPath, "*.exe", System.IO.SearchOption.AllDirectories).OrderBy(p => p))
                {
                    string relative = exeFullPath.Substring(folderPath.Length + 1);
                    this.MainExecutableComboBox.Items.Add(relative);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("[Warn] Failed to scan for executables under \"" + folderPath + "\": " + ex.Message);
            }

            this.MainExecutableComboBox.Text = previousSelection;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(this.NameTextBox.Text))
            {
                MessageBox.Show(this, Properties.Resources.InstallWizard_NameRequired, Properties.Resources.Dialog_Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (String.IsNullOrWhiteSpace(this.IdTextBox.Text))
            {
                MessageBox.Show(this, Properties.Resources.InstallWizard_IdRequired, Properties.Resources.Dialog_Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (this.SelectedFolderPath == null)
            {
                MessageBox.Show(this, Properties.Resources.InstallWizard_FolderRequired, Properties.Resources.Dialog_Title_Error, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            this.ModName = this.NameTextBox.Text.Trim();
            this.ModID = this.IdTextBox.Text.Trim();
            this.MainExecutable = this.MainExecutableComboBox.Text == null ? String.Empty : this.MainExecutableComboBox.Text.Trim();
            this.Arguments = this.ArgumentsTextBox.Text.Trim();
            this.Compatibility = this.CompatibilityTextBox.Text.Trim();

            var selectedParent = this.ParentComboBox.SelectedItem as ParentOption;
            this.ParentID = selectedParent == null ? String.Empty : selectedParent.ID;

            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
