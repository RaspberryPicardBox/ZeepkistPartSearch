using UnityEngine;

namespace ZeepkistPartSearch.UI
{
    /// <summary>
    /// Tag component on the thumbnails this mod creates. It lets the panel tell its own
    /// result buttons apart from the game's (and from any other mod's) without touching
    /// the inspector's private button list.
    /// </summary>
    internal sealed class SearchResultMarker : MonoBehaviour
    {
    }
}
