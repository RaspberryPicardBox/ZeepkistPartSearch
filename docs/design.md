# Design notes and verified facts

Everything below was checked against the game assembly installed on this machine, not
assumed. Types were dumped with `ilspycmd` from
`Zeepkist_Data/Managed/Zeepkist.dll` (the assembly holding the level editor) and
`Zeepkist_Data/Managed/l2Localization.dll` (I2 Localization).

## 1. Where the parts selector actually lives

The window players call "the parts selector" is the **new-object inspector**:
`LEV_Inspector` (global namespace, `MonoBehaviour`). It is reused for three palettes:

| Palette | Entry point | Root folder |
| --- | --- | --- |
| Blocks | `EnableBlockInspector()` | `BlockList.globalBlocksFolder` |
| Nature / trees | `EnableTreeInspector()` | `BlockList.globalTreeFolder` |
| Paint / materials | `EnablePaintInspector()` | `BlockList.globalPaintFolder` |

Members this mod reads (all `public` in the installed build):

| Member | Type | Why |
| --- | --- | --- |
| `central` | `LEV_LevelEditorCentral` | Click and keyboard-lock paths |
| `globalBlockList` | `BlockList` | The palette trees |
| `createBlocksGUI` | `RectTransform` | Whether the parts grid is on screen |
| `contentBox` | `RectTransform` | The box the game parents thumbnails into |
| `buttonPrefab2` | `ThumbnailButton` | The thumbnail prefab to clone |
| `scrollbar` | `UnityEngine.UI.Scrollbar` | Reset scroll position to the top |
| `inspectorTitle` | `TextMeshProUGUI` | Font for the search bar |
| `isPopulated` | `bool` | Set false while the grid is being rebuilt |
| `BlockHoverEnter(long, string)`, `BlockHoverEnterString(string, string)`, `BlockHoverExit()` | methods | The game's own hover title |

Members that are **private** and therefore deliberately not touched:
`allButtons2`, `blockThumbImages`, `testRect`, `currentBlocksFolder`, `CreateBlockGUI()`,
`CreateTreeGUI()`, `CreatePaintGUI()`. QuickFilter reaches into some of these; this mod does
not, which is what makes it survive a rebuild by QuickFilter or by the game.

## 2. The grid, as the game builds it

`CreateBlockGUI()` (private) does roughly:

1. `inspectorTitle.text = LocalizationManager.GetTranslation("Editor_Select_Block")`
2. return early if `isPopulated` is already true;
3. `num = folders.Count + blocks.Count` (+1 for the "one folder up" tile);
4. for each slot, `Instantiate(buttonPrefab2)`, parent it to `contentBox`, set the thumbnail
   sprite and `ApplyIcon(thumbnail_icon)`, and wire `button.onClick` / `onHoverEnter` /
   `onHoverExit`;
5. anchor each button with normalised anchors inside `contentBox` (3 columns, or 4 when the
   player enabled small thumbnails);
6. `isPopulated = true` and `scrollbar.value = rememberScrollbarValue`.

`DestroyBlockGUI()` clears `allButtons2` and sets `isPopulated = false`. Nothing else in the
box is destroyed.

**Consequence:** the mod can hide the buttons the game built, add its own as siblings in the
same `contentBox`, and restore the originals untouched when the query is cleared.

### Where the search bar sits

The bar is a sibling of the **ScrollRect** rect, anchored to that rect's top edge in the
parent's space. The ScrollRect's own top edge is then moved down by the bar's height, so the
bar owns a strip of the layout and the first row of thumbnails starts below it.

Insetting the `ScrollRect.viewport` alone is not enough: in the shipped inspector the content
does not move with it, and the top row of tiles stays behind the bar. A screenshot of build
0.1.0 confirmed this (tile pixels at y=185, the bar covering 185–216, content resuming at 217),
which is why the frame is shrunk instead. A one-frame check after placement still insets the
viewport by the remaining overlap, for hierarchies where the viewport is anchored to the panel
rather than stretched inside the frame.

## 3. Opening a folder

```csharp
private void GoNewFolder(BlocksFolder theFolder, int typeOfThing, bool folderUp = false)
```

`typeOfThing` is the palette discriminator: `0` blocks, `1` trees, anything else paints. The
mod reads it in a prefix (parameters `__0`, `__1`) to track which folder is open, which is what
`Scope = CurrentFolder` needs. `currentBlocksFolder` itself stays private.

## 4. Palette data

`BlockList`:

| Field | Type |
| --- | --- |
| `blocks` | `List<BlockProperties>` — flat list, indexed by block id |
| `globalBlocksFolder`, `globalTreeFolder`, `globalPaintFolder` | `BlocksFolder` |

`BlocksFolder` (a `MonoBehaviour`) holds `folderID`, `useLocalizedFolderName`,
`localizedName`, `hasParent`, `parent`, `progressionCheck`, and the four child lists:
`blocks` (`BlockProperties`), `folders` (`BlocksFolder`), `trees`
(`LEV_TreeGunTreeHolder`), `materials` (`MaterialHolder`).

Per-item data used for a result button:

| Type | Name key | Art | Id |
| --- | --- | --- | --- |
| `BlockProperties` | `bridge.localizedName` | `thumbnail`, `thumbnail_icon` | `blockID` |
| `LEV_TreeGunTreeHolder` | `localizedName` | `thumb`, `thumbnail_icon` | `trees_ID` |
| `MaterialHolder` | `localizedName` | `thumbnail`, `thumbnail_icon` | `materialID` |

