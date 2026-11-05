using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ZeepkistPartSearch.Search;
using ZeepkistPartSearch.Unity;

namespace ZeepkistPartSearch.UI
{
    /// <summary>
    /// The search bar at the top of the level editor's parts selector, and the result grid it
    /// draws in place of the open folder's thumbnails while a query is active.
    ///
    /// It deliberately does not replace the game's <c>CreateBlockGUI</c>: this component hides
    /// the thumbnails the game (or another mod, such as QuickFilter) has already built, puts its
    /// own in their place, and puts the originals back the moment the query is cleared. Nothing
    /// private to the inspector is read or written, so a rebuild by the game or by another mod
    /// is detected and simply re-applied.
    /// </summary>
    internal sealed class PartSearchPanel : MonoBehaviour
    {
        private const float RescanInterval = 0.25f;
        private const float TypingDelay = 0.15f;
        private const float CatalogLifetime = 10f;

        private LEV_Inspector inspector;
        private ModConfig config;

        private GameObject bar;
        private RectTransform barRect;
        private TMP_InputField input;

        private RectTransform contentBox;
        private RectTransform frame;
        private Vector2 frameOffsetMax;
        private Vector2 frameSizeDelta;
        private Vector2 frameAnchoredPosition;
        private bool frameShrunk;

        private ScrollRect scrollRect;
        private RectTransform viewport;
        private Vector2 viewportOffsetMax;
        private bool viewportInset;
        private float verifyAt;
        private bool verified;

        private readonly List<ThumbnailButton> results = new List<ThumbnailButton>();
        private readonly List<ThumbnailButton> hiddenGameButtons = new List<ThumbnailButton>();
        private ThumbnailButton firstHiddenButton;
        private Vector2 contentSizeBefore;
        private bool contentCaptured;

        private PartKind mode = PartKind.Block;
        private BlocksFolder currentFolder;

        private BuiltPartCatalog built;
        private BlockList builtFor;
        private PartKind builtKind = (PartKind)(-1);
        private float builtAt;

        private string appliedQuery = string.Empty;
        private string pendingQuery = string.Empty;
        private float applyAt;
        private bool applyPending;
        private bool needsReapply;

        private float nextRescan;
        private bool lockedInput;
        private bool inputWasLocked;
        private bool warnedNoContentBox;
        private bool barPlaced;
        private bool autoFocused;

        public void Initialize(LEV_Inspector owner, ModConfig modConfig)
        {
            inspector = owner;
            config = modConfig;

            if (inspector != null && inspector.globalBlockList != null)
            {
                currentFolder = inspector.globalBlockList.globalBlocksFolder;
            }

            BuildBar();
        }

        private void BuildBar()
        {
            if (inspector == null || config == null)
            {
                return;
            }

            contentBox = inspector.contentBox;
            if (contentBox == null)
            {
                if (!warnedNoContentBox)
                {
                    warnedNoContentBox = true;
                    Plugin.LogError("Parts Search: the inspector has no content box; the search bar is disabled.");
                }

                return;
            }

            ScrollRect scroll = contentBox.GetComponentInParent<ScrollRect>();

            // The parts area is the ScrollRect: its rect is the thing whose top has to move
            // down to make room. Insetting the viewport alone does not move the content, so
            // the first row of thumbnails would stay behind the bar.
            if (scroll != null)
            {
                frame = (RectTransform)scroll.transform;
                scrollRect = scroll;
                viewport = scroll.viewport;
            }
            else if (inspector.createBlocksGUI != null)
            {
                frame = inspector.createBlocksGUI;
            }

            Transform parent = frame != null ? frame.parent : contentBox.parent;
            if (parent == null)
            {
                return;
            }

            TMP_FontAsset font = inspector.inspectorTitle != null ? inspector.inspectorTitle.font : null;
            if (font == null)
            {
                font = TMP_Settings.defaultFontAsset;
            }

            TMP_InputField field;
            TextMeshProUGUI text;
            Color plateTint;
            Sprite plate = FindPlate(out plateTint);
            bar = PartSearchBar.Create(parent, font, config.BarHeight, config.PlaceholderText,
                plate, plateTint, out field, out text);
            if (bar == null)
            {
                return;
            }

            input = field;
            barRect = (RectTransform)bar.transform;

            bar.AddComponent<SearchBarPointer>().Initialize(inspector);

            PlaceBar();

            if (input != null)
            {
                input.onValueChanged.AddListener(OnValueChanged);
            }
        }

