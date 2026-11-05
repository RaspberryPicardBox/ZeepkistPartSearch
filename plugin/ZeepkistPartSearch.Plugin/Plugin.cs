using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using ZeepkistPartSearch.Patches;

namespace ZeepkistPartSearch
{
    /// <summary>
    /// ZeepkistPartSearch — a search bar at the top of the level editor's parts selector.
    ///
    /// Local, offline and read-only: it reads the palette the game has already loaded and
    /// hands clicks back to the game's own block, tree and paint selection methods. It opens
    /// no sockets, writes no files and loads no assemblies.
    /// </summary>
    [BepInPlugin(Guid, PluginName, PluginVersion)]
    [BepInProcess("Zeepkist.exe")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Guid = "com.nyxeonfox.zeepkist.partsearch";
        public const string PluginName = "Parts Search";
        public const string PluginVersion = "0.1.0";

        private static ManualLogSource staticLogger;

        private Harmony harmony;

        internal static ModConfig Settings { get; private set; }

        internal static void LogInfo(object data)
        {
            staticLogger?.LogInfo(data);
        }

        internal static void LogError(object data)
        {
            staticLogger?.LogError(data);
        }

        private void Awake()
        {
            staticLogger = Logger;

            try
            {
                Settings = new ModConfig(Config);

                harmony = new Harmony(Guid);

                // One patch class at a time: if a game update removes one target, the others
                // still apply and the log says exactly which one failed.
                ApplyPatch(typeof(InspectorAwakePatch));
                ApplyPatch(typeof(EnableBlockInspectorPatch));
                ApplyPatch(typeof(EnablePaintInspectorPatch));
                ApplyPatch(typeof(EnableTreeInspectorPatch));
                ApplyPatch(typeof(GoNewFolderPatch));

                LogInfo("Parts Search " + PluginVersion + " loaded. Press the search bar above the " +
                        "parts grid to look through every folder; configure it in " +
                        "Configuration Manager.");
            }
            catch (Exception exception)
            {
                // A broken patch must not take the game down with it.
                LogError("Parts Search failed to start: " + exception);
            }
        }

        private void ApplyPatch(Type patchType)
        {
            try
            {
                harmony.CreateClassProcessor(patchType).Patch();
            }
            catch (Exception exception)
            {
                LogError("Parts Search: could not patch " + patchType.Name + ": " + exception.Message);
            }
        }

        private void OnDestroy()
        {
            try
            {
                if (harmony != null)
                {
                    harmony.UnpatchSelf();
                    harmony = null;
                }
            }
            catch (Exception exception)
            {
                LogError("Parts Search failed to unpatch cleanly: " + exception.Message);
            }
        }
    }
}
