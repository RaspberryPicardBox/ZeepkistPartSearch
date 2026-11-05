using System;
using System.Collections.Generic;
using System.Globalization;
using I2.Loc;
using UnityEngine;
using ZeepkistPartSearch.Search;

namespace ZeepkistPartSearch.Unity
{
    /// <summary>
    /// Turns the game's own palette objects into the flat, Unity-free <see cref="PartCatalog"/>
    /// the search runs on.
    ///
    /// The folder structure is read straight from the game's <see cref="BlocksFolder"/> trees,
    /// so whatever parts the player (or another mod) has, including custom folders, are
    /// searchable and nothing is hard-coded. <see cref="LocalizationManager"/> is used for the
    /// same names the inspector displays, so a Dutch or German player searches in their own
    /// language.
    /// </summary>
    internal static class PartCatalogBuilder
    {
        public static BuiltPartCatalog BuildBlocks(BlockList list)
        {
            if (list == null || list.globalBlocksFolder == null)
            {
                return null;
            }

            Dictionary<BlocksFolder, PartFolder> folders = new Dictionary<BlocksFolder, PartFolder>();
            Dictionary<string, PartVisuals> visuals = NewVisuals();
            Dictionary<string, UnityEngine.Object> sources = NewSources();
            int index = 0;

            PartFolder root = BuildBlockFolder(list.globalBlocksFolder, string.Empty, true,
                folders, visuals, sources, ref index);
            return new BuiltPartCatalog(new PartCatalog(root), folders, visuals, sources);
        }

        public static BuiltPartCatalog BuildTrees(BlockList list)
        {
            if (list == null || list.globalTreeFolder == null)
            {
                return null;
            }

            Dictionary<BlocksFolder, PartFolder> folders = new Dictionary<BlocksFolder, PartFolder>();
            Dictionary<string, PartVisuals> visuals = NewVisuals();
            Dictionary<string, UnityEngine.Object> sources = NewSources();
            int index = 0;

            PartFolder root = BuildTreeFolder(list.globalTreeFolder, string.Empty, true,
                folders, visuals, sources, ref index);
            return new BuiltPartCatalog(new PartCatalog(root), folders, visuals, sources);
        }

        public static BuiltPartCatalog BuildPaints(BlockList list)
        {
            if (list == null || list.globalPaintFolder == null)
            {
                return null;
            }

            Dictionary<BlocksFolder, PartFolder> folders = new Dictionary<BlocksFolder, PartFolder>();
            Dictionary<string, PartVisuals> visuals = NewVisuals();
            Dictionary<string, UnityEngine.Object> sources = NewSources();
            int index = 0;

            PartFolder root = BuildPaintFolder(list.globalPaintFolder, string.Empty, true,
                folders, visuals, sources, ref index);
            return new BuiltPartCatalog(new PartCatalog(root), folders, visuals, sources);
        }

        // ---------------------------------------------------------------- blocks

