using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mo3ModManager
{
    /// <summary>
    /// Persists per-mod UI display preferences that are local to this
    /// installation and not part of a mod's own identity -- currently just
    /// "hidden" status. Deliberately NOT stored in node.json: node.json
    /// describes the mod itself (what a mod author or the install wizard
    /// would write), whereas this is a personal "I don't want to see this in
    /// my list" preference that has nothing to do with the mod's content.
    /// Stored as a small JSON file directly under Mods/, so it stays right
    /// next to the mods it refers to. This is safe to place there because
    /// NodeTree only scans Mods/'s SUBdirectories (DirectoryInfo.GetDirectories),
    /// never its files, so this file is simply ignored by mod loading.
    /// </summary>
    class ModDisplayState
    {
        private const string FileName = "ModManagerState.json";

        public HashSet<string> HiddenModIDs { get; private set; }

        public ModDisplayState()
        {
            this.HiddenModIDs = new HashSet<string>();
        }

        public bool IsHidden(string modID)
        {
            return this.HiddenModIDs.Contains(modID);
        }

        public void SetHidden(string modID, bool hidden)
        {
            if (hidden)
            {
                this.HiddenModIDs.Add(modID);
            }
            else
            {
                this.HiddenModIDs.Remove(modID);
            }
        }

        /// <summary>
        /// Loads display state from Mods/ModManagerState.json. Returns an empty
        /// (nothing hidden) state if the file doesn't exist, is corrupted, or
        /// can't be read -- a missing/broken preferences file should never
        /// prevent the mod list itself from loading.
        /// </summary>
        public static ModDisplayState Load(string modsDirectory)
        {
            string path = Path.Combine(modsDirectory, FileName);
            var state = new ModDisplayState();
            if (!File.Exists(path)) return state;

            try
            {
                string json = File.ReadAllText(path);
                var raw = Newtonsoft.Json.JsonConvert.DeserializeAnonymousType(json, new { hiddenModIds = new List<string>() });
                if (raw != null && raw.hiddenModIds != null)
                {
                    state.HiddenModIDs = new HashSet<string>(raw.hiddenModIds);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.WriteLine("[Warn] Failed to load \"" + path + "\": " + ex.Message);
            }
            return state;
        }

        public void Save(string modsDirectory)
        {
            string path = Path.Combine(modsDirectory, FileName);
            var raw = new { hiddenModIds = this.HiddenModIDs.ToList() };
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(raw, Newtonsoft.Json.Formatting.Indented);
            File.WriteAllText(path, json);
        }
    }
}
