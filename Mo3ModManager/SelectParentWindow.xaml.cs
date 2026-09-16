using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace Mo3ModManager
{
    /// <summary>
    /// Lets the user pick a new parent mod for an existing mod, from a
    /// dropdown rather than free-text entry (unlike other node.json fields):
    /// typing an arbitrary ID here could silently create a dangling reference
    /// (a typo'd ID that doesn't exist) or a cycle (a mod becoming its own
    /// ancestor), either of which NodeTree can't represent. Restricting the
    /// choices to a caller-supplied valid candidate list makes both classes
    /// of mistake impossible by construction.
    /// </summary>
    public partial class SelectParentWindow : Window
    {
        /// <summary>Display wrapper for the ComboBox.</summary>
        private class ParentOption
        {
            public string Display { get; set; }
            public string ID { get; set; }
        }

        /// <summary>The selected parent's ID (empty string for "no parent / root mod"). Valid after ShowDialog() returns true.</summary>
        public string SelectedParentID { get; private set; }

        public SelectParentWindow(Window owner, string prompt, string caption, IEnumerable<Node> validCandidates, string currentParentID)
        {
            this.InitializeComponent();
            this.Owner = owner;
            this.Title = caption;
            this.TextBlock.Text = prompt;

            this.ParentComboBox.Items.Add(new ParentOption { Display = Properties.Resources.InstallWizard_NoParent, ID = String.Empty });
            foreach (var node in validCandidates.OrderBy(n => n.Name))
            {
                this.ParentComboBox.Items.Add(new ParentOption { Display = node.Name, ID = node.ID });
            }

            var currentOption = this.ParentComboBox.Items.Cast<ParentOption>().FirstOrDefault(o => o.ID == (currentParentID ?? String.Empty));
            this.ParentComboBox.SelectedItem = currentOption ?? this.ParentComboBox.Items[0];
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            var selected = this.ParentComboBox.SelectedItem as ParentOption;
            this.SelectedParentID = selected == null ? String.Empty : selected.ID;
            this.DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