Folder names come from `useLocalizedFolderName ? localizedName : "Block_Folder"`, translated,
which is exactly what the game's own hover title shows.

## 5. Clicking a result

The game's own public entry points are reused, so a search click is indistinguishable from a
folder click:

| Palette | Call |
| --- | --- |
| Blocks | `central.gizmos.CreateNewBlock(int blockID)` |
| Trees | `central.TREEGUNNNN.SetToTree(LEV_TreeGunTreeHolder)` |
| Paints | `central.painter.SetToCurrentPaint(MaterialHolder)` |

`LEV_GizmoHandler.CreateNewBlock(int)` is the same method the game's tile listeners call.

## 6. Keyboard capture

`LEV_Input` is a plain data holder of `InputActionScriptableObject`s plus one flag:

```csharp
public bool inputLocked;
```

The editor checks that flag before acting on a shortcut, for example in `LEV_GizmoHandler`:

```csharp
if (central.input.Delete.buttonDown && !central.input.inputLocked && ...)
```

and `LEV_MoveCamera` stops reading movement while it is set. `LEV_SaveLoad` sets it to `true`
on open and `false` on close, which is the game's own precedent for "a text field is being
typed into".

**Consequence:** the search bar sets `central.input.inputLocked = true` while
`TMP_InputField.isFocused`, remembers the value it found, and restores it on blur, on hide and
on destroy. Typing `save` searches; it does not save the level.

The pointer needs the same treatment: `LEV_CustomButton.OnPointerEnter` calls
`central.cam.OverrideOutsideGameView(true)` (and `false` on exit) so the cursor stays visible
over UI and a drag does not move the camera. `SearchBarPointer` does exactly the same for the
bar, and releases the override on disable so it cannot get stuck.

## 7. Localization

`I2.Loc.LocalizationManager.GetTranslation(string)` (assembly `l2Localization.dll`) returns
the name in the player's current language. The mod translates both part names and folder
names, so matching happens against the text the player can actually see. If a term is missing
the key itself is used, which is also what the game displays.

Note: the call must be fully qualified (`I2.Loc.LocalizationManager`) in this project,
because `LocalizationManager` resolves to an unrelated type once the game assembly and the
Unity modules are referenced together.

## 8. Why this is compatible with other mods

**QuickFilter** (mod.io 4702386) is the closest neighbour: it also changes the block
palette. It works by patching `CreateBlockGUI` with a prefix that returns `false` while it is
filtering, which skips the game's method, and then rebuilds the grid itself into
`inspector.contentBox` and `allButtons2`.

This mod takes a different route on purpose:

| Decision | Reason |
| --- | --- |
| No prefix on `CreateBlockGUI` | Replacing it would break QuickFilter; a postfix would not run when QuickFilter replaces it. |
| Only public members read | No `AccessTools` field access into the inspector, so an internal rename by another mod cannot throw. |
| Own buttons carry a `SearchResultMarker` | The game's (and QuickFilter's) buttons are identified by component, not by parsing names or list positions. |
| Results are siblings in `contentBox` | The existing ScrollRect, mask and scrollbar keep working unchanged. |
| Rebuild detection by object identity | If QuickFilter or the game replaces the grid while a query is active, the first foreign button changes and the results are re-applied. |
| Query state is ours | Nothing stores state on the game's objects, so entering and leaving the inspector is safe. |

ZeepSDK's `LevelEditorApi` does expose an inspector-awake event, but it is `internal`, and
taking a hard dependency on the SDK for one callback would make the mod fail whenever the SDK
lags a game update. A postfix on `LEV_Inspector.Awake` costs nothing and depends on nothing.

## 9. Deliberate limitations

- No pagination: results stop at `Search/MaxResults` (default 60, ceiling 200).
- `Scope = CurrentFolder` means the open folder's **direct** items, matching how the folder
  itself is displayed.
- Progression-locked folders are not hidden or filtered by the search: the mod indexes the
  palette exactly as loaded and never changes what the game will let the player place.
- The palette snapshot is rebuilt at most every ten seconds, so a block added by another mod
  mid-session can take up to ten seconds to become searchable.
- The bar is hidden (with one log line) if the inspector's public fields are ever renamed.

## 10. Reproducing the dumps

```bash
ZEEPKIST=Zeepkist_Data/Managed/Zeepkist.dll
REF=Zeepkist_Data/Managed

ilspycmd -l c             "$ZEEPKIST" | grep -i 'LEV_'
ilspycmd -t LEV_Inspector "$ZEEPKIST"
ilspycmd -t BlocksFolder  "$ZEEPKIST"
ilspycmd -t BlockList     "$ZEEPKIST"
ilspycmd -t BlockProperties "$ZEEPKIST"
ilspycmd -t MaterialHolder  "$ZEEPKIST"
ilspycmd -t LEV_TreeGunTreeHolder "$ZEEPKIST"
ilspycmd -t LEV_GizmoHandler "$ZEEPKIST" | grep -n 'CreateNewBlock'
ilspycmd -t LEV_Input        "$ZEEPKIST"
ilspycmd -t LEV_SaveLoad     "$ZEEPKIST" | grep -n 'inputLocked'
ilspycmd -t I2.Loc.LocalizationManager "Zeepkist_Data/Managed/l2Localization.dll" \
  | grep -n 'GetTranslation'
```
