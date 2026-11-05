# Security review

Scope: ZeepkistPartSearch 0.1.0, audited against the question *can anything this mod touches
make it do something other than search a parts palette?* The short answer is no, and this
document shows what was checked, what was built to make it true, and what risks remain.

## Threat model

This mod has no network path at all. The inputs it handles are:

| Input | Who controls it | Reaches |
| --- | --- | --- |
| The typed query | the player | string comparisons only; never rendered as markup |
| Config values (`ConfigEntry`) | the player, or anyone who can edit the cfg | limits, placeholder text, scope |
| Palette names and folder names | the game's localization database, and any mod that adds custom blocks/folders | search comparisons; the game renders its own hover text |
| Sprite and object references | the game (`BlockProperties`, `MaterialHolder`, `LEV_TreeGunTreeHolder`) | the game's own thumbnail prefab |

There is no remote input, no file parsing, no deserialisation and no plugin loading.

Assets worth protecting, in order: the integrity of the game process, the stability of the
editor (a crash loses an unsaved level), and the player's machine.

## Verdict

| Risk | Finding |
| --- | --- |
| Code execution | **Not possible.** No dynamic loading, no `Type.GetType`, no `Activator`, no serializers, no process spawn, no P/Invoke. See *Code execution surface*. |
| Rich text (TMP) injection through the query | **Prevented.** The input's text component and the `TMP_InputField` both have rich text switched off, and the query is only ever compared as a string. Angle brackets are also stripped before matching. |
| Rich text injection through a part name | **Not our surface.** This mod never writes a part name into a text field; it hands the game's localization key back to the game's own hover method, which renders exactly as it always has. |
| Local file read or write | **No file API at all.** The gate in `tools/check-dangerous-apis.sh` fails the build on `System.IO`. |
| Network access | **No network API at all.** The same gate fails on `System.Net`, `UnityWebRequest` and `WWW`. |
| Unbounded work or memory | **Bounded** at every growth point: query length, result count, indexed palette size. See *Resource limits*. |
| Corrupting the editor's state | **Narrow and reversible.** Only thumbnails are hidden/shown, `contentBox.sizeDelta`, `scrollbar.value` and `LEV_Input.inputLocked` are changed, and each has its previous value restored. The palette objects are never modified. |
| Unlocking progression content | **Not possible.** The mod never calls an unlock, and never filters a folder out of the game's own view: it only lists what is already loaded. |
| Privacy | Nothing leaves the process. No telemetry, no files, no sockets, no clipboard reads. |

## Code execution surface

The .NET mechanisms that can turn data into code, and what this mod contains:

| Mechanism | Present? | Evidence |
| --- | --- | --- |
| `Assembly.Load` / `LoadFrom` / `AssemblyBuilder` | no | grep over all sources |
| `Type.GetType(string)` / `Activator.CreateInstance` | no | grep |
| Reflection on names read from data | no | none at all; Harmony targets are compile-time types and literal method names |
| Harmony patches from data | no | `[HarmonyPatch(typeof(LEV_Inspector), "Awake")]`, `"EnableBlockInspector"`, `"EnablePaintInspector"`, `"EnableTreeInspector"`, `"GoNewFolder"` |
| `BinaryFormatter` / `XmlSerializer` / `DataContractSerializer` / Json.NET | no | grep; the mod reads no serialized data |
| Scripting engines | no | grep |
| `Process.Start` / shell | no | grep |
| File IO, sockets | no | grep, and the gate above |
| P/Invoke | no | grep |

The only reflection-like operation is Harmony's own method lookup for the five patch targets,
with names fixed in source. If a game update renames one, Harmony logs a patch failure and the
mod's `Awake` catches it; the editor itself keeps working.

## Query handling

`PartQuery.NormaliseQuery` is the single funnel for everything typed:

* control characters become spaces,
* runs of whitespace collapse to one,
* `<` and `>` are removed (the two characters TextMeshPro treats as markup delimiters),
* zero-width characters, bidi overrides and the BOM are removed,
* the result is trimmed and cut to `Interface/MaxQueryLength` (64 by default), never splitting
  a surrogate pair.

Independently of that, the input field is created with `richText = false` on both the field
and its text component, and `characterLimit` is set to the hard ceiling, so even the text the
player sees while typing cannot be interpreted as markup.

The sanitised query is used **only** for comparisons. It is not logged and not written
anywhere.

## Resource limits

| Limit | Value | Where |
| --- | --- | --- |
| Query length | 64 characters, configurable 8–64 | `SearchOptions.MaxQueryLength`, `Interface/MaxQueryLength` |
| Minimum query | 1 character | `Search/MinimumCharacters` |
| Results drawn | 60 by default, ceiling 200 | `SearchOptions.MaxResults`, `Search/MaxResults` |
| Parts indexed | 20,000, then indexing stops | `PartCatalog.MaxEntries` |
| Thumbnail buttons rebuilt | one per result, never more than the result cap | `PartSearchPanel.Render` |
| Palette rescan while searching | at most 4 times a second | `PartSearchPanel.RescanInterval` |
| Palette snapshot rebuild | at most once every 10 seconds | `PartSearchPanel.CatalogLifetime` |
| Placeholder text | 32 characters, markup stripped | `ModConfig.PlaceholderText` |
| Plate texture | one 20×20 RGBA image, built once per session (1.6 KB) | `TilePlateSprite` |

Every config value is clamped on read, and an out-of-range value falls back to the documented
default rather than being trusted.

## What the mod changes on game objects

| Change | Restored? |
| --- | --- |
| `ThumbnailButton.gameObject.SetActive(false)` for the game's/another mod's tiles | yes, on clear, on hide and on destroy |
| `contentBox.sizeDelta` | yes, the value captured before the first result is put back |
| `inspector.scrollbar.value = 1` | no; this is the same thing the game does when it opens a folder |
| `central.input.inputLocked` | yes, the captured value is put back on blur, hide and destroy |
| `central.cam.OverrideOutsideGameView(bool)` | yes, on pointer exit and on disable, so the camera never stays in "over UI" mode |
| The ScrollRect's top edge (shrunk so the bar owns a real strip) and, only when the hierarchy needs it, `viewport.offsetMax` | yes, both restored on destroy |

No palette object, list, folder or id is written to. No save file is touched.

## Residual risks

* **A custom part name containing markup** would be rendered by the game's own hover code, not
  by this mod. That is pre-existing behaviour for every custom block and is out of scope here,
  but it is the only place where third-party text reaches a TMP field in this feature.
* **A malicious config file** is trusted local input; it can only change bounded numbers, one
  scope enum, seven booleans and a 32-character placeholder.
* **A game update** that renames the inspector members used here disables the bar. It does not
  crash: construction and every patch body are wrapped and log a single line.
* **Harmony patch failure** is caught in `Plugin.Awake` and logged; the game continues without
  the mod's UI.

## How to re-run the audit

```bash
# everything, including the normalisation and hostile-input section
dotnet run --project tests/ZeepkistPartSearch.LogicTests -c Release

# the forbidden-API sweep this document is based on
./tools/check-dangerous-apis.sh

# machine paths, personal addresses, credentials
./tools/check-personal-data.sh

# the raw grep, if you want to see it yourself
grep -rnE 'Assembly\.Load|Type\.GetType|Activator\.|Process\.|BinaryFormatter|DllImport|System\.IO|System\.Net' \
  plugin --include='*.cs'
```
