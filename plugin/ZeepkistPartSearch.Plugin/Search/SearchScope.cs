namespace ZeepkistPartSearch.Search
{
    /// <summary>Which parts a query is allowed to look at.</summary>
    public enum SearchScope
    {
        /// <summary>Every folder of the palette. The default, and what the mod advertises.</summary>
        AllFolders = 0,

        /// <summary>Only the parts directly inside the folder the user is currently in.</summary>
        CurrentFolder = 1
    }
}
