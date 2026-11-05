using System;
using BepInEx.Configuration;
using ZeepkistPartSearch.Search;

namespace ZeepkistPartSearch
{
    /// <summary>
    /// Everything the mod exposes in <c>BepInEx/config/com.nyxeonfox.zeepkist.partsearch.cfg</c>
    /// and in the in-game Configuration Manager.
    ///
    /// Plain <see cref="ConfigEntry{T}"/> values only: Configuration Manager draws these
    /// automatically, so the mod needs no dependency on it (or on ZeepSDK) to be configurable.
    /// Every value is clamped when it is read, so a hand-edited config cannot ask the search
    /// for an unbounded amount of work.
    /// </summary>
    internal sealed class ModConfig
    {
        public const string DefaultPlaceholder = "Search parts...";

        private readonly ConfigEntry<bool> enabled;
        private readonly ConfigEntry<SearchScope> scope;
        private readonly ConfigEntry<int> minimumCharacters;
        private readonly ConfigEntry<bool> caseSensitive;
        private readonly ConfigEntry<bool> matchPartIds;
        private readonly ConfigEntry<bool> matchFolderNames;
        private readonly ConfigEntry<int> maxResults;
        private readonly ConfigEntry<bool> includeTrees;
        private readonly ConfigEntry<bool> includePaints;
        private readonly ConfigEntry<int> columnsPerRow;
        private readonly ConfigEntry<int> barHeight;
        private readonly ConfigEntry<int> maxQueryLength;
        private readonly ConfigEntry<string> placeholder;
        private readonly ConfigEntry<bool> autoFocus;

        public ModConfig(ConfigFile file)
        {
            if (file == null)
            {
                throw new ArgumentNullException("file");
            }

            enabled = file.Bind("General", "Enabled", true,
                "Show the search bar at the top of the level editor's parts selector. " +
                "Turn this off to hide the bar completely; searching is unaffected by any other mod.");

            scope = file.Bind("Search", "Scope", SearchScope.AllFolders,
                "AllFolders searches the whole parts palette, including folders you are not in. " +
                "CurrentFolder searches only the folder that is open.");

            minimumCharacters = file.Bind("Search", "MinimumCharacters", 1,
                "How many characters must be typed before results appear (1-64).");

            caseSensitive = file.Bind("Search", "CaseSensitive", false,
                "Match case when searching. Off by default.");

            matchPartIds = file.Bind("Search", "MatchPartIds", true,
                "Also match the numeric block or material id, so typing '42' finds block 42.");

            matchFolderNames = file.Bind("Search", "MatchFolderNames", true,
                "Also match the folder a part lives in, so typing 'physics' lists the physics parts.");

            maxResults = file.Bind("Search", "MaxResults", 60,
                "Most results shown at once (1-200). Narrow the query rather than raising this.");

            includeTrees = file.Bind("Search", "IncludeTrees", true,
                "Show the search bar in the nature/tree part selector as well.");

            includePaints = file.Bind("Search", "IncludePaints", true,
                "Show the search bar in the paint/material part selector as well.");

            columnsPerRow = file.Bind("Interface", "ColumnsPerRow", 3,
                "Thumbnails per row in the search results (1-8). Match the game's own " +
                "inspector setting: 3 normally, 4 with small thumbnails.");

            barHeight = file.Bind("Interface", "BarHeight", 30,
                "Height of the search bar in UI pixels (18-80).");

            maxQueryLength = file.Bind("Interface", "MaxQueryLength", SearchOptions.MaxQueryLength,
                "Longest query the bar accepts, in characters (8-64). Longer input is truncated.");

            placeholder = file.Bind("Interface", "Placeholder", DefaultPlaceholder,
                "Grey text shown in the empty search bar. Rich-text tags are stripped.");

            autoFocus = file.Bind("Interface", "AutoFocus", false,
                "Put the caret in the search bar as soon as the parts selector opens.");
        }

        public bool Enabled
        {
            get { return enabled.Value; }
        }

        public bool IncludeTrees
        {
            get { return includeTrees.Value; }
        }

        public bool IncludePaints
        {
            get { return includePaints.Value; }
        }

        public bool AutoFocus
        {
            get { return autoFocus.Value; }
        }

        public int BarHeight
        {
            get { return Clamp(barHeight.Value, 18, 80, 30); }
        }

        public int ColumnsPerRow
        {
            get { return Clamp(columnsPerRow.Value, 1, 8, 3); }
        }

        public int MaxQueryLength
        {
            get { return Clamp(maxQueryLength.Value, 8, SearchOptions.MaxQueryLength, SearchOptions.MaxQueryLength); }
        }

        public int MinimumCharacters
        {
            get { return Clamp(minimumCharacters.Value, 1, SearchOptions.MaxQueryLength, 1); }
        }

        /// <summary>The placeholder, stripped of markup and invisible characters and capped.</summary>
        public string PlaceholderText
        {
            get
            {
                string cleaned = PartQuery.NormaliseQuery(placeholder.Value, 32);
                return cleaned.Length == 0 ? DefaultPlaceholder : cleaned;
            }
        }

        public SearchOptions BuildSearchOptions()
        {
            return new SearchOptions
            {
                Scope = scope.Value,
                CaseSensitive = caseSensitive.Value,
                MatchPartId = matchPartIds.Value,
                MatchFolderName = matchFolderNames.Value,
                MinimumCharacters = MinimumCharacters,
                MaxResults = Clamp(maxResults.Value, 1, SearchOptions.MaxResultsCeiling, 60)
            }.Clamp();
        }

        /// <summary>Values outside their supported range fall back to the documented default.</summary>
        private static int Clamp(int value, int minimum, int maximum, int fallback)
        {
            return value < minimum || value > maximum ? fallback : value;
        }
    }
}
