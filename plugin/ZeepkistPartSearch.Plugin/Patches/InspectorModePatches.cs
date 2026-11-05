using HarmonyLib;
using ZeepkistPartSearch.Search;
using ZeepkistPartSearch.UI;

namespace ZeepkistPartSearch.Patches
{
    /// <summary>
    /// Keeps the panel in step with which palette the inspector is showing, and with which
    /// folder the player has opened (needed for the optional "current folder" search scope).
    ///
    /// These are postfixes on public methods plus one prefix on the private
    /// <c>GoNewFolder</c>; nothing is replaced, so other mods that do replace
    /// <c>CreateBlockGUI</c> keep working and the panel simply re-applies to their grid.
    /// </summary>
    [HarmonyPatch(typeof(LEV_Inspector), "EnableBlockInspector")]
    internal static class EnableBlockInspectorPatch
    {
        private static void Postfix(LEV_Inspector __instance)
        {
            PartSearchPanel panel = Panel(__instance);
            if (panel != null)
            {
                BlockList list = __instance.globalBlockList;
                panel.SetMode(PartKind.Block, list != null ? list.globalBlocksFolder : null);
            }
        }

        internal static PartSearchPanel Panel(LEV_Inspector inspector)
        {
            return inspector != null ? inspector.GetComponent<PartSearchPanel>() : null;
        }
    }

    [HarmonyPatch(typeof(LEV_Inspector), "EnablePaintInspector")]
    internal static class EnablePaintInspectorPatch
    {
        private static void Postfix(LEV_Inspector __instance)
        {
            PartSearchPanel panel = EnableBlockInspectorPatch.Panel(__instance);
            if (panel != null)
            {
                BlockList list = __instance.globalBlockList;
                panel.SetMode(PartKind.Paint, list != null ? list.globalPaintFolder : null);
            }
        }
    }

    [HarmonyPatch(typeof(LEV_Inspector), "EnableTreeInspector")]
    internal static class EnableTreeInspectorPatch
    {
        private static void Postfix(LEV_Inspector __instance)
        {
            PartSearchPanel panel = EnableBlockInspectorPatch.Panel(__instance);
            if (panel != null)
            {
                BlockList list = __instance.globalBlockList;
                panel.SetMode(PartKind.Tree, list != null ? list.globalTreeFolder : null);
            }
        }
    }

    /// <summary>
    /// <c>GoNewFolder(BlocksFolder theFolder, int typeOfThing, bool folderUp)</c>.
    /// The type is the game's own: 0 blocks, 1 trees, anything else paints.
    /// </summary>
    [HarmonyPatch(typeof(LEV_Inspector), "GoNewFolder")]
    internal static class GoNewFolderPatch
    {
        private static void Prefix(LEV_Inspector __instance, BlocksFolder __0, int __1)
        {
            PartSearchPanel panel = EnableBlockInspectorPatch.Panel(__instance);
            if (panel == null)
            {
                return;
            }

            PartKind kind = __1 == 0 ? PartKind.Block : (__1 == 1 ? PartKind.Tree : PartKind.Paint);
            panel.SetMode(kind, __0);
        }
    }
}
