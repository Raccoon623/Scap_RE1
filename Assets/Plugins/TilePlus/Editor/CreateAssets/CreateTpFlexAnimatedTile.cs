using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlus.Editor
{
    //this class allows you to set TpFlexAnimatedTile as the default drag-drop-to-palette tile.
    //look in preferences pane/2d/tile palette
    public static class CreateTpFlexAnimatedTile
    {
        [CreateTileFromPalette]
        public static TileBase TpFlexAnimatedTile(Sprite sprite)
        {
            var tp = ScriptableObject.CreateInstance<TpFlexAnimatedTile>();
            tp.sprite = sprite;
            tp.name = sprite.name;
            return tp;
        }
    }

}
