namespace ZeepkistPartSearch.Search
{
    /// <summary>
    /// How a query is run. Values are clamped by <see cref="Clamp"/> before use, so a value
    /// from the config file can never make the search allocate without bound.
    /// </summary>
    public sealed class SearchOptions
    {
        /// <summary>Longest query the search bar accepts, in characters.</summary>
        public const int MaxQueryLength = 64;

        /// <summary>Ceiling on returned results, whatever the config asks for.</summary>
        public const int MaxResultsCeiling = 200;

        public SearchScope Scope = SearchScope.AllFolders;

        public bool CaseSensitive;

        /// <summary>Match the numeric block / material id as well as the name.</summary>
        public bool MatchPartId = true;

        /// <summary>Match the folder a part lives in as well as its own name.</summary>
        public bool MatchFolderName = true;

        public int MinimumCharacters = 1;

        public int MaxResults = 60;

        public SearchOptions Copy()
        {
            return new SearchOptions
            {
                Scope = Scope,
                CaseSensitive = CaseSensitive,
                MatchPartId = MatchPartId,
                MatchFolderName = MatchFolderName,
                MinimumCharacters = MinimumCharacters,
                MaxResults = MaxResults
            };
        }

        /// <summary>Returns a copy with every value inside its supported range.</summary>
        public SearchOptions Clamp()
        {
            SearchOptions copy = Copy();

            if (copy.MinimumCharacters < 0)
            {
                copy.MinimumCharacters = 0;
            }

            if (copy.MinimumCharacters > MaxQueryLength)
            {
                copy.MinimumCharacters = MaxQueryLength;
            }

            if (copy.MaxResults < 1)
            {
                copy.MaxResults = 1;
            }

            if (copy.MaxResults > MaxResultsCeiling)
            {
                copy.MaxResults = MaxResultsCeiling;
            }

            return copy;
        }
    }
}
