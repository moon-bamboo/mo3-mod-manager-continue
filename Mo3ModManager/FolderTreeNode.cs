using System.Collections.Generic;
using System.Linq;

namespace Mo3ModManager
{
    /// <summary>
    /// A lightweight tree node representing a folder on disk, used by
    /// InstallModWizard to let the user pick which folder's contents should
    /// become the mod's Files/ content. Only directories are represented
    /// (files are irrelevant to this choice).
    /// </summary>
    class FolderTreeNode
    {
        public string Name { get; private set; }

        /// <summary>The full path on disk this node represents.</summary>
        public string FullPath { get; private set; }

        public List<FolderTreeNode> Children { get; private set; }

        public FolderTreeNode(string fullPath, string name)
        {
            this.FullPath = fullPath;
            this.Name = name;
            this.Children = new List<FolderTreeNode>();
        }

        /// <summary>
        /// Builds a folder tree rooted at the given directory. The root node's
        /// Name is set to displayName (e.g. the archive's file name), so the
        /// user can tell at a glance that selecting the root means "use the
        /// whole archive as-is".
        /// </summary>
        public static FolderTreeNode BuildTree(string rootPath, string rootDisplayName)
        {
            var root = new FolderTreeNode(rootPath, rootDisplayName);
            AddChildren(root);
            return root;
        }

        private static void AddChildren(FolderTreeNode node)
        {
            foreach (var subDir in System.IO.Directory.GetDirectories(node.FullPath).OrderBy(d => d))
            {
                var child = new FolderTreeNode(subDir, System.IO.Path.GetFileName(subDir));
                node.Children.Add(child);
                AddChildren(child);
            }
        }
    }
}
