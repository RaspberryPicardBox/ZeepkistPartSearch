using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZeepkistPartSearch.UI
{
    /// <summary>
    /// Builds the search bar itself: a TextMeshPro input field styled as one of the inspector's
    /// own tile plates, using the same font and (when it can be found) the same rounded sprite
    /// the thumbnails use.
    ///
    /// The colours are Zeepkist's own, sampled from the parts grid: the pale yellow plate
    /// (#FFE873) and a dark ink for the text, so the field reads as part of the inspector
    /// rather than an overlay.
    ///
    /// The object is assembled while inactive so <see cref="TMP_InputField"/> never wakes up
    /// with a half-configured text component.
    /// </summary>
    internal static class PartSearchBar
    {
        private static readonly Color PlateColour = new Color(1f, 0.909804f, 0.450980f, 1f);
        private static readonly Color InkColour = new Color(0.286275f, 0.176471f, 0.050980f, 1f);
        private static readonly Color AccentColour = new Color(1f, 0.572549f, 0f, 1f);

        /// <summary>The sampled tile-plate colour, used when the game's own plate cannot be read.</summary>
        internal static Color DefaultPlateColour
        {
            get { return PlateColour; }
        }

        public static GameObject Create(Transform parent, TMP_FontAsset font, int height, string placeholder,
            Sprite plate, Color plateTint, out TMP_InputField input, out TextMeshProUGUI text)
        {
            input = null;
            text = null;

            if (parent == null)
            {
                return null;
            }

            GameObject root = new GameObject("Parts Search Bar", typeof(RectTransform));
            root.SetActive(false);
            root.transform.SetParent(parent, false);

            Image background = root.AddComponent<Image>();
            background.raycastTarget = true;
            background.color = plate != null ? plateTint : PlateColour;

            if (plate != null)
            {
                background.sprite = plate;
                background.type = plate.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            }

            GameObject viewportObject = new GameObject("Text Area", typeof(RectTransform));
            viewportObject.transform.SetParent(root.transform, false);
            RectTransform viewport = (RectTransform)viewportObject.transform;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(10f, 3f);
            viewport.offsetMax = new Vector2(-10f, -3f);
            viewportObject.AddComponent<RectMask2D>();

            TextMeshProUGUI textComponent = CreateText("Text", viewport, font, InkColour);
            TextMeshProUGUI placeholderComponent = CreateText("Placeholder", viewport, font,
                new Color(InkColour.r, InkColour.g, InkColour.b, 0.45f));
            placeholderComponent.text = string.IsNullOrEmpty(placeholder) ? ModConfig.DefaultPlaceholder : placeholder;

            TMP_InputField field = root.AddComponent<TMP_InputField>();
            field.textViewport = viewport;
            field.textComponent = textComponent;
            field.placeholder = placeholderComponent;
            field.targetGraphic = background;
            field.lineType = TMP_InputField.LineType.SingleLine;
            field.characterLimit = Search.SearchOptions.MaxQueryLength;
            field.richText = false;
            field.onFocusSelectAll = false;
            field.customCaretColor = true;
            field.caretColor = InkColour;
            field.caretWidth = 2;
            field.selectionColor = new Color(AccentColour.r, AccentColour.g, AccentColour.b, 0.35f);

            RectTransform rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(0f, height);

            root.SetActive(true);

            input = field;
            text = textComponent;
            return root;
        }

        private static TextMeshProUGUI CreateText(string name, RectTransform parent, TMP_FontAsset font, Color colour)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(parent, false);

            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                text.font = font;
            }

            text.fontSize = 16f;
            text.color = colour;
            text.alignment = TextAlignmentOptions.Left;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.richText = false;
            text.raycastTarget = false;
            text.margin = Vector4.zero;

            return text;
        }
    }
}
