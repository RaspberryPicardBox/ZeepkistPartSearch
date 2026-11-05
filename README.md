# ZeepkistPartSearch

A **search bar at the top of the level editor's parts selector**. Type `ice` and every ice
block in the palette appears, whichever folder it actually lives in — no more clicking
through *Physics → Ice → Ramps* to find one part.

```
┌─ Inspector ─────────────────────────────┐
│  ┌───────────────────────────────────┐  │
│  │ Search parts...              ice  │  │  <- added by this mod
│  └───────────────────────────────────┘  │
│   [Ice Block]   [Ice Ramp]   [Straight  │
│   [Ice Corner]  [Ice Slope]  [Ice ...]  │
└─────────────────────────────────────────┘
```

| Part | What it is |
| --- | --- |
| Search bar | A TextMeshPro input field at the very top of the parts area, on a rounded plate drawn in the game's tile colours (pale yellow `#FFE873`, dark ink text) |
| Results | The game's real thumbnail buttons, in the order this mod ranks them |
| Clear | Empty the field and the folder you were in comes back exactly as it was |

---

## What it does and does not do

**Does**

- Appends a search bar to the top of the level editor's parts selector (blocks, and optionally
  nature/tree and paint palettes too).
- Searches **the whole palette by default**, not just the folder you are standing in. Set
  `Search/Scope = CurrentFolder` if you would rather it only looked at the open folder.
- Matches the **localized** name you actually see, so a Dutch or German player searches in
  their own language. It also matches the numeric block id and (optionally) the folder name.
- Handles several words as *all must match*: `ice ramp` is narrower than `ice`.
- Ranks exact matches first, then prefixes, then word starts, then anything.
- Reuses the game's own click paths (`CreateNewBlock`, `SetToTree`, `SetToCurrentPaint`), so
  placing a part behaves exactly like clicking it in the folder.
- Locks the editor's keyboard shortcuts while the field has focus, so typing `save` searches
  instead of opening the save dialog.
- Is fully toggleable and configurable, both in the in-game Configuration Manager and in the
  config file.

**Does not**

- Modify, reorder or delete anything in the game's palette. It hides the thumbnails the game
  has already built and shows its own; clearing the query puts the originals back.
- Require ZeepSDK, QuickFilter or any other mod. It does not depend on them and does not
  replace the method they patch.
- Send anything anywhere, download anything, write files, or add telemetry.
- Change how levels are saved: a search result is the same block object a folder click gives.

---

## Requirements

| Item | Notes |
| --- | --- |
| Zeepkist | Steam appid `1440670` |
| BepInEx | 5.4.x x64 (Mono). Installed by Modkist, or manually |
| .NET | Nothing to install; the mod targets `net472` like every other Zeepkist mod |

No other mod is required.

## Install

### Modkist (recommended)

