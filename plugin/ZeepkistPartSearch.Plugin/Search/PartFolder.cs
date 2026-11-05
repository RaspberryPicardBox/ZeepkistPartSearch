using System.Collections.Generic;

namespace ZeepkistPartSearch.Search
{
    /// <summary>One folder of a parts palette, with its direct items and its subfolders.</summary>
    public sealed class PartFolder
    {
        public PartFolder(string name, string path)
        {
            Name = name ?? string.Empty;
            Path = path ?? string.Empty;
        }

        /// <summary>Folder name as shown in the inspector, already localized.</summary>
        public string Name { get; }

        /// <summary>"Folder &gt; Subfolder"; empty for the palette root.</summary>
        public string Path { get; }

        /// <summary>Parts directly inside this folder.</summary>
        public List<PartEntry> Items { get; } = new List<PartEntry>();

        public List<PartFolder> Children { get; } = new List<PartFolder>();

        /// <summary>Items in this folder and everything below it. Used by the tests and the log line.</summary>
        public int TotalItemCount
        {
            get
            {
                int total = Items.Count;
                for (int i = 0; i < Children.Count; i++)
                {
                    total += Children[i].TotalItemCount;
                }

                return total;
            }
        }
    }
}
