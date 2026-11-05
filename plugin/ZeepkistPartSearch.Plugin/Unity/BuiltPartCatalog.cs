using System;
using System.Collections.Generic;
using UnityEngine;
using ZeepkistPartSearch.Search;

namespace ZeepkistPartSearch.Unity
{
    /// <summary>
    /// A built snapshot of one palette: the searchable entries, the folder each entry lives
    /// in (so "current folder" scope can be resolved), and the Unity references the result
    /// buttons need to draw themselves and to be clicked.
    /// </summary>
    internal sealed class BuiltPartCatalog
    {
        public BuiltPartCatalog(PartCatalog catalog,
            Dictionary<BlocksFolder, PartFolder> folderBySource,
            Dictionary<string, PartVisuals> visuals,
            Dictionary<string, UnityEngine.Object> sources)
        {
            Catalog = catalog;
            FolderBySource = folderBySource;
            Visuals = visuals;
            Sources = sources;
        }

        public PartCatalog Catalog { get; }

        /// <summary>Game folder object -> the node it was flattened into.</summary>
        public Dictionary<BlocksFolder, PartFolder> FolderBySource { get; }

        public Dictionary<string, PartVisuals> Visuals { get; }

        /// <summary>Key -> the tree holder or material the game needs to actually select it.</summary>
        public Dictionary<string, UnityEngine.Object> Sources { get; }
    }
}
