using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ZeepkistPartSearch.UI
{
    /// <summary>
    /// Tells the editor's camera that the pointer is over UI, exactly the way the game's own
    /// <see cref="LEV_CustomButton"/> does. Without it the cursor can stay hidden and a drag
    /// over the search bar can move the camera instead of selecting text.
    ///
    /// The override is always released again, including when the bar is hidden while hovered,
    /// so the camera never ends up stuck in "pointer is over UI" mode.
    /// </summary>
    internal sealed class SearchBarPointer : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private LEV_Inspector inspector;
        private bool inside;

        public void Initialize(LEV_Inspector owner)
        {
            inspector = owner;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            inside = true;
            SetOverride(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (!inside)
            {
                return;
            }

            inside = false;
            SetOverride(false);
        }

        private void OnDisable()
        {
            if (!inside)
            {
                return;
            }

            inside = false;
            SetOverride(false);
        }

        private void SetOverride(bool outsideGameView)
        {
            try
            {
                LEV_LevelEditorCentral central = inspector != null ? inspector.central : null;
                if (central != null && central.cam != null)
                {
                    central.cam.OverrideOutsideGameView(outsideGameView);
                }
            }
            catch (Exception)
            {
                // A missing camera is not worth an exception while the pointer moves.
            }
        }
    }
}
