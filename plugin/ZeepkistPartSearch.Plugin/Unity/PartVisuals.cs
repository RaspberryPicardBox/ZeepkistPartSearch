using UnityEngine;

namespace ZeepkistPartSearch.Unity
{
    /// <summary>
    /// The pieces the inspector needs to draw one result button. Kept in the Unity layer
    /// (it holds a Sprite), keyed by <see cref="Search.PartEntry.Key"/>.
    /// </summary>
    internal sealed class PartVisuals
    {
        public PartVisuals(Sprite thumbnail, ThumbnailIcon icon)
        {
            Thumbnail = thumbnail;
            Icon = icon;
        }

        public Sprite Thumbnail { get; }

        public ThumbnailIcon Icon { get; }
    }
}