1. Install [Modkist](https://github.com/Thundernerd/ModkistMKII) and point it at your
   Zeepkist folder.
2. Find **Parts Search** in the mod.io browser and press install.

### Manual

1. Install BepInEx 5.4.x x64 into your Zeepkist folder if you have not already.
2. Copy `ZeepkistPartSearch.dll` into `BepInEx/plugins/`.
3. Start the game once. The mod creates
   `BepInEx/config/com.nyxeonfox.zeepkist.partsearch.cfg`.

## Configure

Everything is in `BepInEx/config/com.nyxeonfox.zeepkist.partsearch.cfg`, and every entry also
appears in the in-game **Configuration Manager** (F1 by default).

```ini
[General]
## Show the search bar at the top of the parts selector.
Enabled = true

[Search]
## AllFolders searches the whole palette. CurrentFolder searches only the open folder.
Scope = AllFolders
MinimumCharacters = 1
CaseSensitive = false
MatchPartIds = true
MatchFolderNames = true
MaxResults = 60
IncludeTrees = true
IncludePaints = true

[Interface]
## Match the game's inspector setting: 3 normally, 4 with small thumbnails.
ColumnsPerRow = 3
BarHeight = 30
MaxQueryLength = 64
Placeholder = Search parts...
AutoFocus = false
```

Changes apply while you type; `Enabled` hides or shows the bar immediately.

| Setting | What it does |
| --- | --- |
| `General/Enabled` | Master toggle for the bar |
| `Search/Scope` | `AllFolders` (default) or `CurrentFolder` |
| `Search/MinimumCharacters` | Results only appear after this many characters |
| `Search/CaseSensitive` | Require the typed case to match |
| `Search/MatchPartIds` | Typing `42` finds block 42 |
| `Search/MatchFolderNames` | Typing `physics` lists everything in Physics folders |
| `Search/MaxResults` | Cap on results drawn at once (1–200) |
| `Search/IncludeTrees` | Show the bar in the nature/tree palette |
| `Search/IncludePaints` | Show the bar in the paint/material palette |
| `Interface/ColumnsPerRow` | Thumbnails per row; match the game's own inspector setting |
| `Interface/BarHeight` | Height of the bar in UI pixels |
| `Interface/MaxQueryLength` | Longest query accepted |
| `Interface/Placeholder` | Grey text when the bar is empty |
| `Interface/AutoFocus` | Put the caret in the bar when the selector opens |

## Compatibility

| Other mod | What happens |
| --- | --- |
| **QuickFilter** | Coexists. QuickFilter builds its own filtered grid inside the same content box; this mod hides those buttons while a query is active and restores them when it is cleared. Neither mod replaces the other's method. |
| **ZeepSDK / mods built on it** | No dependency and no shared state. The bar is attached with its own Harmony postfix on the inspector's `Awake`. |
| **Mods that add blocks or custom folders** | Indexed automatically, because the palette tree is walked as the game has loaded it. |
| **Localization mods** | Search matches the translated names, via the game's own `I2.Loc.LocalizationManager`. |
| **Configuration Manager** | Config entries are plain BepInEx `ConfigEntry` values, so they appear with no extra dependency. |

Locked progression folders still show their padlock in the normal view, and this mod never
writes to the palette, so it cannot unlock anything.

## Build from source

The plugin targets `net472` and compiles against your **own** game install. No game binaries
are vendored or redistributed.

```bash
# Point the build at your Zeepkist folder (the one containing Zeepkist.exe)
ZEEPKIST_GAME_DIR=/path/to/Zeepkist dotnet build plugin/ZeepkistPartSearch.Plugin -c Release

# Logic checks: normalisation, scopes, ranking and limits (no game needed)
dotnet run --project tests/ZeepkistPartSearch.LogicTests -c Release

# The automated half of the security review
./tools/check-dangerous-apis.sh
./tools/check-personal-data.sh

# Copy the DLL into the game, or build the zip mod.io takes
tools/deploy-plugin.sh
tools/package-mod.sh
```

You can also create a `local.props` next to `Directory.Build.props` with
`<GameDir>/path/to/Zeepkist</GameDir>`; it is ignored by git. See `local.props.example`.

Game and BepInEx assemblies are **referenced in place** and never copied to the output, so
the release zip contains only `ZeepkistPartSearch.dll` (plus README and changelog).

## Status and limitations

- The search core is covered by 56 offline checks, which run in CI on every push.
- The Unity-facing wiring was written against the members of the installed game assembly,
  recorded with the decompiler in [docs/design.md](docs/design.md). It is built against that
  assembly here, but this repository has not been run with the game inside this environment.
- A game update that renames the inspector's public fields, or its
  `EnableBlockInspector` / `EnablePaintInspector` / `EnableTreeInspector` / `GoNewFolder`
  methods, disables the bar and leaves one clear line in `LogOutput.log` rather than breaking
  the editor.
- `Scope = CurrentFolder` searches the folder's **direct** parts, not its subfolders.
- Results are capped by `Search/MaxResults` (60 by default) rather than paginated: narrow the
  query.
- Search runs on the main thread when the query changes. The palette is flattened once and
  cached for ten seconds, so typing costs a string comparison per part.

## Documentation

| File | What is in it |
| --- | --- |
| [docs/design.md](docs/design.md) | The game internals this mod relies on, with the decompiled evidence |
| [docs/security.md](docs/security.md) | Threat model, code-execution surface, limits, privacy |
| [docs/ci.md](docs/ci.md) | What CI runs and how to make it block merges |
| [docs/modio-description.md](docs/modio-description.md) | Listing copy for the mod.io page |

## Licence

MIT. See [LICENSE](LICENSE).
