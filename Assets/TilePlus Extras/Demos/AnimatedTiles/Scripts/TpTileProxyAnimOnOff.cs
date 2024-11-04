
using TilePlus;
using UnityEngine;
#nullable enable

namespace TilePlusDemo
{
    /// <summary>
    /// An example of how to control a tile from a button.
    /// PLEASE NOTE: the scene is set up for both tiles to run
    /// one-shot animations. 
    /// </summary>
    public class TpTileProxyAnimOnOff : MonoBehaviour
    {
        /// <summary>
        /// Toggle a tile's animation given the GUID of said tile.
        /// </summary>
        /// <param name="guid"></param>
        public void ToggleAnimation(string guid)
        { 
           //The GUID comes from the button's OnClick parameter field 
           //you can copy it from a tile by doing this:
           // 1. Select the tile with the Tile+Brush or the Utility Window
           // 2. Click the inspector's toolbar 'Copy GUID' button (looks like an eyedropper)
           // 3. The GUID will be on the clipboard. You can use CTRL+V  (Win) to paste it
           //    into the 'OnClick' field of a button. 
           // Note that tile GUIDs are persistent but only apply to a single tile instance.
           // If you delete a tile and replace it with a new one OR you use a copy/paste
           // operation, then the replacement tile or the pasted tile (which is a new copy)
           // has a different GUID. 
           //
           var tile = TpLib.GetTilePlusBaseFromGuid(guid); //try to lookup a tile from the GUID
           //do what's appropriate given the result. 
           if (tile == null) //nothing was found
               Debug.LogError($"GUID {guid} not found");
           else if (tile is TpAnimatedTile tpa) //this is a TpAnimatedTile
               tpa.ActivateAnimation(!tpa.AnimationIsRunning);
           else if (tile is TpFlexAnimatedTile tpf) //this is a TpFlexAnimatedTile.
               tpf.ActivateAnimation(!tpf.AnimationIsRunning);
           else //it wasn't an animated tile...
               Debug.LogError($"Tile with GUID {guid} was not an animated tile.");
        }
    }
}
