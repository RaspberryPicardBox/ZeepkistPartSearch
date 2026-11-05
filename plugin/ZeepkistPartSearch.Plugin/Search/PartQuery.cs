using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace ZeepkistPartSearch.Search
{
    /// <summary>
    /// The whole search: query normalisation, matching, ranking and the result cap.
    ///
    /// No Unity types, no game types and no reflection, so the test harness can compile and
    /// exercise every line of it. The Unity layer only turns a <see cref="PartEntry"/> back
    /// into a click on the game's own button prefab.
    /// </summary>
    public static class PartQuery
    {
        private const int NameWeight = 0;
        private const int TermWeight = 2;
        private const int IdWeight = 4;
        private const int FolderWeight = 6;

        private static readonly char[] TermSeparators = { ' ' };

        /// <summary>
        /// Cleans a typed query before it is matched.
        ///
        /// Control characters become spaces, runs of whitespace collapse, and the characters
        /// that mean something to TextMeshPro (<c>&lt;</c> and <c>&gt;</c>) or that are
        /// invisible (zero width, bidi overrides, BOM) are removed. The result is trimmed and
        /// cut to <paramref name="maxLength"/> without splitting a surrogate pair.
        /// </summary>
        public static string NormaliseQuery(string raw, int maxLength = SearchOptions.MaxQueryLength)
        {
            if (string.IsNullOrEmpty(raw) || maxLength <= 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder(raw.Length);
            bool lastWasSpace = false;

            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];

                if (IsInvisible(c) || c == '<' || c == '>')
                {
                    continue;
                }

                if (char.IsControl(c))
                {
                    c = ' ';
                }

                if (c == ' ')
                {
                    if (lastWasSpace)
                    {
                        continue;
                    }

                    lastWasSpace = true;
                }
                else
                {
                    lastWasSpace = false;
                }

                builder.Append(c);

                if (builder.Length >= maxLength)
                {
                    break;
                }
            }

            string result = builder.ToString().Trim();
            if (result.Length > 0 && char.IsHighSurrogate(result[result.Length - 1]))
            {
                result = result.Substring(0, result.Length - 1);
            }

            return result;
        }

        /// <summary>
        /// Runs a query against the whole palette, or against the open folder when
        /// <see cref="SearchOptions.Scope"/> says so. Returns at most
        /// <see cref="SearchOptions.MaxResults"/> entries, best match first.
        /// </summary>
        public static List<PartEntry> Search(PartCatalog catalog, PartFolder currentFolder,
            string query, SearchOptions options)
        {
            List<PartEntry> results = new List<PartEntry>();
            if (catalog == null)
            {
                return results;
            }

            SearchOptions effective = (options ?? new SearchOptions()).Clamp();
            string normalised = NormaliseQuery(query, SearchOptions.MaxQueryLength);
            if (normalised.Length < Math.Max(1, effective.MinimumCharacters))
            {
                return results;
            }

            string[] terms = normalised.Split(TermSeparators, StringSplitOptions.RemoveEmptyEntries);
            if (terms.Length == 0)
            {
                return results;
            }

            IReadOnlyList<PartEntry> source =
                effective.Scope == SearchScope.CurrentFolder && currentFolder != null
                    ? (IReadOnlyList<PartEntry>)currentFolder.Items
                    : catalog.Entries;

            List<ScoredEntry> scored = new List<ScoredEntry>();
            for (int i = 0; i < source.Count; i++)
            {
                int score;
                if (TryScore(source[i], terms, effective, out score))
                {
                    scored.Add(new ScoredEntry(source[i], score));
                }
            }

            scored.Sort(Compare);

            int take = Math.Min(effective.MaxResults, scored.Count);
            for (int i = 0; i < take; i++)
            {
                results.Add(scored[i].Entry);
            }

            return results;
        }

        // ---------------------------------------------------------------- matching

        private static bool TryScore(PartEntry entry, string[] terms, SearchOptions options, out int score)
        {
            score = 0;

            for (int i = 0; i < terms.Length; i++)
            {
                string term = terms[i];
                int best = FieldScore(entry.Name, term, options.CaseSensitive, NameWeight);
                best = Math.Min(best, FieldScore(entry.Term, term, options.CaseSensitive, TermWeight));

                if (options.MatchPartId && entry.Id >= 0)
                {
                    string id = entry.Id.ToString(CultureInfo.InvariantCulture);
                    best = Math.Min(best, FieldScore(id, term, options.CaseSensitive, IdWeight));
                }

                if (options.MatchFolderName && entry.FolderPath.Length > 0)
                {
                    best = Math.Min(best, FieldScore(entry.FolderPath, term, options.CaseSensitive, FolderWeight));
                }

                if (best == int.MaxValue)
                {
                    return false;
                }

                score += best;
            }

            return true;
        }

        /// <summary>
        /// Lower is better: exact &lt; prefix &lt; word start &lt; anywhere, plus the field's
        /// weight so a name hit always outranks the same hit inside a folder path.
        /// </summary>
        private static int FieldScore(string haystack, string needle, bool caseSensitive, int weight)
        {
            if (string.IsNullOrEmpty(haystack) || string.IsNullOrEmpty(needle))
            {
                return int.MaxValue;
            }

            StringComparison comparison = caseSensitive
                ? StringComparison.Ordinal
                : StringComparison.OrdinalIgnoreCase;

            if (haystack.Equals(needle, comparison))
            {
                return weight;
            }

            if (haystack.StartsWith(needle, comparison))
            {
                return weight + 1;
            }

            int index = haystack.IndexOf(needle, comparison);
            if (index < 0)
            {
                return int.MaxValue;
            }

            return IsWordStart(haystack, index) ? weight + 2 : weight + 3;
        }

        private static bool IsWordStart(string haystack, int index)
        {
            if (index <= 0)
            {
                return true;
            }

            char previous = haystack[index - 1];
            return previous == ' ' || previous == '-' || previous == '_' || previous == '/'
                   || previous == '>' || previous == '(' || previous == '[' || previous == '.'
                   || previous == ':' || previous == '&';
        }

        private static int Compare(ScoredEntry a, ScoredEntry b)
        {
            int byScore = a.Score.CompareTo(b.Score);
            if (byScore != 0)
            {
                return byScore;
            }

            int byName = string.Compare(a.Entry.Name, b.Entry.Name, StringComparison.OrdinalIgnoreCase);
            if (byName != 0)
            {
                return byName;
            }

            int byFolder = string.Compare(a.Entry.FolderPath, b.Entry.FolderPath, StringComparison.OrdinalIgnoreCase);
            if (byFolder != 0)
            {
                return byFolder;
            }

            return string.CompareOrdinal(a.Entry.Key, b.Entry.Key);
        }

        private static bool IsInvisible(char c)
        {
            return c == '\u200B' || c == '\u200C' || c == '\u200D' || c == '\u200E' || c == '\u200F'
                   || (c >= '\u202A' && c <= '\u202E')
                   || (c >= '\u2066' && c <= '\u2069')
                   || c == '\uFEFF';
        }

        private struct ScoredEntry
        {
            public ScoredEntry(PartEntry entry, int score)
            {
                Entry = entry;
                Score = score;
            }

            public PartEntry Entry { get; }

            public int Score { get; }
        }
    }
}
