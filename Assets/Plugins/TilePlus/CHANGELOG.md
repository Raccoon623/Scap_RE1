# Changelog

## [NOTES]
 - If upgrading from an earlier version, delete the Plugins/TilePlus folder, upgrade, then after the post-upgrade compilation has completed, please close and re-open the Unity Editor.
 - Please note that documentation can be found in the TilePlusExtras folder.
   - The current Source API reference documentation is based on Version 3.0.0.0.  The filename indicates the version.
 - The TpLib/TpLibEditor version numbers can be found by using the System Information window, available from the Tools menu.
 - If you want to use DOTween please read the user/programmer's guide for more information.
 - Upgrading from V1.X? Newer versions are internally very different. It is advised to use a copy of your existing project when upgrading.
  - Unity6 .0 thru .4 are not supported
 
### Important note: requires Unity 2022.3 LTS or newer. Tested with several 2023.x versions and Unity6 Preview. 

### UNITY 2023 VERSIONS: the default shortcut for area selection (ALT+1) conflicts with a new Unity shortcut in some of these versions. Open the Unity shortcut editor, click the ALT button in the display, and right-click the 1 button to remove the Unity shortcuts.

### [3.3.0] 2024-06-17

### Added
- Editor update rate added to System Information window.
- TpLib
  - added TpLibEditor as friend assembly.
  - added a new Property InhibitTilemapCallbacks. This can be used to inhibit TpLib from responding to Tilemap callbacks for certain use cases. 
- The Bundler (Tools/TilePlus/Prefabs/Bundle Tilemaps or the similarly-named hierarchy context menu item or from the Painter Grid Selections menu) will ask you for a base name for the created assets. If you don't provide a name (if the text field is empty when the dialog closes) then the Grid and Tilemap names are used as before. The name of the parent scene is provided as a default.
- TpTileBundle and TpTileFab were updated to Version 3. Added an 'Icon' field. 
  - If this isn't null, then Painter will show this sprite next to the name of the item in the Center column in Paint mode as well as showing this sprite at the top of the right column ('Brush inspector') in Paint mode.
  - This is useful when deconstructing large Palettes into small TileFabs; for example, a completed structure comprising several tiles can be painted into a scene and saved as a Tile Bundle. The icon allows a visual cue for what the Bundle will paint.
  - See the "What's New" document for more details.