        /// <summary>
        /// The rounded plate the game draws behind every thumbnail, and the colour it draws it
        /// in, so the bar shares the inspector's exact corner art and hue instead of an
        /// approximation. The sprite is null when the prefab does not expose one, in which case
        /// the bar falls back to the sampled plate colour.
        /// </summary>
        private Sprite FindPlate(out Color tint)
        {
            tint = PartSearchBar.DefaultPlateColour;

            if (inspector == null || inspector.buttonPrefab2 == null)
            {
                return null;
            }

            LEV_CustomButton button = inspector.buttonPrefab2.button;
            if (button != null && button.buttonImage != null && button.buttonImage.sprite != null)
            {
                Color normal = button.normalColor;
                if (normal.a > 0.5f)
                {
                    normal.a = 1f;
                    tint = normal;
                }

                return button.buttonImage.sprite;
            }

            Image image = inspector.buttonPrefab2.GetComponent<Image>();
            return image != null ? image.sprite : null;
        }

        /// <summary>
        /// Puts the bar in the strip directly above the parts area and shrinks that area from
        /// the top, so the bar owns real layout space instead of covering the first row of
        /// thumbnails. Other mods' elements are not moved.
        /// </summary>
        private void PlaceBar()
        {
            if (barRect == null || barPlaced || frame == null)
            {
                return;
            }

            RectTransform parent = barRect.parent as RectTransform;
            if (parent == null)
            {
                return;
            }

            Rect frameRect = frame.rect;
            Rect parentRect = parent.rect;
            if (Mathf.Approximately(frameRect.width, 0f) || Mathf.Approximately(frameRect.height, 0f)
                || Mathf.Approximately(parentRect.width, 0f) || Mathf.Approximately(parentRect.height, 0f))
            {
                // The canvas has not been laid out yet; Update tries again.
                return;
            }

            Vector3[] corners = new Vector3[4];
            frame.GetWorldCorners(corners); // 0 bottom-left, 1 top-left, 2 top-right, 3 bottom-right

            Vector3 topLeft = parent.InverseTransformPoint(corners[1]);
            Vector3 topRight = parent.InverseTransformPoint(corners[2]);

            float xMin = Mathf.Clamp01((topLeft.x - parentRect.xMin) / parentRect.width);
            float xMax = Mathf.Clamp01((topRight.x - parentRect.xMin) / parentRect.width);
            float yTop = Mathf.Clamp01((topLeft.y - parentRect.yMin) / parentRect.height);

            if (xMax <= xMin)
            {
                xMin = 0.05f;
                xMax = 0.95f;
            }

            float height = Mathf.Max(18f, config.BarHeight);

            barRect.anchorMin = new Vector2(xMin, yTop);
            barRect.anchorMax = new Vector2(xMax, yTop);
            barRect.pivot = new Vector2(0.5f, 1f);
            barRect.sizeDelta = new Vector2(0f, height);
            barRect.anchoredPosition = Vector2.zero;
            barRect.SetAsLastSibling();

            // Move the parts area down by exactly the strip the bar now occupies. The bar's
            // bottom and the frame's new top line up, with a two pixel seam.
            frameOffsetMax = frame.offsetMax;
            frameSizeDelta = frame.sizeDelta;
            frameAnchoredPosition = frame.anchoredPosition;
            ShrinkTop(frame, height + 2f);
            frameShrunk = true;

            barPlaced = true;
            verifyAt = Time.unscaledTime + 0.25f;
            verified = false;
        }

