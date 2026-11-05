using System;
using HarmonyLib;
using ZeepkistPartSearch.UI;

namespace ZeepkistPartSearch.Patches
{
    /// <summary>
    /// Attaches the search panel the moment the level editor's inspector exists.
    ///
    /// A postfix on <c>LEV_Inspector.Awake</c> is used rather than ZeepSDK's inspector event so
    /// the mod keeps working when the SDK lags behind a game build. The patch target is a
    /// compile-time type and method name, never anything read from a file or the network.
    /// </summary>
    [HarmonyPatch(typeof(LEV_Inspector), "Awake")]
    internal static class InspectorAwakePatch
    {
        private static void Postfix(LEV_Inspector __instance)
        {
            try
            {
                if (__instance == null || __instance.GetComponent<PartSearchPanel>() != null)
                {
                    return;
                }

                PartSearchPanel panel = __instance.gameObject.AddComponent<PartSearchPanel>();
                panel.Initialize(__instance, Plugin.Settings);
            }
            catch (Exception exception)
            {
                Plugin.LogError("Parts Search: could not attach to the level editor inspector: " + exception);
            }
        }
    }
}
