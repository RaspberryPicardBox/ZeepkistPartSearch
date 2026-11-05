using System;
using System.Collections.Generic;
using ZeepkistPartSearch.Search;

namespace ZeepkistPartSearch.LogicTests
{
    /// <summary>
    /// The security-relevant half: what a query is allowed to contain, and what the search
    /// does with input that is hostile, absurd or simply empty.
    /// </summary>
    internal static partial class Program
    {
        private static void SecurityChecks()
        {
            Console.WriteLine();
            Console.WriteLine("Query normalisation");

            CheckEqual("null is empty", string.Empty, PartQuery.NormaliseQuery(null));
            CheckEqual("blank is empty", string.Empty, PartQuery.NormaliseQuery("   "));
            CheckEqual("surrounding whitespace is trimmed", "straight", PartQuery.NormaliseQuery("  straight  "));
            CheckEqual("runs of whitespace collapse", "a b", PartQuery.NormaliseQuery("a   b"));
            CheckEqual("tabs and newlines become single spaces", "a b c", PartQuery.NormaliseQuery("a\tb\nc"));
            CheckEqual("control characters cannot reach the field", "a b", PartQuery.NormaliseQuery("a\u0001b"));

            CheckEqual("TextMeshPro tag characters are stripped", "size=99road/size",
                PartQuery.NormaliseQuery("<size=99>road</size>"));

            CheckEqual("zero width characters are stripped", "road", PartQuery.NormaliseQuery("ro\u200Bad"));
            CheckEqual("bidi overrides are stripped", "road", PartQuery.NormaliseQuery("\u202Eroad"));
            CheckEqual("a byte order mark is stripped", "road", PartQuery.NormaliseQuery("\uFEFFroad"));
            CheckEqual("an invisible-only query becomes empty", string.Empty,
                PartQuery.NormaliseQuery("\u200B\u200C\u200D"));

            string hundred = new string('a', 100);
            CheckEqual("the default limit is " + SearchOptions.MaxQueryLength, SearchOptions.MaxQueryLength,
                PartQuery.NormaliseQuery(hundred).Length);
            CheckEqual("a custom limit is honoured", 5, PartQuery.NormaliseQuery("abcdefgh", 5).Length);
            CheckEqual("a zero limit returns nothing", string.Empty, PartQuery.NormaliseQuery("abc", 0));

            string emoji = "a" + char.ConvertFromUtf32(0x1F600);
            string cut = PartQuery.NormaliseQuery(emoji, 2);
            Check("a surrogate pair is never split", cut == "a", "got '" + cut + "'");

            Console.WriteLine();
            Console.WriteLine("Hostile and broken input");

            PartCatalog catalog = SampleCatalog();

            string huge = new string('x', 10000);
            string hugeNormalised = PartQuery.NormaliseQuery(huge);
            Check("a 10,000 character query stays inside the limit", hugeNormalised.Length == SearchOptions.MaxQueryLength,
                hugeNormalised.Length.ToString());

            CheckEqual("an empty query returns nothing", 0,
                PartQuery.Search(catalog, null, string.Empty, new SearchOptions()).Count);
            CheckEqual("a whitespace-only query returns nothing", 0,
                PartQuery.Search(catalog, null, "   ", new SearchOptions()).Count);
            CheckEqual("an invisible-only query returns nothing", 0,
                PartQuery.Search(catalog, null, "\u200B", new SearchOptions()).Count);

            Check("a null catalog returns nothing instead of throwing",
                PartQuery.Search(null, null, "road", null).Count == 0);
            Check("null options fall back to the defaults",
                PartQuery.Search(catalog, null, "road", null).Count == 2);

            PartFolder onlyTrees = new PartFolder("Nature", string.Empty);
            onlyTrees.Items.Add(new PartEntry("tree:1", PartKind.Tree, -1, "Pine Tree", "Tree_Pine", "pine", string.Empty));
            PartCatalog treeCatalog = new PartCatalog(onlyTrees);
            CheckEqual("an id-less part is not matched by a numeric query", 0,
                PartQuery.Search(treeCatalog, null, "1", new SearchOptions()).Count);

            Console.WriteLine();
            Console.WriteLine("Bounds");

            PartFolder big = new PartFolder("Big", string.Empty);
            for (int i = 0; i < 30; i++)
            {
                big.Items.Add(Block("block:" + i, i, "Part " + i, "Part_" + i, string.Empty));
            }

            PartCatalog capped = new PartCatalog(big, 10);
            CheckEqual("the catalog refuses to index more than its cap", 10, capped.Count);

            SearchOptions clamped = new SearchOptions
            {
                Scope = SearchScope.CurrentFolder,
                CaseSensitive = true,
                MatchPartId = false,
                MatchFolderName = false,
                MinimumCharacters = 3,
                MaxResults = 25
            }.Clamp();

            Check("clamping keeps valid values",
                clamped.Scope == SearchScope.CurrentFolder && clamped.CaseSensitive
                && !clamped.MatchPartId && !clamped.MatchFolderName
                && clamped.MinimumCharacters == 3 && clamped.MaxResults == 25);

            SearchOptions copy = clamped.Copy();
            copy.MaxResults = 1;
            Check("copying options does not mutate the original", clamped.MaxResults == 25);
        }
    }
}