        /// <summary>
        /// One-frame safety net for hierarchies where the scroll view's viewport is anchored to
        /// the panel rather than stretched inside the frame: if the viewport still reaches above
        /// the bar's bottom edge, inset it by exactly the offending amount so the content also
        /// starts below the bar.
        /// </summary>
        private void VerifyNoOverlap()
        {
            if (barRect == null || scrollRect == null || viewport == null)
            {
                return;
            }

            Vector3[] viewportCorners = new Vector3[4];
            viewport.GetWorldCorners(viewportCorners);

            Vector3[] barCorners = new Vector3[4];
            barRect.GetWorldCorners(barCorners);

            float overlap = viewportCorners[1].y - barCorners[0].y; // viewport top above bar bottom
            if (overlap <= 1f)
            {
                return;
            }

            viewportOffsetMax = viewport.offsetMax;
            viewport.offsetMax = new Vector2(viewportOffsetMax.x, viewportOffsetMax.y - overlap - 1f);
            viewportInset = true;
        }

        /// <summary>
        /// Reduces a rect's height by <paramref name="amount"/> from the top edge, keeping the
        /// bottom edge where it is. Handles both anchor-stretched and fixed-height rects, which
        /// is the difference between "offsetMax" and "sizeDelta + position".
        /// </summary>
        private static void ShrinkTop(RectTransform rect, float amount)
        {
            if (rect == null || amount <= 0f)
            {
                return;
            }

            if (!Mathf.Approximately(rect.anchorMin.y, rect.anchorMax.y))
            {
                Vector2 offsetMax = rect.offsetMax;
                rect.offsetMax = new Vector2(offsetMax.x, offsetMax.y - amount);
                return;
            }

            float pivotY = rect.pivot.y;
            Vector2 size = rect.sizeDelta;
            Vector2 position = rect.anchoredPosition;

            rect.sizeDelta = new Vector2(size.x, Mathf.Max(size.y - amount, 0f));
            rect.anchoredPosition = new Vector2(position.x, position.y - pivotY * amount);
        }

        /// <summary>Called by the patches when the game switches to blocks, trees or paints.</summary>
        public void SetMode(PartKind kind, BlocksFolder folder)
        {
            bool changed = mode != kind;
            mode = kind;
            currentFolder = folder;

            if (changed || built == null)
            {
                built = null;
                builtAt = 0f;
            }

            ClearResults();
            appliedQuery = string.Empty;
            applyPending = false;
            needsReapply = input != null && !string.IsNullOrEmpty(input.text);
        }

        private void Update()
        {
            if (inspector == null || bar == null || config == null)
            {
                return;
            }

            bool gridVisible = inspector.createBlocksGUI != null && inspector.createBlocksGUI.gameObject.activeInHierarchy;
            bool show = config.Enabled && IsModeEnabled(mode) && gridVisible;

            if (bar.activeSelf != show)
            {
                bar.SetActive(show);
            }

            if (!show)
            {
                if (results.Count > 0 || hiddenGameButtons.Count > 0)
                {
                    ClearResults();
                }

                if (appliedQuery.Length > 0)
                {
                    appliedQuery = string.Empty;
                    needsReapply = true;
                }

                ReleaseInputLock();
                return;
            }

            if (!barPlaced)
            {
                PlaceBar();
            }

            if (barPlaced && !verified && Time.unscaledTime >= verifyAt)
            {
                verified = true;
                VerifyNoOverlap();
            }

            if (!autoFocused && config.AutoFocus && input != null)
            {
                autoFocused = true;
                input.ActivateInputField();
            }

            if (needsReapply)
            {
                needsReapply = false;
                if (input != null && !string.IsNullOrEmpty(input.text))
                {
                    Apply(input.text);
                }
            }

            if (applyPending && Time.unscaledTime >= applyAt)
            {
                applyPending = false;
                Apply(pendingQuery);
            }

            if (appliedQuery.Length > 0 && Time.unscaledTime >= nextRescan)
            {
                nextRescan = Time.unscaledTime + RescanInterval;
                if (GridChanged())
                {
                    Apply(appliedQuery);
                }
            }

            SyncInputLock();
        }

        private void OnDisable()
        {
            ReleaseInputLock();
        }

