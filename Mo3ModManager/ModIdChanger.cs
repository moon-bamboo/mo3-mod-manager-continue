using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mo3ModManager
{
    /// <summary>
    /// Changes a mod's ID, cascading the change everywhere the old ID is
    /// referenced. A mod's ID is not just a label -- it's used as:
    ///   1. The key other mods use to declare it as their parent (node.json's
    ///      "parent" field on direct child mods).
    ///   2. The folder name under each Profile's directory where that mod's
    ///      save data lives (Profiles/&lt;profile&gt;/&lt;modID&gt;/, see
    ///      MainWindow.RunButton_Click / ModProcessManager).
    /// Changing only the mod's own node.json without updating these would
    /// silently orphan child mods (their parent reference would point to a
    /// non-existent ID) and, more importantly, silently disconnect all
    /// existing save data for every profile (the game would just start a
    /// fresh save under the new ID's folder, with the old save data left
    /// behind on disk but never loaded again).
    /// </summary>
    static class ModIdChanger
    {
        /// <summary>
        /// Changes Node's ID to NewID, updating:
        ///  - Node's own node.json
        ///  - the "parent" field of every direct child of Node (in NodeTree)
        ///  - the save-data folder name under every profile in ProfilesDirectory
        /// This mutates the Node object and its direct children in place (so
        /// the in-memory NodeTree/UI stay consistent with what's on disk).
        ///
        /// Attempts to leave the filesystem in its original state if any step
        /// fails partway through, by undoing the profile folder renames that
        /// already succeeded before re-throwing. This is a best-effort
        /// rollback (e.g. it cannot undo a node.json write that itself failed
        /// midway), not a transactional guarantee.
        /// </summary>
        public static void ChangeId(Node Node, string NewID, string ProfilesDirectory)
        {
            string oldID = Node.ID;
            if (NewID == oldID) return;

            if (String.IsNullOrWhiteSpace(NewID))
            {
                throw new Exception("The new ID cannot be empty.");
            }

            // Rename this mod's save-data folder under every existing profile
            // first, while we can still identify it by the old ID. Track
            // which ones we've actually renamed so we can roll them back if a
            // later step fails.
            var renamedProfileDirs = new List<Tuple<string, string>>(); // (from, to)
            try
            {
                if (Directory.Exists(ProfilesDirectory))
                {
                    foreach (var profileDir in Directory.GetDirectories(ProfilesDirectory))
                    {
                        string oldSaveDir = Path.Combine(profileDir, oldID);
                        if (!Directory.Exists(oldSaveDir)) continue;

                        string newSaveDir = Path.Combine(profileDir, NewID);
                        if (Directory.Exists(newSaveDir))
                        {
                            throw new Exception("Profile \"" + Path.GetFileName(profileDir) + "\" already has save data under the new ID \"" + NewID + "\". Cannot change ID without risking overwriting it.");
                        }

                        Directory.Move(oldSaveDir, newSaveDir);
                        renamedProfileDirs.Add(Tuple.Create(oldSaveDir, newSaveDir));
                    }
                }

                // Update every direct child's "parent" field. Indirect
                // descendants are untouched: their own "parent" field points
                // at their immediate parent's ID, not at this mod's ID, so
                // they're unaffected by this mod's ID changing.
                var updatedChildren = new List<Node>();
                try
                {
                    foreach (var child in Node.Childs)
                    {
                        child.ParentID = NewID;
                        child.Write(child.Directory);
                        updatedChildren.Add(child);
                    }
                }
                catch (Exception)
                {
                    // Roll back the children whose node.json was already
                    // rewritten before the failure, so we don't leave some
                    // children pointing at the new ID while the mod itself
                    // still has the old one.
                    foreach (var child in updatedChildren)
                    {
                        child.ParentID = oldID;
                        SafeWrite(child);
                    }
                    throw;
                }

                // Finally, update the mod's own node.json and in-memory ID.
                Node.ID = NewID;
                try
                {
                    Node.Write(Node.Directory);
                }
                catch (Exception)
                {
                    Node.ID = oldID;
                    foreach (var child in updatedChildren)
                    {
                        child.ParentID = oldID;
                        SafeWrite(child);
                    }
                    throw;
                }
            }
            catch (Exception)
            {
                // Roll back any profile save-data folders already renamed.
                foreach (var rename in renamedProfileDirs)
                {
                    try
                    {
                        if (Directory.Exists(rename.Item2) && !Directory.Exists(rename.Item1))
                        {
                            Directory.Move(rename.Item2, rename.Item1);
                        }
                    }
                    catch (Exception rollbackEx)
                    {
                        System.Diagnostics.Trace.WriteLine("[Warn] Failed to roll back profile save folder rename \"" + rename.Item2 + "\" -> \"" + rename.Item1 + "\": " + rollbackEx.Message);
                    }
                }
                throw;
            }
        }

        /// <summary>
        /// Best-effort node.json write used only while already unwinding from
        /// another failure: logs rather than throws, so a rollback failure
        /// doesn't replace the original (more relevant) exception.
        /// </summary>
        private static void SafeWrite(Node node)
        {
            try
            {
                node.Write(node.Directory);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("[Warn] Failed to roll back node.json for \"" + node.Directory + "\": " + ex.Message);
            }
        }
    }
}
