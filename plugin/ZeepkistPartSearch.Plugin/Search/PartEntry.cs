namespace ZeepkistPartSearch.Search
{
    /// <summary>
    /// One selectable part as the search sees it.
    ///
    /// Deliberately free of UnityEngine types: the whole search core is compiled by the
    /// test harness, so the matching logic that ships is exactly the logic that is tested.
    /// Sprite and source-object references stay in the Unity layer, keyed by <see cref="Key"/>.
    /// </summary>
    public sealed class PartEntry
    {
        public PartEntry(string key, PartKind kind, int id, string name, string term,
            string sourceId, string folderPath)
        {
            Key = key ?? string.Empty;
            Kind = kind;
            Id = id;
            Name = name ?? string.Empty;
            Term = term ?? string.Empty;
            SourceId = sourceId ?? string.Empty;
            FolderPath = folderPath ?? string.Empty;
        }

        /// <summary>Stable identity, used to look the part up in the Unity layer.</summary>
        public string Key { get; }

        public PartKind Kind { get; }

        /// <summary>Block id or material id; -1 for trees, which are identified by <see cref="SourceId"/>.</summary>
        public int Id { get; }

        /// <summary>Localized name, as the inspector shows it. What the query is matched against.</summary>
        public string Name { get; }

        /// <summary>Localization key of the name, handed back to the game's hover label unchanged.</summary>
        public string Term { get; }

        /// <summary>Tree id for the game's tree hover label; empty for blocks and paints.</summary>
        public string SourceId { get; }

        /// <summary>"Folder &gt; Subfolder"; empty for items at the palette root.</summary>
        public string FolderPath { get; }

        public override string ToString()
        {
            return Kind + " " + Key + " (" + Name + ")";
        }
    }
}