        private bool IsModeEnabled(PartKind kind)
        {
            switch (kind)
            {
                case PartKind.Tree:
                    return config.IncludeTrees;
                case PartKind.Paint:
                    return config.IncludePaints;
                default:
                    return true;
            }
        }

        private void Apply(string rawQuery)
        {
            if (inspector == null || config == null || contentBox == null)
            {
                return;
            }

            string query = PartQuery.NormaliseQuery(rawQuery, config.MaxQueryLength);
            appliedQuery = query;

            if (query.Length < config.MinimumCharacters)
            {
                ClearResults();
                return;
            }

            BuiltPartCatalog catalog = GetCatalog();
            if (catalog == null)
            {
                ClearResults();
                return;
            }

            PartFolder folder = null;
            if (currentFolder != null)
            {
                catalog.FolderBySource.TryGetValue(currentFolder, out folder);
            }

            List<PartEntry> matches = PartQuery.Search(catalog.Catalog, folder, query, config.BuildSearchOptions());
            Render(catalog, matches);
        }

        private BuiltPartCatalog GetCatalog()
        {
            BlockList list = inspector != null ? inspector.globalBlockList : null;
            if (list == null)
            {
                return null;
            }

            bool stale = built == null
                         || builtFor != list
                         || builtKind != mode
                         || Time.unscaledTime - builtAt > CatalogLifetime;
            if (!stale)
            {
                return built;
            }

            try
            {
                switch (mode)
                {
                    case PartKind.Tree:
                        built = PartCatalogBuilder.BuildTrees(list);
                        break;
                    case PartKind.Paint:
                        built = PartCatalogBuilder.BuildPaints(list);
                        break;
                    default:
                        built = PartCatalogBuilder.BuildBlocks(list);
                        break;
                }
            }
            catch (Exception exception)
            {
                Plugin.LogError("Parts Search: building the parts list failed: " + exception);
                built = null;
            }

            builtFor = list;
            builtKind = mode;
            builtAt = Time.unscaledTime;

            return built;
        }

        private void Render(BuiltPartCatalog catalog, List<PartEntry> matches)
        {
            HideGameButtons();

            for (int i = 0; i < results.Count; i++)
            {
                if (results[i] == null)
                {
                    continue;
                }

                results[i].gameObject.SetActive(false);
                Destroy(results[i].gameObject);
            }

            results.Clear();

            if (matches.Count == 0)
            {
                return;
            }

            int columns = config.ColumnsPerRow;
            float rows = Mathf.Max(1f, Mathf.Ceil((float)matches.Count / columns));
            float contentWidth = Mathf.Max(contentBox.rect.width, 1f);

            if (inspector.buttonPrefab2 == null)
            {
                Plugin.LogError("Parts Search: the inspector has no thumbnail prefab; results cannot be drawn.");
                return;
            }

            // Square cells: the content grows by however many rows the matches need.
            contentBox.sizeDelta = new Vector2(0f, contentWidth * rows / columns);

            for (int i = 0; i < matches.Count; i++)
            {
                PartEntry entry = matches[i];

                PartVisuals visuals;
                catalog.Visuals.TryGetValue(entry.Key, out visuals);

                ThumbnailButton button = Instantiate(inspector.buttonPrefab2, contentBox, false);
                if (button == null)
                {
                    continue;
                }

                button.gameObject.name = "Parts Search Result - " + entry.Name;
                button.gameObject.SetActive(true);
                button.gameObject.AddComponent<SearchResultMarker>();

                if (button.button != null)
                {
                    button.button.central = inspector.central;
                    ClearListeners(button.button);

                    PartEntry captured = entry;
                    button.button.onClick.AddListener(() => Select(catalog, captured));
                    button.button.onHoverEnter.AddListener(() => Hover(captured));
                    button.button.onHoverExit.AddListener(HoverExit);
                }

                if (button.thumbnailImage != null && visuals != null && visuals.Thumbnail != null)
                {
                    button.thumbnailImage.sprite = visuals.Thumbnail;
                }

                button.ApplyIcon(visuals != null ? visuals.Icon : null);
                LayoutButton(button, i, columns, rows);
                results.Add(button);
            }

            if (inspector.scrollbar != null)
            {
                inspector.scrollbar.value = 1f;
            }
        }

