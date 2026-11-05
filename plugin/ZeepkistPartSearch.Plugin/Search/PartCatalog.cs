using System.Collections.Generic;

namespace ZeepkistPartSearch.Search
{
    /// <summary>
    /// A flattened snapshot of a parts palette, plus the folder tree it came from.
    ///
    /// The palette is flattened once, when the inspector opens, so typing in the search bar
    /// never walks the game's folder tree on the main thread.
    /// </summary>
    public sealed class PartCatalog
    {
        /// <summary>Ceiling on indexed parts, so a mod that adds a pathological palette cannot stall the game.</summary>
        public const int MaxEntries = 20000;

        private readonly List<PartEntry> entries = new List<PartEntry>();

        public PartCatalog(PartFolder root, int maxEntries = MaxEntries)
        {
            Root = root ?? new PartFolder(string.Empty, string.Empty);
            Flatten(Root, maxEntries < 0 ? 0 : maxEntries);
        }

        public PartFolder Root { get; }

        /// <summary>Every part in the palette, in folder order.</summary>
        public IReadOnlyList<PartEntry> Entries
        {
            get { return entries; }
        }

        public int Count
        {
            get { return entries.Count; }
        }

        private void Flatten(PartFolder folder, int maxEntries)
        {
            for (int i = 0; i < folder.Items.Count && entries.Count < maxEntries; i++)
            {
                entries.Add(folder.Items[i]);
            }

            for (int i = 0; i < folder.Children.Count && entries.Count < maxEntries; i++)
            {
                Flatten(folder.Children[i], maxEntries);
            }
        }
    }
}
