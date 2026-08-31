using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mo3ModManager
{
    class NodeTree
    {
        public Dictionary<string, Node> NodesDictionary { get; private set; }
        public List<Node> RootNodes { get; private set; }


        private List<Node> GetNodesFromDirectory(string Directory)
        {
            System.IO.DirectoryInfo modsParentFolder = new System.IO.DirectoryInfo(Directory);
            System.IO.DirectoryInfo[] modsFolders = modsParentFolder.GetDirectories();


            List<Node> nodes = new List<Node>();
            //Find "node.json" file in the folders and try to parse them.
            foreach (var modFolder in modsFolders)
            {
                // node.json must exist
                var nodeFiles = modFolder.GetFiles("node.json", System.IO.SearchOption.TopDirectoryOnly);
                System.Diagnostics.Debug.Assert(nodeFiles.Length <= 1);
                if (nodeFiles.Length == 0) continue;
                var nodeFile = nodeFiles[0];

                // Files folder must exist
                var filesFolders = modFolder.GetDirectories("Files", System.IO.SearchOption.TopDirectoryOnly);
                System.Diagnostics.Debug.Assert(filesFolders.Length <= 1);
                if (filesFolders.Length == 0) continue;

                //parse
                try
                {
                    Node node = Node.Parse(modFolder.FullName);
                    nodes.Add(node);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine("[Warn] Failed to parse mod at \"" + modFolder.FullName + "\": " + ex.Message);
                    continue;
                }


            }
            return nodes;
        }

        /// <summary>
        /// Checks whether the given nodes could be merged into this tree without
        /// conflicts (duplicate IDs, either against existing nodes or among
        /// themselves; or parent references that cannot be resolved against
        /// existing nodes or the batch itself).
        /// This is a read-only check: it does not mutate this tree, nor does it
        /// assign Parent/Childs on any Node instance (existing or new). This
        /// matters because Node objects are shared by reference whenever a
        /// NodeTree's contents are reused elsewhere (see ValidateNodes below),
        /// so any mutation here would otherwise leak into that other context.
        /// Throws if a conflict is found.
        /// </summary>
        private void ValidateNodesCanBeMerged(List<Node> Nodes)
        {
            var idsInBatch = new HashSet<string>();
            foreach (var node in Nodes)
            {
                if (NodesDictionary.ContainsKey(node.ID))
                {
                    throw new Exception("Mod \"" + node.Name + "\" (" + node.Directory + ") has a duplicate ID \"" + node.ID + "\", which is already used by mod \"" + NodesDictionary[node.ID].Name + "\" (" + NodesDictionary[node.ID].Directory + "). Please check node.json of these mods.");
                }
                if (!idsInBatch.Add(node.ID))
                {
                    throw new Exception("Mod \"" + node.Name + "\" (" + node.Directory + ") has a duplicate ID \"" + node.ID + "\", which is used by another mod in the same batch. Please check node.json of these mods.");
                }
            }

            foreach (var node in Nodes)
            {
                if (!node.IsRoot)
                {
                    if (!NodesDictionary.ContainsKey(node.ParentID) && !idsInBatch.Contains(node.ParentID))
                    {
                        throw new Exception("Mod \"" + node.Name + "\" (" + node.Directory + ") refers to a parent mod with ID \"" + node.ParentID + "\", but no such mod was found. Please check node.json of this mod.");
                    }
                }
            }
        }

        private void BuildTree(List<Node> Nodes)
        {
            // Validate before mutating anything, so a conflict partway through
            // the batch can never leave this tree (or any Node in it) in a
            // partially-modified state.
            this.ValidateNodesCanBeMerged(Nodes);

            foreach (var node in Nodes)
            {
                NodesDictionary[node.ID] = node;
            }

            foreach (var node in Nodes)
            {
                if (!node.IsRoot)
                {
                    node.Parent = NodesDictionary[node.ParentID];
                    node.Parent.Childs.Add(node);
                }
                else
                {
                    RootNodes.Add(node);
                    node.Parent = null;
                }
            }

        }

        public NodeTree()
        {
            this.NodesDictionary = new Dictionary<string, Node>();
            this.RootNodes = new List<Node>();
        }

        public int Count() {
            return this.NodesDictionary.Count();
        }

        public void AddNodes(string Directory)
        {
            var nodes = GetNodesFromDirectory(Directory);
            BuildTree(nodes);
        }

        /// <summary>
        /// Checks whether the nodes found in the given directory could be added
        /// to this tree without conflicts, without actually adding them and
        /// without mutating this tree or any of its existing Node instances.
        /// Returns the number of valid nodes found in the directory (0 if none).
        /// Throws if a conflict is found (see ValidateNodesCanBeMerged).
        /// </summary>
        public int ValidateNodes(string Directory)
        {
            var nodes = GetNodesFromDirectory(Directory);
            this.ValidateNodesCanBeMerged(nodes);
            return nodes.Count;
        }

        public void RemoveNode(Node OldNode)
        {
            //only leaf node is allowed to be removed
            System.Diagnostics.Debug.Assert(OldNode.Childs.Count == 0);

            if (OldNode.Parent != null)
            {
                OldNode.Parent.Childs.Remove(OldNode);
            }
            else
            {
                this.RootNodes.Remove(OldNode);
            }

            this.NodesDictionary.Remove(OldNode.ID);
        }

    }
}
