using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlus.Editor
{
    //this class allows you to set TpAnimatedTile as the default drag-drop-to-palette tile.
    //look in preferences pane/2d/tile palette
    public static class CreateTpAnimatedTile
    {
        [CreateTileFromPalette]
        public static TileBase TpAnimatedTile(Sprite sprite)
        {
            var tp = ScriptableObject.CreateInstance<TpAnimatedTile>();
            tp.sprite = sprite;
            tp.name = sprite.name;
            return tp;
        }
    }
}