- Tile+Painter: 
  - The History List has been renamed to "Favorites". Additionally, it's now serialized in TilePlusPainterFavorites. 
    - That's a Scriptable Singleton and the contents will persist between sessions unless you clear it using Tools/TilePlus/Clear Painter Favorites.
  - Painter's window menu has a *Refresh Tp System* function which duplicates the functionality of the Refresh button at the lower left corner of the Painter window.
  - Favorites is no longer cleared when using the Refresh button at the lower left corner of the Painter Window.
    - A new Tools/TilePlus/Clear Painter Favorites menu item has been added.
    - Painter's window menu also provides a *Clear Painter Favorites' function.
    - The Favorites list is limited to 32 items of two different types. When that limit is reached or exceeded then the list is truncated.
    - Each individual Favorites item in the right column has an added 'X' deletion button. Click 'X' to delete that item.
  - The Clipboard section has four convenience buttons: Clear Clipboard ("X"), Add to Favorites ("F"), Bundle ("B") and Add Icon ("I"). The last two are only active when the Clipboard contains a multiple-selection item.
    - Clicking or CTRL-Clicking the Clipboard image no longer has any effect.
  - Multiple selection PICKs made in the scene with CTRL held down are added to the Favorites list. 
    - Multiple selection Picks in the Clipboard are added to Favorites with a new "F" button.
    - These picks have special handling when painted: you can rotate or flip individual tiles (the standard shortcuts) or hold down ALT to rotate/flip the entire pick.
    - Favorites tries to detect duplicates which is more difficult for Multiple Selection picks. It doesn't examine all of the tiles within the Selection as that could be too slow.
    - Multiple-selection picks added to favorites or Bundled with the "B" button cause an icon to be created for the pick which is embedded in the Favorites asset using a PNG format.
  - You can create a Bundle from just Project folder Prefabs: select more than one prefab in a project folder, right click, then TilePlus/Bundle Project Prefabs
  - You can add Project folder Prefabs to the Favorites List. Note that certain prefabs may do weird things or cause exceptions. 
    - For example, you can add a Palette prefab to the Favorites List and paint it. Surprisingly, this works.
    - Adding a prefab that has scripts which execute out of context may have issues.
    - You can use transform modification shortcuts like ALT+V, ALT+R/T/X/C/Z when previewing Prefabs. Note that this does not affect the prefab itself until after it is painted.
    - You can add Project folder Bundles to the Favorites list.
  - If you want to prevent a Palette from appearing in Painter's list of Palettes just change the layer on the Palette prefab asset to anything other than Default.
  - added new config item to adjust size of sprite when a palette's contents are being displayed in the RH column.
  - added new config item to adjust font size in lists. Note that you can make the display look awful as the resultant display is affected by the UI size and sprite size configuration settings.
  - added new config item to adjust relative size of upper and lower (minibutton) toolbars. Requires clicking refresh button to redraw the UI.
  - added new config item to allow sync of palettes between Painter and the Unity Palette window, when both are open. Additionally, when Painter is opened it will use the current- or last-selected Palette from the Unity Palette.
  - added new config item to allow reversal of sort order when tilemap sorting is enabled via the settings checkbox.
  - added new config item that controls whether or not Painter examines Tilemaps for changes (ie, new tilemap, deleted tilemap etc) during play mode. Default=true. 
    - These checks are only needed if you are actually adding or deleting tilemaps in Play mode.
    - Note that if this is OFF and you actually add or delete Tilemaps in Play mode then:
      - Deleted tilemaps when something on that map is being inspected in EDIT mode's right-hand column may cause nullref errors.
      - Similarly, if you are examining a Tile in the right-hand column but the map has actually been deleted then you can also get nullrefs.
    - The checkbox isn't shown unless Update-in-play is checked.
  - added new config item to allow aggressive detection of: paint mode with paint tool active, user changes sel from Tilemap to something else then returns to a tilemap: in that case, reactivate PainterTool and the Paint Tool (not the same thing).
    - If the Unity Tilemap Editor is open and has an active tool aggressive selection is automatically disabled.
  - added new bottom toolbar button that shows if tile or Prefab default transform mods are active. Capital P or T denotes active Prefab or Tile default mod active. LC letters mean not active.
    - Clicking on this button focuses the Transform Editor window.
    - The button color changes when either default preset is active.
  - MaxTilesForViewer config item default changed to 400. Acceptable values are 50-9999. This corresponds to "Max # Tiles to Display" in the Painter config panel.
  - when using the Shortcut (Default ALT+1) to drag a marquee, in addition to automatically painting the area within the marquee with the current tile when painting (if the PAINT tool is selected), if the ERASE tool is selected in PAINT or EDITING mode then drawing the marquee will delete all tiles within the selected area on ONLY the currently-selected Tilemap when the drag is completed by releasing the mouse button.
  - When displaying a TileFab or Bundle in the Paint mode inspector (Rightmost column), the asset creation date was added to the display.
  - In the Tilemaps list (left column) rather than a small "+" icon for Tilemaps with TilePlus tiles, such maps instead have a highlight color and are bolded.
  - In the Paint mode tiles list (right column) rather than a small "+" icon for TilePlus tiles, such tiles instead have a highlight color and are bolded.
  - Grid Selection mode:
    - The Create Chunk button is renamed to Create TileFab. 
    - A new "Create Bundle" button.
      - This is used when you make a Grid Selection in the Unity Tile Palette. See the "What's New" document for more info.
    - A new "Apply Mod" button.
      - When there's an active grid selection and an entry in the MODIFIERS panel AND that entry has any actions selected in the top dropdown then the APPLY MOD button is enabled. When clicked, applies the mod to the grid selection for the currently-selected Tilemap.
  - In Paint mode, the center column options has a new checkbox: Matches Only. This only appears if "Show TileFabs" is checked.
    - When this is checked, Tilefabs that don't match any existing scene Tilemap names or tags are filtered out.
  - In Paint mode, the center column options has a new checkbox: Bundle Tiles View. This only appears if "Show Tile Bundles" is checked.  
    - When this is checked, the tiles from a Bundle are displayed in a list as if they were a Palette and can be painted that way. 
    - When this is unchecked, the Bundle is painted as a single item as usual.
    - This is useful in several ways, but one use would be when you have a large palette but you want to have a subset of that palette. 
      - See 'NewInVersion3.pdf' for more information
    - Another use: if there are prefabs in the Bundle you can select it in the list and paint it, including use of ALT-V to modify the transform.
      - See 'NewInVersion3.pdf' for more information
  - In Paint mode, the center columns options has a new checkbox: Use Unity Palette. This only appears if "Show Palettes" is checked.
    - When this is checked, tiles in a Palette prefab Asset are displayed in the same Grid/Tilemap mini-scene as a Unity Tile Palette window.
      - A subset of the Palette window's functions are provided. You can paint single tiles or drag-select a group to paint.
      - If a multiple-selection is made in this fashion, the clipboard shows an eyedropper.
      - See NewInVersion3.pdf for more information.
  - In Paint mode when a Palette is displayed in a List and the number of tiles is greater than the "Max # Tiles to Display" in the config panel the border of the list is highlit yellow.
  - Support for "FANG" autotiles in Tile+Painter.
    - This uses two scriptable object class files: FangAutotileTpPlugin and FangLegacyAutotileTpPlugin. 
    - DRAG the folder from Plugins/TilePlus/Editor/Painter/TpPainterPlugins/Fang to the root assets folder, or any other folder outside of the Plugins root folder.
    - Open the two files within this folder in your IDE and uncomment the code. If you uncomment the scripts while they're within the Plugins folder you'll get errors. 
    - Then create project assets of the two scriptable objects. You can put them in the same folder as the scripts.
    - Note that newly-created TpPainterPlugins have the 'ignore this' toggle SET. Examine the assets and uncheck these toggles.
    - These auto tiles are from the "2D RPG topdown tilesets - pixelart assets MEGA BUNDLE" on the asset store [ https://assetstore.unity.com/packages/2d/environments/2d-rpg-topdown-tilesets-pixelart-assets-mega-bundle-212921 ].
     - You can see which plugins are loaded at the top of Painter's Settings panel. 
  - A new Tile: TpBundleTile. These are automatically created when you use the "Create Bundle" button described above. See the "What's New" document for more details.
  - Several of Painter's static class libraries converted to ScriptableSingletons: TpPainterScanners, TpPainterSceneView.

### Changed
- General: most of the code base was changed to nullable-enabled rather than using Jetbrains.Annotations.
- Tile+Painter and the Modifiers windows were added to the Window menu as well as the tools menu.
- TpLib: 
  - Breaking change: the OnTpLibChangedEvent no longer passes a class instance with the change information.
    - The DbChangedArgs class has been removed from the distribution.
    - The info in that class is passed to subscribers in the method call.
  - TpLib no longer prints version information when the "Informational" debug level is enabled. Use Tools/TilePlus/System Info.
  - The Runtime/Resources folder with a prefab called _TPP___TIMING_ has been removed. The GameObject is created programmatically in TpLib.PreloadTimingSubsystem.
  - TpLib ignores TpLog and TpLogWarning in builds unless TPT_DEBUG is defined in the Player scripting define symbols. 
- TpLibEditor: assembly version info deleted; it's always the same as that of TpLib.
- TpConditionalTasks (editor-only) 
  - default value for 'timeout frames' changed from int.MaxValue to 1E6
  - Callback change: used to be Action, now is Action<TpConditionalTasks.ContinuationResult>. The enum describes the return. 'Exec' value of the enum means callback should execute, others are early exit (which may mean that you want to execute your code), exception, etc.
  - frame count computed differently (and more accurately). Where used in TilePlus' Editor code, timeouts were adjusted as necc. Improved performance/speed/overhead. Irrelevant for user code.
- IMGUI Styles: font for checkbox title changed to non-bold, non-italic for more consistent appearance.
- Default colors for Tile+Painter scene gizmos changed to yellow from white for better distinction from the Palette.
- TilePlusPainter:
  - The Action buttons toolbar only shows available actions for each mode.
    - Paint mode remains the same: all actions. PAINT action only enabled when painting is possible (not a change)
    - Edit mode only has OFF, PICK, HELP, and SETTINGS
    - Grid Selection mode only has HELP and SETTINGS
  - The Marquee-drag shortcut has been changed to ALT+1 for Ergonomic reasons.
  - Much of the state maintained in TilePlusPainterWindow has been migrated to TpPainterState ScriptableSingleton.
  - API change: OnPainterModeChange event is now in TpPainterState instead of TilePlusPainterWindow.
  - Improvements in Edit mode detection of deleted tiles when RESET is used on a Tilemap component.
  - TpLib's OnTpLibChanged callback is used by Painter. Rather than handle these one-by-one they're now mostly cached and handled on the next Update and InspectorUpdate events.
  - Overwrite protection works differently: overwrite protection only works when the placed tile is a TilePlus tile. Normal Unity tiles don't have overwrite protection: they really don't need it.
  - The GridSelection panel would complain if > 64 GridSelections were present in the Scriptable Singleton used to archive these selections. Now it deletes the oldest one instead.
  - The GridSelection panel would pop up a message box if a new grid selection already existed. That doesn't happen anymore.
  - The Clipboard 'Variety' indicator shows a Tile icon rather than a scriptable object icon for standard Unity tiles.
  - Tilemaps list (left column): 
    - Hovering over an item shows the Sorting Layer name and Sorting Order in addition to the normal tooltip.
    - The number of different types of tiles is shown next to the Tilemap name except when the Tilemap is empty.
  - Settings pane:
    - The list of plugins has been deleted. You can see installed plugins in the System Info window. 
    - top section is now enclosed in a scrollview for better appearance as the number of Painter settings has increased.
      - Observant folks will note that this new scrollbar and the scrollbar in the bottom section of this pane are different: the top one is from UIElements and the bottom one is IMGUI.
  - Previews of any kind and most on-SceneView text **_are only drawn if the scene is in 2D mode._** 
- Tile+Painter Transform Editor window: 
  - Renamed to Painter Modifiers.
  - The top-right editor window menu has two functions: save and load. When loading, you'll get a message "ScriptableSingleton already exists. Did you query the singleton in a constructor?" which you can ignore.
  - Each item (a 'preset') in the list has a Color field. This is only used with Tiles (not TileBase-derived since those don't have a Color property).
  - If Painter has a modified single tile in the Clipboard, the "Add..." button changes to "Add from Clipboard". A click would copy the color and transform from the modified Clipboard Object.
    - This only works with single tiles but is a nice shortcut to copy transform/color changes from a tile that you modified during preview.
  - Each item also has an APPLY field. This is used to control what is affected when you use the particular preset. See 'NewInVersion3.pdf' for more information.
  - Each item now has a set of Radio buttons called "Defaults" with two buttons: Tiles and Prefabs.
    - If one of these is set, then ALL painted tiles or Prefabs use the Position, Rotation, and Scale for that particular Transform.
    - This turns out to be really convenient at times. Currently, this ONLY applies to single tile or Prefab painting. Painting groups of tiles in a selection and other multiple-tile operations aren't supported at this time.
    - The small icon next to the Clipboard image will change to the Transform icon when a tile or Prefab is selected from the list of objects in the right column.
    - On the Painter window, a new bottom toolbar button shows if tile or Prefab default transform mods are active. 
      - Capital P or T denotes active Prefab or Tile default mod active. Lower case letters mean not active. The button color changes when either default preset is active.
      - Clicking on this button focuses the Transform Editor window.
- Tools/TilePlus menu items have been reordered a bit and a new Clear Painter History menu item has been added.
- The Bundler will no longer accept the selection of a Tilemap as a source. You have to select a Grid that's the parent of the Tilemap that you wish to bundle.
  - The Bundler no longer provides the option to create variant prefabs. Prefabs are stored in the bundle as references to the source prefab.
  - When bundling prefabs from a scene the stored rotation and scale are the rotation and localScale of the root GameObject of the Prefab in the scene hierarchy.
  - When bundling prefabs from a project folder, the stored rotation and scale are the rotation and lossyScale of the root GameObject of the Prefab in the Project folder.
- Newer versions of DOTween automatically add DOTWEEN to the Project' scripting define symbols list. Toolkit and the Collision Demo have been upgraded to use either DOTWEEN or TPT_DOTWEEN.
- TilePlusBase: the ODIN-specific attr on m_State has been removed; the state will only be shown as a non-modifiable string. Unity 6 issue w/ ODIN and custom inspectors forced this change.

### Fixed
- Occasional non-fatal null-ref error fixed in TpPainterClasses file.
  - occasional nonfatal error message for null palettes. Edge case.
- TilePlusPainter: 
  - Very large palettes (several thousand tiles) could cause Painter to appear to hang with a Unity popup wait window.
    - The issue is that in Paint mode the Tilemap within the palette prefab is queried with Tilemap.GetUsedTilesCount. That method blocks execution and dirties/undirties all sorts of internal objects.
    - The fix is for the user to place the TpNoPaint component on the palette prefab; painter won't use GetUsedTilesCount on that Tilemap.
    - More of a workaround than a bug fix.
  - changes to Config panel UI Size and Font size values will update the various panels without having to click the Refresh button at the lower-right of the Painter panel.
  - Config panel UI Size, Font Size, and Palette sprite size fields changed to range sliders.
  - Fixed edge condition when collecting palettes: TpPainterContentWindow.ComputeTypeFilter.
  - Fixed logic error in TilePlusPainterWindow.cs in OnAddedAsTab event handler that prevented proper reconfig when the Painter window is added or removed as a child to another panel.
  - resetting the Painter configuration from the settings pane will properly save the config to mass storage and rebuilds the UI for consistency. 
  - fixed bug: the options buttons for Painting Source (in Paint mode) doesn't update the palettes list.
  - fixed bug: during the pick->paint transition of a Move (move tool) sometimes the source tilemap doesn't get the position erased if the move target is a different tilemap.
  - fixed issue in Painter lists when the name of a palette (including TileFabs, bundles, etc), a tilemap, or a Tile is long and field is narrow - visual overflow of name on top of the image on the same line.
  - When selecting a palette, the tiles list (right column) now scrolls the list to top. 
    - Solves issue when looking at a long list of tiles in a palette and then changing to one with a few: scroll position doesn't change and list appears empty until manually scrolled back to the area where there are list items.
- TilePlusBase:
  - Fix for Tilemap.RefreshAllTiles bug (see GetTileData).
    - Tilemap.RefreshAllTiles doesn't call Startup method when tile has an instanced GameObject. Reported to Unity, CASE IN-61288.
    - Can cause TPT tiles with an instanced GO to not register themselves in TPLib.
- TpAnimZoneBase: bug fix
  - If VisualizeArea is used and a different tile is inspected, the area is no longer visualized but the tile internally thinks that it is. This only affects this function and is only an issue in the Unity Editor, doesn't affect runtime. 
    - Fixed within TpAnimZoneBase.TpTrigBaseGui and also within TpLibEditor.OnSceneViewSceneGui. 
- TpLib: 
  - ResetOnEnterPlayMode() changed to avoid extra initializations.
  - Fixed issue where exiting play mode would not re-init callbacks properly.
    - Main affect was TilePlus tiles painted after exiting play mode would not clone properly.
    - Added test for callbacks correctly set up in TpLib.PreloadTimingSubsystem.
  - Improved performance of GetTilemapsInScene by ignoring Tilemaps with the TpNoPaint component attached.
  - Improved performance of OntilemapPositionsChanged and OntilemapTileChanged callbacks.
- TpLibEditor:
  - ResetOnEnterPlayMode() changed to avoid extra initializations.
  - OnEditorPlayModeStateChanged() clause case PlayModeStateChange.ExitingPlayMode: added call to TpLib.CallbacksControl()
    - Part of fix for "TilePlus tiles painted after exiting play mode would not clone properly" as mentioned above.
- TpPainterSceneView: AnyRestrictionsForThisTile would interpret locked tile as paintable. A bug.
- TpPrefabUtilities: if a bundle (separately or as part of a TileFab) is created with a GridSelection, and the Tilemap BoundsInt size is smaller than that of the GridSelection, then the BoundsInt size is increased to that of the GridSelection. 
  - Prior to this fix, if an area specified by a GridSelection had a Tilemap where the Tilemap's BoundsInt size was < GridSelection.size then any prefabs outside of the Tilemap's BoundsInt size wouldn't be archived. A bug. 
- TileFabLib.LoadBundle did not use the transform of a stored prefab to set the rotation, so even if the prefab had been rotated when archived it would be placed at rotation = Quaternion.Identity. A bug.

### Removed
- Since Painter has proper Prefab support, it no longer has the feature where you can paint a Tile's GameObject by holding down ALT.
- TpLib no longer pokes version info into the Scripting Define symbols of the Player.
- All of the window-opening shortcuts were removed. The only remaining shortcuts are for Tile+Painter: ALT+1 for marquee drawing and 1 for "Can't paint here" overriding.
- The Quick Open Palette menu item has been removed.
- The Configuration window items that set specific paths for editor commands have been removed.
- Some unused textures were removed from the TilePlus Extras common assets to reduce the package size.

 
### Known issues
- In Unity 6 the Unity Tile Palette UI-element's operation has changed.
  - Prior to this change the UI element component could operate independently of others
  - Now they don't so if you make a selection on Painter and then switch to the Unity palette the selection will appear on that window.
  - So they no longer can be independent.
  - Pre-Unity6 the palette does not properly disable itself in response to SetEnabled, so the palette is set to hide (ie it disappears).
- If a palette does not appear in the Painter's palette list, please check to see if the Palette prefab has the layer set to "DEFAULT". It's OK to change all objects when prompted to make the choice. 
  - Normal palettes created by the Unity Palette tool always have this set up properly, others may not.
- When using the new Palette display in Painter (viewing Palettes with the Use Unity Palette toggle on) the interface between Painter and the Palette element (this is a UI Toolkit element) can get funky. If this happens the Palette won't appear to be working.
  - Normally when you click on something in the palette a small red square at the upper left of the window will blink for 500 msec. 
  - If the Palette display doesn't appear to respond just click the refresh button at the lower left corner of the Painter window. 
- If you have unsaved scenes (created and present in the Hierarchy but never saved to mass storage) then the Tilemaps list in painter will not show the scene name properly since there isn't a scene name until it is saved. In this case "????" is shown to indicate an unsaved scene.
- When using the Painter's Grid Selection panel to bundle tiles selected from a Unity Palette window there will occasionally be an error related to writing meta files - date stamps are off by a tiny amount. Obscure race condition which at worst means you'd have to repeat the select-then-bundle process.
- In PAINT mode, asset previews for Prefabs may not appear immediately in lists. If that occurs scroll the list or re-select the source in the center column.
- When previewing TileFabs and Bundles, archived prefabs preview using proxy tiles and GameObject previews. This is a bit memory-intensive so the total number of prefab previews from a bundle is capped at 128. 
  - The previews are plain tiles with the prefab's "Preview" image. These tiles (of the type TpPrefabProxyTile) are pooled.
- Icons: if you create a multiple-selection from a Scene Tilemap and any of the tiles are rotated in the Tilemap then the generated Icon won't reflect that. May be fixed in the future, not an operational issue.
- This warning message: Ignoring textureless sprite named [NoName] TpImageLib:h128,w128, please import as a VectorImage instead. It's usually caused when you have a prefab as the first favorites item and has to do with asset previews, but is harmless. Mostly a UI Toolkit oddity.
 
### [2.0.1] 2023-09-01

### Known issues
- in Play mode, opening Tile+Painter or toggling Palettes ON in the Paint mode options panel in the center column when it had been off prior to entering Play mode may show this error: "The referenced script (Unknown) on this Behaviour is missing"
  - This isn't a Tile+Painter error, rather, one of the palettes has a missing asset reference; in other words, the original tile asset used in a Palette was deleted and when scanned to see how many tiles exist in the Palette, this 'deep within Unity' error occurs.
  - The fix is to examine all palettes by opening each Palette prefab so that you can see the embedded Tilemap, then open the info folder in the Tilemap component, and see if any of the tiles appear as 'TileBase'. 
  - If you find one, activate "Allow Prefab Editing" in the Configuration editor. Then select the Tilemap with your mouse and use Tools/TilePlus/Utilities/Delete Null Tiles. Then uncheck 'Allow Prefab Editing'.
- Tile+Painter won't show tiles in an opened Palette prefab's Tilemap. If you use the Tile+Brush to inspect a tile in an opened Palette prefab the TilePlusBase section of the Inspector will display "Could not Format: Tilemap was null".
  - This is because the Tiles in the Palette prefab aren't initialized properly, they don't seem to get the StartUp invocation.
  - It's not an error, it's a side effect (but not a feature).

### Added
- Documentation has a new file: Articles.pdf. This contains design and historical information about this project.

### Fixed

- TilePlusPainter.TpPainterContentPanel: fixed edge case where changing global mode to Paint mode didn't clear the PainterWindow.TileTarget.
  - Showed incorrect info in the brush inspector pane.
- TpPainterShortcuts: Copy to history would nullref if Painter window wasn't ever opened in a session. Fix = automatically open the window.
- TpSysInfo: added conditional compilation to avoid warning in 2023.x on change in content.verticalScrollerVisibility property.
- TpPainterSceneView: 
  - fixed tests for paintmask to only run in Paint mode when Painting tool is active. Minor: just removes spurious console messages.
  - changed the warnings in TpPainterSceneView.AnyRestrictionsForThisTile to only print to the console when the 'Informational' messages setting is active.
  - changed vertical offsets for some SceneView messages in TpPainterSceneView.HandleRepaint so that "Can't paint here" and "Tile sprite is hidden" appear correctly when multiple text messages are presented in the SceneView.

### Changed
- TpEditorUtilities in DeleteNulls, added check for 'Allow Prefab Editing' so that nulls can be removed from Palettes; see the Known Issues section.


### [2.0.0] 2023-5-08

### Added
- Tile+Painter: A UIElements-based tile/GameObject painter and tilemap viewer; replacing TilePlus Utility. See the pdf docs for more info.
  - Shortcut key is ALT+1. 
  - Uses the palettes you create with the Unity painter as well as TileFabs and Bundles.
  - Support is ONLY for single-tile-at-a-time painting/erasing/moving etc.
    - Exception: painting TileFabs and Bundles will of course paint numerous tiles at once.
  - Supports creating and saving GridSelections for better workflow.
  - Support for virtual grid as a multiple of Tilemap's Grid: useful to paint chunks of tiles from TileFabs.
- SystemInfo: The menu or ALT+2 displays a window with TPT statistics and useful information.
- Transform Editor: The menu or ALT+3 displays an editor window where you can create custom tile-sprite transforms when using Tile+Painter.
- Template Tool: The menu or ALT+4 displays an editor window that's used to create templates when using ZoneManagers.
- TpLib: add support for delayed callbacks using Async delays for those > 10 msec.
- TileUtil:  added tile transform convenience methods GetRotation, GetPosition, GetScale. Handy for Dotween 'getters'
- TileFabLib support for loading chunks of tiles at Runtime including adding/removing chunks around the camera as it moves.
  - Demo projects added to illustrate how to use this feature.
  - JSON archiving of active chunks for game saves.
- New Demos
 
### Changed
- Required Unity version changed to 2022.3 LTS or newer
- TilePlusBase:
  - Performance improvements in StartUp. Don't register tile if it already is registered (usually that's done in TpLib anyway but has a lot of extra checks that can be avoided if done in TilePlusBase)
  - new class for return from methods tagged with TptShowCustomGUI attribute.
  - NoActionRequiredCustomGuiReturn property can be used as a return value from methods tagged with TptShowCustomGui.
- TpLib:
  - new simplified forms of TpLog, TpLogWarning and TpLogError provided for convenience. Note: DO NOT USE within TilePlusBase or subclasses
    - Please note that this is for future compatibility.
  - support for delayed callbacks.
- Prefabs: Tilemap prefabs are simpler: only GameObjects are archived and the Tilemaps within the Prefab are loaded from TileFabs. 
  - Greatly increases efficiency especially if loaded more than once due to Bundle caching (see below).
- Animated tiles upgraded to use new Tile animation features and tile flags for animation.
- TileFabLib:
  - added capability to use Tilemap.LoadTiles with a TileChangeData array.
  - non-TilePlus tiles are cached when a Bundle is loaded for the first time: subsequent loads use the cache.
  - added filtering for tiles when a Bundle is loaded.

### Deprecated


### Removed
- Plugin system removed. TpTiming replaced with TpLib DelayedCall. TpDOTween replaced with DOTweenAdapter.
- TilePlus Utility window removed. Replaced with Tile+Painter.
- TpLog(LogType,string) deleted. Use instead TpLog, TpLogWarning, TpLogError.

### Fixed
- Initial release of 2.0 fixed innumerable small issues.

### Open Issues
- Rarely, when starting Unity with an existing scene that contains TilePlus tiles, these tiles may not be initialized properly. This is difficult to observe since it doesn't seem to be repeatable but it appears as if the Tilemap does not properly call StartUp in these cases.
  - This is an editor-only situation and if it occurs just use the Tools/Refresh TpLib menu item.  
- The History Stack in Painter has an inconsistent behaviour:
  - If you open and close the Painter window during a Unity session the stack will be cleared.
  - If you have the Painter window open during a Unity session and end the session with the window open then when you restart Unity the stack will be unchanged.
    - Since the stack can have clone tiles, in this situation the clone tile refs would be invalid (they are scene objects) so those are removed.
  - An obvious fix would be to clear the History stack when the Painter window is opened or Enabled but that would clear the stack on every scripting reload.
  - It's sort of a conundrum. Note: this is no longer an issue in V2.1 or later.
