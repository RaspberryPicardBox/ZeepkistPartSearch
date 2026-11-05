# mod.io listing copy

Use this to fill in the mod.io page for ZeepkistPartSearch.

## Name

`Parts Search`

## Summary (short description, ~200 chars)

A search bar at the top of the level editor's parts selector. Type a word and see every
matching block from every folder at once — no more digging through nested folders.

## Description

**Find a part by typing its name.**

Parts Search adds a search box to the top of the level editor's parts selector. The default
search covers the **whole palette**, not just the folder you happen to be in, so `ice` lists
every ice block, ramp and corner in one grid.

```
Search parts...  [ ice                    ]
[Ice Block] [Ice Ramp] [Straight Ice]
[Ice Corner] [Ice Slope] [Ice Platform]
```

### Features

- **Whole-palette search by default**, or restrict it to the open folder in the settings
- Matches the **localized** name you see in game, plus the block id and folder name
- Several words all have to match: `ice ramp` is narrower than `ice`
- Best matches first: exact names, then prefixes, then word starts, then anything
- Works on **blocks, nature parts and paint materials**
- Clicking a result uses the game's own placement path, so nothing about your level changes
- Your keyboard shortcuts are paused while you type, so typing cannot trigger them
- Clearing the box puts the folder back exactly as it was
- Everything configurable in-game via **Configuration Manager**, or in the config file
- No other mod required

### Settings worth knowing

| Setting | Default | What it does |
| --- | --- | --- |
| `General/Enabled` | on | Show or hide the bar |
| `Search/Scope` | `AllFolders` | Search everything, or only the open folder |
| `Search/MaxResults` | 60 | How many results to draw at once |
| `Interface/ColumnsPerRow` | 3 | Match the game's inspector setting (3, or 4 with small thumbnails) |

### Compatibility

Designed to sit alongside other level editor mods. It does not replace the game's palette
builder and does not require ZeepSDK, so it keeps working when other mods are updated — and
mods that add custom blocks or folders are searchable automatically.

### Notes

- Requires BepInEx 5.4.x x64 (Modkist installs it for you).
- The parts list is read exactly as the game loads it; the mod cannot unlock anything.

## Tags

`level editor`, `editor`, `search`, `utility`, `client`, `quality of life`

## Changelog

See `CHANGELOG.md`.
