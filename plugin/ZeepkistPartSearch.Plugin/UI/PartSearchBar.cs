using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ZeepkistPartSearch.UI
{
    /// <summary>
    /// Builds the search bar itself: a TextMeshPro input field with the same font the
    /// inspector already uses, so it looks like part of the game's UI rather than a bolted-on
    /// overlay.
    ///
    /// The object is assembled while inactive so <see cref="TMP_InputField"/> never wakes up
    /// with a half-configured text component.
    /// </summary>
    internal static class PartSearchBar
    {
        public static GameObject Create(Transform parent, TMP_FontAsset font, int height, string placeholder,
            out TMP_InputField input, out TextMeshProUGUI text)
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
            background.color = new Color(0.07f, 0.07f, 0.08f, 0.96f);
            background.raycastTarget = true;

            GameObject viewportObject = new GameObject("Text Area", typeof(RectTransform));
            viewportObject.transform.SetParent(root.transform, false);
            RectTransform viewport = (RectTransform)viewportObject.transform;
            viewport.anchorMin = Vector2.zero;
            viewport.anchorMax = Vector2.one;
            viewport.offsetMin = new Vector2(8f, 3f);
            viewport.offsetMax = new Vector2(-8f, -3f);
            viewportObject.AddComponent<RectMask2D>();

            TextMeshProUGUI textComponent = CreateText("Text", viewport, font,
                new Color(0.96f, 0.96f, 0.96f, 1f));
            TextMeshProUGUI placeholderComponent = CreateText("Placeholder", viewport, font,
                new Color(0.62f, 0.62f, 0.62f, 0.85f));
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
            field.caretColor = new Color(1f, 0.85f, 0.2f, 1f);
            field.caretWidth = 1;
            field.selectionColor = new Color(0.35f, 0.55f, 0.9f, 0.45f);

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
