using System;
using System.Collections.Generic;
using ZeepkistPartSearch.Search;

namespace ZeepkistPartSearch.LogicTests
{
    internal static partial class Program
    {
        private static void SearchChecks()
        {
            Console.WriteLine();
            Console.WriteLine("Catalog");

            PartCatalog catalog = SampleCatalog();
            CheckEqual("every part is indexed exactly once", 9, catalog.Count);
            CheckEqual("root folder is kept", "Blocks", catalog.Root.Name);
            CheckEqual("nested folders are kept", "Physics > Ice", catalog.Root.Children[0].Children[0].Path);

            PartEntry straightIce = Find(catalog, "Straight Ice");
            Check("a nested part carries its folder path",
                straightIce != null && straightIce.FolderPath == "Physics > Ice",
                straightIce == null ? "entry missing" : straightIce.FolderPath);

            PartEntry straightRoad = Find(catalog, "Straight Road");
            Check("a root part has an empty folder path",
                straightRoad != null && straightRoad.FolderPath.Length == 0);

            PartFolder physics = catalog.Root.Children[0];

            Console.WriteLine();
            Console.WriteLine("All folders (the default scope)");

            List<PartEntry> ice = PartQuery.Search(catalog, physics, "ice", new SearchOptions());
            CheckEqual("finds parts in the open folder and in folders the player is not in", 3, ice.Count);
            CheckEqual("best match first", "Ice Block", ice.Count > 0 ? ice[0].Name : "<none>");
            Check("results keep folder context", Join(ice).Contains("Straight Ice"), Join(ice));

            List<PartEntry> roads = PartQuery.Search(catalog, physics, "road", new SearchOptions());
            CheckEqual("a word in the middle of a name matches", 2, roads.Count);
            Check("alphabetical tie-break is stable", Join(roads) == "Curved Road | Straight Road", Join(roads));

            Console.WriteLine();
            Console.WriteLine("Current folder scope");

            SearchOptions currentOnly = new SearchOptions { Scope = SearchScope.CurrentFolder };
            List<PartEntry> currentIce = PartQuery.Search(catalog, physics, "ice", currentOnly);
            CheckEqual("only the open folder's own parts", 1, currentIce.Count);

            List<PartEntry> currentBlocks = PartQuery.Search(catalog, physics, "block", currentOnly);
            CheckEqual("subfolders are excluded", 2, currentBlocks.Count);
            Check("the root folder's parts are excluded",
                !Join(currentBlocks).Contains("Straight Road"), Join(currentBlocks));

            List<PartEntry> noFolder = PartQuery.Search(catalog, null, "road", currentOnly);
            Check("a missing current folder falls back to the whole palette", noFolder.Count == 2);

            Console.WriteLine();
            Console.WriteLine("Matching");

            CheckEqual("matching is case-insensitive by default", 2,
                PartQuery.Search(catalog, physics, "ROAD", new SearchOptions()).Count);

            SearchOptions caseSensitive = new SearchOptions { CaseSensitive = true };
            CheckEqual("case sensitivity can be required", 0,
                PartQuery.Search(catalog, physics, "ROAD", caseSensitive).Count);

            CheckEqual("the numeric block id matches", 1,
                PartQuery.Search(catalog, physics, "42", new SearchOptions()).Count);

            SearchOptions noId = new SearchOptions { MatchPartId = false };
            CheckEqual("id matching can be turned off", 0,
                PartQuery.Search(catalog, physics, "42", noId).Count);

            CheckEqual("folder names match", 4,
                PartQuery.Search(catalog, physics, "physics", new SearchOptions()).Count);

            SearchOptions noFolderName = new SearchOptions { MatchFolderName = false };
            CheckEqual("folder matching can be turned off", 0,
                PartQuery.Search(catalog, physics, "physics", noFolderName).Count);

            List<PartEntry> twoTerms = PartQuery.Search(catalog, physics, "ice block", new SearchOptions());
            CheckEqual("several words must all match", 3, twoTerms.Count);
            CheckEqual("the tightest match comes first", "Ice Block", twoTerms.Count > 0 ? twoTerms[0].Name : "<none>");

            CheckEqual("an unmatched query returns nothing", 0,
                PartQuery.Search(catalog, physics, "zzzz", new SearchOptions()).Count);

            Console.WriteLine();
            Console.WriteLine("Ranking");

            PartCatalog ranking = RankingCatalog();
            List<PartEntry> ranked = PartQuery.Search(ranking, null, "road", new SearchOptions());
            Check("exact, then prefix, then word start, then anywhere",
                Join(ranked) == "Road | Road Curve | Big Road | Crossroad", Join(ranked));

            Console.WriteLine();
            Console.WriteLine("Limits");

            SearchOptions twoCharacters = new SearchOptions { MinimumCharacters = 2 };
            CheckEqual("a query below the minimum returns nothing", 0,
                PartQuery.Search(catalog, physics, "i", twoCharacters).Count);
            Check("a query at the minimum is searched",
                PartQuery.Search(catalog, physics, "ic", twoCharacters).Count > 0);

            SearchOptions twoResults = new SearchOptions { MaxResults = 2 };
            CheckEqual("the result cap is applied", 2,
                PartQuery.Search(catalog, physics, "block", twoResults).Count);

            CheckEqual("a zero result cap clamps to one", 1, new SearchOptions { MaxResults = 0 }.Clamp().MaxResults);
            CheckEqual("an enormous result cap clamps to the ceiling", SearchOptions.MaxResultsCeiling,
                new SearchOptions { MaxResults = 9999 }.Clamp().MaxResults);
            CheckEqual("a negative minimum clamps to zero", 0,
                new SearchOptions { MinimumCharacters = -5 }.Clamp().MinimumCharacters);

            Console.WriteLine();
            Console.WriteLine("Determinism");

            List<PartEntry> first = PartQuery.Search(catalog, physics, "block", new SearchOptions());
            List<PartEntry> second = PartQuery.Search(catalog, physics, "block", new SearchOptions());
            Check("the same query twice returns the same order",
                Join(first) == Join(second), Join(first) + " vs " + Join(second));
        }

        private static PartEntry Find(PartCatalog catalog, string name)
        {
            for (int i = 0; i < catalog.Entries.Count; i++)
            {
                if (catalog.Entries[i].Name == name)
                {
                    return catalog.Entries[i];
                }
            }

            return null;
        }
    }
}
