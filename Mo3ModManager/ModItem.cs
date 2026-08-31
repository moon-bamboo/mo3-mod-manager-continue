using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Mo3ModManager
{
    public class ModItem : INotifyPropertyChanged
    {
        private string name;
        public string Name {
            get {
                return this.name;
            }
            set {
                this.name = value;
                NotifyPropertyChanged("Name");
                NotifyPropertyChanged("Title");
            }
        }

        public string Title { get { return this.isHidden ? this.Name + " " + Properties.Resources.HiddenModSuffix : this.Name; } }

        // Whether this mod is currently marked hidden (see ModDisplayState).
        // Note this is independent of whether it's actually present in the
        // tree: when "show hidden mods" is checked, hidden mods are included
        // but still flagged here so the UI can visually distinguish them
        // (grayed out + "(hidden)" suffix) rather than looking identical to
        // a normal mod.
        private bool isHidden;
        public bool IsHidden {
            get {
                return this.isHidden;
            }
            set {
                this.isHidden = value;
                NotifyPropertyChanged("IsHidden");
                NotifyPropertyChanged("Title");
            }
        }

        // Bound two-way to TreeViewItem.IsSelected via the ItemContainerStyle in
        // MainWindow.xaml. This is the standard workaround for TreeView.SelectedItem
        // being read-only: setting this property programmatically (e.g. to restore
        // a previous selection after rebuilding the tree) selects the corresponding
        // TreeViewItem once its container exists.
        private bool isSelected;
        public bool IsSelected {
            get {
                return this.isSelected;
            }
            set {
                this.isSelected = value;
                NotifyPropertyChanged("IsSelected");
            }
        }

        public System.Collections.ObjectModel.ObservableCollection<ModItem> Items { get; set; }

        //the following properties is not used by UI
        public Node Node { get; set; }
        public ModItem Parent { get; set; }

        public ModItem()
        {
            this.Items = new System.Collections.ObjectModel.ObservableCollection<ModItem>();
            this.Parent = null;
            this.Node = null;
        }
        public ModItem(Node Node)
        {
            this.Items = new System.Collections.ObjectModel.ObservableCollection<ModItem>();
            this.Name = Node.Name;
            this.Node = Node;
            foreach (var child in Node.Childs)
            {
                var childItem = new ModItem(child)
                {
                    Parent = this
                };
                this.Items.Add(childItem);
            }
        }

        /// <summary>
        /// Builds a ModItem tree like ModItem(Node), but skips any descendant
        /// Node for which shouldInclude returns false -- and, since a skipped
        /// Node's own children are therefore never visited either, this
        /// naturally cascades: hiding a mod hides its entire subtree too,
        /// without needing to mark each descendant as hidden individually.
        /// Used to filter out mods the user has hidden from the list.
        /// isHidden flags each included ModItem's IsHidden property (used for
        /// visually distinguishing hidden-but-shown mods when "show hidden
        /// mods" is checked); it's independent of shouldInclude since a mod
        /// can be included yet still be hidden in that scenario.
        /// </summary>
        public ModItem(Node Node, Func<Node, bool> shouldInclude, Func<Node, bool> isHidden)
        {
            this.Items = new System.Collections.ObjectModel.ObservableCollection<ModItem>();
            this.Name = Node.Name;
            this.Node = Node;
            this.IsHidden = isHidden(Node);
            foreach (var child in Node.Childs)
            {
                if (!shouldInclude(child)) continue;

                var childItem = new ModItem(child, shouldInclude, isHidden)
                {
                    Parent = this
                };
                this.Items.Add(childItem);
            }
        }



        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged(String propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