        private static PartFolder BuildBlockFolder(BlocksFolder folder, string parentPath, bool isRoot,
            Dictionary<BlocksFolder, PartFolder> folders, Dictionary<string, PartVisuals> visuals,
            Dictionary<string, UnityEngine.Object> sources, ref int index)
        {
            string path = PathFor(folder, parentPath, isRoot);
            PartFolder node = new PartFolder(FolderName(folder), path);
            folders[folder] = node;

            List<BlockProperties> blocks = folder.blocks;
            for (int i = 0; i < blocks.Count && index < PartCatalog.MaxEntries; i++)
            {
                BlockProperties block = blocks[i];
                if (block == null)
                {
                    continue;
                }

                string key = Key("block", ref index);
                string term = block.bridge != null ? block.bridge.localizedName : null;

                node.Items.Add(new PartEntry(key, PartKind.Block, block.blockID,
                    Translate(term), term, string.Empty, path));
                visuals[key] = new PartVisuals(block.thumbnail, block.thumbnail_icon);
            }

            List<BlocksFolder> children = folder.folders;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] != null)
                {
                    node.Children.Add(BuildBlockFolder(children[i], path, false,
                        folders, visuals, sources, ref index));
                }
            }

            return node;
        }

        // ---------------------------------------------------------------- trees

        private static PartFolder BuildTreeFolder(BlocksFolder folder, string parentPath, bool isRoot,
            Dictionary<BlocksFolder, PartFolder> folders, Dictionary<string, PartVisuals> visuals,
            Dictionary<string, UnityEngine.Object> sources, ref int index)
        {
            string path = PathFor(folder, parentPath, isRoot);
            PartFolder node = new PartFolder(FolderName(folder), path);
            folders[folder] = node;

            List<LEV_TreeGunTreeHolder> trees = folder.trees;
            for (int i = 0; i < trees.Count && index < PartCatalog.MaxEntries; i++)
            {
                LEV_TreeGunTreeHolder tree = trees[i];
                if (tree == null)
                {
                    continue;
                }

                string key = Key("tree", ref index);
                node.Items.Add(new PartEntry(key, PartKind.Tree, -1,
                    Translate(tree.localizedName), tree.localizedName, tree.trees_ID, path));
                visuals[key] = new PartVisuals(tree.thumb, tree.thumbnail_icon);
                sources[key] = tree;
            }

            List<BlocksFolder> children = folder.folders;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] != null)
                {
                    node.Children.Add(BuildTreeFolder(children[i], path, false,
                        folders, visuals, sources, ref index));
                }
            }

            return node;
        }

        // ---------------------------------------------------------------- paints

        private static PartFolder BuildPaintFolder(BlocksFolder folder, string parentPath, bool isRoot,
            Dictionary<BlocksFolder, PartFolder> folders, Dictionary<string, PartVisuals> visuals,
            Dictionary<string, UnityEngine.Object> sources, ref int index)
        {
            string path = PathFor(folder, parentPath, isRoot);
            PartFolder node = new PartFolder(FolderName(folder), path);
            folders[folder] = node;

            List<MaterialHolder> materials = folder.materials;
            for (int i = 0; i < materials.Count && index < PartCatalog.MaxEntries; i++)
            {
                MaterialHolder material = materials[i];
                if (material == null)
                {
                    continue;
                }

                string key = Key("paint", ref index);
                node.Items.Add(new PartEntry(key, PartKind.Paint, material.materialID,
                    Translate(material.localizedName), material.localizedName, string.Empty, path));
                visuals[key] = new PartVisuals(material.thumbnail, material.thumbnail_icon);
                sources[key] = material;
            }

            List<BlocksFolder> children = folder.folders;
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] != null)
                {
                    node.Children.Add(BuildPaintFolder(children[i], path, false,
                        folders, visuals, sources, ref index));
                }
            }

            return node;
        }

        // ---------------------------------------------------------------- helpers

        private static Dictionary<string, PartVisuals> NewVisuals()
        {
            return new Dictionary<string, PartVisuals>(StringComparer.Ordinal);
        }

        private static Dictionary<string, UnityEngine.Object> NewSources()
        {
            return new Dictionary<string, UnityEngine.Object>(StringComparer.Ordinal);
        }

        private static string Key(string prefix, ref int index)
        {
            index++;
            return prefix + ":" + index.ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// The palette root contributes no path segment, so a root item reads as "Straight"
        /// rather than "Blocks &gt; Straight".
        /// </summary>
        private static string PathFor(BlocksFolder folder, string parentPath, bool isRoot)
        {
            if (isRoot)
            {
                return string.Empty;
            }

            string name = FolderName(folder);
            if (string.IsNullOrEmpty(parentPath))
            {
                return name;
            }

            if (string.IsNullOrEmpty(name))
            {
                return parentPath;
            }

            return parentPath + " > " + name;
        }

        private static string FolderName(BlocksFolder folder)
        {
            if (folder == null)
            {
                return string.Empty;
            }

            if (folder.useLocalizedFolderName && !string.IsNullOrEmpty(folder.localizedName))
            {
                return Translate(folder.localizedName);
            }

            return Translate("Block_Folder");
        }

        /// <summary>
        /// Localized text for a term, falling back to the term itself. Never throws: this runs
        /// while the inspector is being built, and a localization hiccup must not stop the mod.
        /// </summary>
        private static string Translate(string term)
        {
            if (string.IsNullOrEmpty(term))
            {
                return string.Empty;
            }

            try
            {
                string translated = I2.Loc.LocalizationManager.GetTranslation(term);
                return string.IsNullOrEmpty(translated) ? term : translated;
            }
            catch (Exception)
            {
                return term;
            }
        }
    }
}

