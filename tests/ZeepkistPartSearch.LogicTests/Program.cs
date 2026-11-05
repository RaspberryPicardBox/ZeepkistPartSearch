using System;
using System.Collections.Generic;
using ZeepkistPartSearch.Search;

namespace ZeepkistPartSearch.LogicTests
{
    /// <summary>
    /// Dependency-free checks for the search core. Exit code 0 = every check passed.
    /// </summary>
    internal static partial class Program
    {
        private static int checks;
        private static int failures;

        private static int Main(string[] args)
        {
            Console.WriteLine("ZeepkistPartSearch logic checks");
            Console.WriteLine("===============================");

            SearchChecks();
            SecurityChecks();

            Console.WriteLine();
            Console.WriteLine(checks + " checks, " + failures + " failure(s).");
            return failures == 0 ? 0 : 1;
        }

        // ---------------------------------------------------------------- helpers

        private static void Check(string name, bool condition, string detail = null)
        {
            checks++;
            if (condition)
            {
                Console.WriteLine("  PASS  " + name);
                return;
            }

            failures++;
            Console.WriteLine("  FAIL  " + name + (detail == null ? string.Empty : "  -> " + detail));
        }

        private static void CheckEqual(string name, string expected, string actual)
        {
            Check(name, expected == actual, "expected '" + expected + "', got '" + actual + "'");
        }

        private static void CheckEqual(string name, int expected, int actual)
        {
            Check(name, expected == actual, "expected " + expected + ", got " + actual);
        }

        private static string Join(List<PartEntry> entries)
        {
            string joined = string.Empty;
            for (int i = 0; i < entries.Count; i++)
            {
                if (i > 0)
                {
                    joined += " | ";
                }

                joined += entries[i].Name;
            }

            return joined;
        }

        private static PartEntry Block(string key, int id, string name, string term, string folder)
        {
            return new PartEntry(key, PartKind.Block, id, name, term, string.Empty, folder);
        }

        /// <summary>
        /// A small palette with nesting, so "all folders" and "current folder" are genuinely
        /// different answers:
        ///
        ///   (root)      Straight Road (1), Curved Road (2), Pine Tree
        ///   Physics     Ice Block (10), Soap Block (11)
        ///     Ice       Ice Ramp (12), Straight Ice (42)
        ///   Decor       Lamp (20)
        ///     Lamps     Neon Lamp (21)
        /// </summary>
        private static PartCatalog SampleCatalog()
        {
            PartFolder root = new PartFolder("Blocks", string.Empty);

            root.Items.Add(Block("block:1", 1, "Straight Road", "Block_Straight", string.Empty));
            root.Items.Add(Block("block:2", 2, "Curved Road", "Block_Curve", string.Empty));
            root.Items.Add(new PartEntry("tree:1", PartKind.Tree, -1, "Pine Tree", "Tree_Pine", "pine", string.Empty));

            PartFolder physics = new PartFolder("Physics", "Physics");
            physics.Items.Add(Block("block:10", 10, "Ice Block", "Block_Ice", "Physics"));
            physics.Items.Add(Block("block:11", 11, "Soap Block", "Block_Soap", "Physics"));

            PartFolder ice = new PartFolder("Ice", "Physics > Ice");
            ice.Items.Add(Block("block:12", 12, "Ice Ramp", "Block_IceRamp", "Physics > Ice"));
            ice.Items.Add(Block("block:42", 42, "Straight Ice", "Block_IceStraight", "Physics > Ice"));
            physics.Children.Add(ice);
            root.Children.Add(physics);

            PartFolder decor = new PartFolder("Decor", "Decor");
            decor.Items.Add(Block("block:20", 20, "Lamp", "Block_Lamp", "Decor"));

            PartFolder lamps = new PartFolder("Lamps", "Decor > Lamps");
            lamps.Items.Add(Block("block:21", 21, "Neon Lamp", "Block_NeonLamp", "Decor > Lamps"));
            decor.Children.Add(lamps);
            root.Children.Add(decor);

            return new PartCatalog(root);
        }

        private static PartCatalog RankingCatalog()
        {
            PartFolder root = new PartFolder("Blocks", string.Empty);
            root.Items.Add(Block("block:1", 1, "Road", "Block_Road", string.Empty));
            root.Items.Add(Block("block:2", 2, "Road Curve", "Block_RoadCurve", string.Empty));
            root.Items.Add(Block("block:3", 3, "Big Road", "Block_BigRoad", string.Empty));
            root.Items.Add(Block("block:4", 4, "Crossroad", "Block_Crossroad", string.Empty));
            return new PartCatalog(root);
        }
    }
}
