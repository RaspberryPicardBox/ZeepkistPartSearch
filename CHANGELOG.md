# Changelog

All notable changes to ZeepkistPartSearch are recorded here.

## 0.1.0 — first release

### Added

- **A search bar at the very top of the level editor's parts selector**, built from the
  inspector's own font and button prefab so it matches the game's UI. The bar reserves its own
  strip by shrinking the parts ScrollRect from the top, so the first row of thumbnails is never
  covered.
- **Whole-palette search by default.** The query looks through every folder, not just the one
  that is open. `Search/Scope = CurrentFolder` restricts it to the open folder instead.
- Blocks, nature/tree parts and paint materials are all searchable, following whichever
  palette the inspector is showing (`Search/IncludeTrees`, `Search/IncludePaints`).
- Matching against the **localized** part name, the localization key, the numeric block or
  material id, and the folder path, each individually configurable.
- Multi-word queries where every word must match; exact matches rank above prefixes, which
  rank above word starts, which rank above a match anywhere in the name.
- Results are drawn with the game's real `ThumbnailButton` and clicked through the game's own
  `CreateNewBlock` / `SetToTree` / `SetToCurrentPaint` methods, including hover titles.
- Clearing the query restores the folder exactly as it was: the original thumbnails are kept
  alive and simply re-shown.
- The editor's keyboard shortcuts are locked through the game's own `LEV_Input.inputLocked`
  while the bar has focus, so typing cannot trigger the editor's hotkeys.
- Configuration Manager support with no extra dependency, plus a documented config file.
- 56 offline checks: palette traversal, both scopes, matching, ranking, limits, query
  normalisation (markup, control characters, zero-width and bidi characters), and the
  catalog cap.

### Notes

- Coexists with QuickFilter: it does not patch or replace `CreateBlockGUI`, so another mod's
  filtered grid is hidden while a query is active and restored when it is cleared.
- No dependency on ZeepSDK, and no network, file or process access of any kind.