        private static void LayoutButton(ThumbnailButton button, int index, int columns, float rows)
        {
            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null)
            {
                return;
            }

            float cellWidth = 1f / columns;
            float cellHeight = 1f / rows;
            int column = index % columns;
            int row = index / columns;

            float xMin = column * cellWidth;
            float yMax = 1f - row * cellHeight;

            rect.anchorMin = new Vector2(xMin, yMax - cellHeight);
            rect.anchorMax = new Vector2(xMin + cellWidth, yMax);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
        }

        /// <summary>
        /// Hides the thumbnails the game (or another mod) has already put in the content box.
        /// The originals stay alive and are simply re-shown when the query is cleared.
        /// </summary>
        private void HideGameButtons()
        {
            if (contentBox == null)
            {
                return;
            }

            if (!contentCaptured)
            {
                contentSizeBefore = contentBox.sizeDelta;
                contentCaptured = true;
            }

            hiddenGameButtons.Clear();

            for (int i = 0; i < contentBox.childCount; i++)
            {
                Transform child = contentBox.GetChild(i);
                if (child == null || child.GetComponent<SearchResultMarker>() != null)
                {
                    continue;
                }

                ThumbnailButton button = child.GetComponent<ThumbnailButton>();
                if (button == null)
                {
                    continue;
                }

                if (button.gameObject.activeSelf)
                {
                    button.gameObject.SetActive(false);
                }

                hiddenGameButtons.Add(button);
            }

            firstHiddenButton = hiddenGameButtons.Count > 0 ? hiddenGameButtons[0] : null;
        }

        private void ClearResults()
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (results[i] != null)
                {
                    results[i].gameObject.SetActive(false);
                    Destroy(results[i].gameObject);
                }
            }

            results.Clear();

            for (int i = 0; i < hiddenGameButtons.Count; i++)
            {
                if (hiddenGameButtons[i] != null)
                {
                    hiddenGameButtons[i].gameObject.SetActive(true);
                }
            }

            hiddenGameButtons.Clear();
            firstHiddenButton = null;

            if (contentCaptured && contentBox != null)
            {
                contentBox.sizeDelta = contentSizeBefore;
                contentCaptured = false;
            }
        }

        // ---------------------------------------------------------------- interaction

        private void Select(BuiltPartCatalog catalog, PartEntry entry)
        {
            if (catalog == null || entry == null || inspector == null)
            {
                return;
            }

            try
            {
                LEV_LevelEditorCentral central = inspector.central;
                if (central == null)
                {
                    return;
                }

                switch (entry.Kind)
                {
                    case PartKind.Block:
                        if (central.gizmos != null)
                        {
                            central.gizmos.CreateNewBlock(entry.Id);
                        }

                        break;

                    case PartKind.Tree:
                    {
                        UnityEngine.Object source;
                        if (central.TREEGUNNNN != null && catalog.Sources.TryGetValue(entry.Key, out source))
                        {
                            LEV_TreeGunTreeHolder tree = source as LEV_TreeGunTreeHolder;
                            if (tree != null)
                            {
                                central.TREEGUNNNN.SetToTree(tree);
                            }
                        }

                        break;
                    }

                    case PartKind.Paint:
                    {
                        UnityEngine.Object source;
                        if (central.painter != null && catalog.Sources.TryGetValue(entry.Key, out source))
                        {
                            MaterialHolder material = source as MaterialHolder;
                            if (material != null)
                            {
                                central.painter.SetToCurrentPaint(material);
                            }
                        }

                        break;
                    }
                }
            }
            catch (Exception exception)
            {
                Plugin.LogError("Parts Search: selecting a result failed: " + exception.Message);
            }
        }

        private void Hover(PartEntry entry)
        {
            if (inspector == null || entry == null)
            {
                return;
            }

            if (entry.Kind == PartKind.Tree)
            {
                inspector.BlockHoverEnterString(entry.SourceId, entry.Term);
            }
            else
            {
                inspector.BlockHoverEnter(entry.Id, entry.Term);
            }
        }

        private void HoverExit()
        {
            if (inspector != null)
            {
                inspector.BlockHoverExit();
            }
        }

        private void OnValueChanged(string value)
        {
            if (config == null)
            {
                return;
            }

            pendingQuery = value ?? string.Empty;

            // Clearing the bar restores the folder straight away; typing is debounced so a
            // fast typist does not rebuild the thumbnail grid on every key.
            if (PartQuery.NormaliseQuery(pendingQuery, config.MaxQueryLength).Length == 0)
            {
                applyPending = false;
                Apply(pendingQuery);
                return;
            }

            applyAt = Time.unscaledTime + TypingDelay;
            applyPending = true;
        }

        /// <summary>
        /// While the bar has focus the editor's keyboard shortcuts are locked through the
        /// game's own <see cref="LEV_Input.inputLocked"/> flag, so typing "save" searches
        /// instead of saving the level. The previous value is restored on blur.
        /// </summary>
        private void SyncInputLock()
        {
            LEV_Input editorInput = inspector != null && inspector.central != null ? inspector.central.input : null;
            if (editorInput == null)
            {
                return;
            }

            bool focused = input != null && input.isFocused && bar != null && bar.activeInHierarchy;

            if (focused && !lockedInput)
            {
                inputWasLocked = editorInput.inputLocked;
                editorInput.inputLocked = true;
                lockedInput = true;
            }
            else if (!focused && lockedInput)
            {
                editorInput.inputLocked = inputWasLocked;
                lockedInput = false;
            }
        }

        private void ReleaseInputLock()
        {
            if (!lockedInput)
            {
                return;
            }

            LEV_Input editorInput = inspector != null && inspector.central != null ? inspector.central.input : null;
            if (editorInput != null)
            {
                editorInput.inputLocked = inputWasLocked;
            }

            lockedInput = false;
        }

        /// <summary>
        /// True when the game (or another mod) has rebuilt the grid underneath us, so the
        /// results have to be laid out again. Checked by identity, not by count: a different
        /// folder with the same number of parts is still a different grid.
        ///
        /// <c>isPopulated</c> is deliberately not used: it is false whenever the palette is
        /// empty, which would re-apply a query forever on a palette with no parts.
        /// </summary>
        private bool GridChanged()
        {
            return FindFirstForeignButton() != firstHiddenButton;
        }

        private ThumbnailButton FindFirstForeignButton()
        {
            if (contentBox == null)
            {
                return null;
            }

            for (int i = 0; i < contentBox.childCount; i++)
            {
                Transform child = contentBox.GetChild(i);
                if (child == null || child.GetComponent<SearchResultMarker>() != null)
                {
                    continue;
                }

                ThumbnailButton button = child.GetComponent<ThumbnailButton>();
                if (button != null)
                {
                    return button;
                }
            }

            return null;
        }

        private static void ClearListeners(LEV_CustomButton button)
        {
            button.onClick.RemoveAllListeners();
            button.onHoverEnter.RemoveAllListeners();
            button.onHoverExit.RemoveAllListeners();

            DisablePersistentListeners(button.onClick);
            DisablePersistentListeners(button.onHoverEnter);
            DisablePersistentListeners(button.onHoverExit);
        }

        private static void DisablePersistentListeners(UnityEngine.Events.UnityEventBase unityEvent)
        {
            if (unityEvent == null)
            {
                return;
            }

            for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                unityEvent.SetPersistentListenerState(i, UnityEngine.Events.UnityEventCallState.Off);
            }
        }

        private void OnDestroy()
        {
            ClearResults();
            ReleaseInputLock();

            if (input != null)
            {
                input.onValueChanged.RemoveListener(OnValueChanged);
            }

            if (frameShrunk && frame != null)
            {
                frame.offsetMax = frameOffsetMax;
                frame.sizeDelta = frameSizeDelta;
                frame.anchoredPosition = frameAnchoredPosition;
                frameShrunk = false;
            }

            if (viewportInset && viewport != null)
            {
                viewport.offsetMax = viewportOffsetMax;
                viewportInset = false;
            }
        }
    }
}
