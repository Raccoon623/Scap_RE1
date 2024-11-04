// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-17-2022
// ***********************************************************************
// <copyright file="TilePlusBrushEditor.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Editor for the TilePlus Brush</summary>
// ***********************************************************************


#nullable enable
using TilePlus.Editor.Painter;
using UnityEditor;
using static TilePlus.Editor.TpLibEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace TilePlus.Editor
{
    /// <summary>
    /// This editor is used for the TilePlusBrush. If you want to modify it to
    /// show specific info from tiles in the palette window or Selection Inspector,
    /// you don't have to!  Check out the Attributes such as
    /// [TptShowField]
    /// [TptShowEnum]
    /// [TptShowAsLabelSelectionInspector]
    /// [TptShowAsLabelBrushInspector]
    /// [Tooltip] (normal Unity attribute)
    /// etc
    /// </summary>
    [CustomEditor(typeof(TilePlusBrush))]
    public class TilePlusBrushEditor : GridBrushEditor
    {
        #region private
        private TilePlusBrush? BrushPlus => target as TilePlusBrush;
        private TileBase?      tile;
        private int            cellCount;
        private Vector3Int     position;
        
        
        #endregion
        

        #region TpPainterSelectionInspector
        /// <summary>
        /// Create the GUI for the GridSelection shown in an inspector when you pick a tile.
        /// </summary>
        public override void OnSelectionInspectorGUI()
        {
            var config = TilePlusConfig.instance;
            if (config == null)
            {
                EditorGUILayout.HelpBox("Can't get configuration.",MessageType.Error);
                return;
            }
            var tilemap = GridSelection.target.GetComponent<Tilemap>();
            if (tilemap == null)
            {
                EditorGUILayout.HelpBox("Null tilemap to Sel Inspector. ??",MessageType.Error);
                return;
            }

            tile = null;
            var selection                      = GridSelection.position;
            var autoSave                       = config.AutoSave;
            var isPlaying                      = Application.isPlaying;
            var cachedKeycode                  = KeyCode.None;
            var mapTransform                   = tilemap.transform;
            var parentTilemapLayer             = mapTransform.parent.gameObject.layer;

            if((tilemap.hideFlags & HideFlags.DontSave) == HideFlags.DontSave ||
               parentTilemapLayer                       == TilePlusBase.PaletteTilemapLayer)
            {
                EditorGUILayout.HelpBox("For safety, hiding inspector when target is Palette. If you really need to use the inspector on a palette, use a different brush.",MessageType.Warning);
                return;
            }

            var noEdit = false;
            (var noPaintLocked, (var allowPrefabEditing, _, var inPrefab, var inStage)) = NoPaint(tilemap);

            if(noPaintLocked || inPrefab)
            {
                noEdit = true;
                var msg = inStage
                              ? "Please don't modify this locked tilemap in a Prefab editing context."
                              : "This Tilemap is in a prefab. DO NOT edit if there are any TilePlus tiles!!!";
                EditorGUILayout.HelpBox(msg, MessageType.Warning);
                if(!config.ShowDefaultSelInspector)
                    return;
            }

            cellCount = selection.size.x * selection.size.y * selection.size.z;
            
            if (cellCount != 1 || (inPrefab && !allowPrefabEditing))  //no multiple selections
            {
                base.OnSelectionInspectorGUI();
                return;
            }

            position = selection.position;
            tile     = tilemap.GetTile(position);
            
            var tileIsTptTile = (tile as TilePlusBase) != null;

            //if tile isn't a TPT tile, or if showing default sel inp AND tilemap isn't locked, force showing the default sel insp
            if ((!tileIsTptTile || config.ShowDefaultSelInspector) && !TpLib.IsTilemapLocked(tilemap))
            {
                if (config.ShowDefaultSelInspector)
                {
                    //foldout for the default selection inspector
                    const string si     = "Default Selection Inspector";
                    var          result = false;
                    if (tileIsTptTile)
                    {
                        var openSelInsDef = config.OpenSelectionInspectorDefault;
                        result = EditorGUILayout.BeginFoldoutHeaderGroup(openSelInsDef, si);
                        if (result != openSelInsDef)
                            config.OpenSelectionInspectorDefault = result;
                    }

                    if (result || !tileIsTptTile)
                    {
                        //get config for allowing BKSP/DEL intercept for default Sel Insp.
                        //note that the config file default is false, so BKSP and DEL are inhibited
                        //for the default inspector.
                        var allowBkspDel = config.AllowBackspaceDelInSelectionInspector;

                       
                        if (!allowBkspDel)
                        {
                            //if not allowing delete, consume the keystrokes
                            if (Event.current.type == EventType.KeyDown &&
                                Event.current.keyCode is KeyCode.Delete or KeyCode.Backspace)
                            {
                                cachedKeycode         = Event.current.keyCode;
                                Event.current.keyCode = KeyCode.None;
                            }
                        }

                        base.OnSelectionInspectorGUI();
                        if (cachedKeycode != KeyCode.None) //only the case if we intercept keystrokes above
                            Event.current.keyCode = cachedKeycode;
                    }

                    if (tileIsTptTile)
                        EditorGUILayout.EndFoldoutHeaderGroup();
                }
            }
            //now inspect Tileplus tiles
            SelectionInspectorGui.Gui(tilemap, tile, position, autoSave, isPlaying, noEdit);
            GUI.skin = null;
        }
        #endregion

       
        #region PaintInspector
        private          float   previewSize = 1f;
        private readonly Vector3 textOffset  = new Vector3(0.25f, 0);


        /// <inheritdoc />
        public override void OnPaintSceneGUI(GridLayout gridLayout, GameObject brushTarget, BoundsInt theposition, GridBrushBase.Tool tool, bool executing)
        {
            base.OnPaintSceneGUI(gridLayout, brushTarget, theposition, tool, executing);
            var config = TilePlusConfig.instance;
            if (config == null)
            {
                EditorGUILayout.HelpBox("Can't get configuration.", MessageType.Error);
                return;
            }
            
            var isPaintTool    = tool is GridBrushBase.Tool.Paint;
            var tgt            = brushTarget.GetComponent<Tilemap>();
            var showPos        = tgt != null && !TpLib.IsTilemapFromPalette(tgt);
            var brushplus      = BrushPlus;
            var brushTargetPos = brushTarget.transform.position;
            
            if (config.ShowBrushPosition && showPos)
            {
                var positionTextGuiStyle = new GUIStyle
                                           {
                                               normal    = { textColor = config.BrushPositionTextColor },
                                               fontStyle = FontStyle.Bold,
                                               fontSize  = config.BrushPositionFontSize
                                           };
                var pos      = gridLayout.WorldToCell(theposition.center + brushTargetPos);
                var theSize  = isPaintTool ? brush.size :  theposition.size;
                var showSize = theSize.x > 1 || theSize.y > 1;
                
                var text = showSize ? $"[{pos.x}:{pos.y}]\n[{theSize.x}*{theSize.y}]" : $" [{pos.x}:{pos.y}]";
                
                Handles.Label((theposition.max + textOffset) + brushTargetPos, text , positionTextGuiStyle);
            }

            if (!isPaintTool ||  (brushplus != null && ( !brushplus.NoOverwriteFromPalette || !brushplus.AllowOverwriteOrIgnoreMap)))
                return;
           
            var center =new BoundsInt(theposition.min - brush.pivot, brush.size).center;
            center += brushTargetPos;
            var len    = new Vector2(brush.size.x, brush.size.y) / 2f;
            var end    = new Vector2(center.x - len.x, center.y - len.y);
            var start  = new Vector2(center.x + len.x, center.y + len.y);
            TpLibEditor.TilemapLine(start, end, Color.black, 0);
        }


        /// <summary>
        /// Create the GUI for the inspector shown at the bottom of the palette window
        /// when this brush is used.
        /// Custom data in a tile can be displayed by using attributes 
        /// </summary>
        public override void OnPaintInspectorGUI()
        {
            const string topBarText = "Use TilePlus assets to paint modifiable tiles."+
                                      "\nUse Toolbar buttons to Inspect the asset, focus Project window, or edit the tile script." +
                                      "\nNo Overwrites prevents mouse/kbd bounce from overwriting positions. Advised to leave on!" +
                                      "\nThis can be overridden with a hot key: the default is the number key '1'";
            
            const string hs          = "Help";
            
            var config = TilePlusConfig.instance;
            if (config == null)
            {
                EditorGUILayout.HelpBox("Can't get configuration.",MessageType.Error);
                return;
            }
            GUI.skin = TpIconLib.TpSkin;
            var buttonSize = config.BrushInspectorButtonSize;
            var brushplus  = BrushPlus;

            if (brushplus != null)
            {
                //foldout for the help box
                brushplus.m_ShowHelpBar = EditorGUILayout.BeginFoldoutHeaderGroup(brushplus.m_ShowHelpBar, hs); //GUILayout.Toggle(hideTopBar, hs);
                if (brushplus.m_ShowHelpBar)
                    EditorGUILayout.HelpBox(topBarText, MessageType.None);
                EditorGUILayout.EndFoldoutHeaderGroup();
            }

            //palette tilemap multiple selection is a no-no for special treatment
            if (brush.cellCount > 1)
            {
                // ReSharper disable once ConvertToUsingDeclaration
                using (var h = new EditorGUILayout.VerticalScope("Box"))
                {
                    GUI.Box(h.rect, GUIContent.none);
                    var count = brush.cellCount.ToString();
                    EditorGUILayout.HelpBox($"--multiple selection of size {count}. Preview unavailable.",MessageType.Info);
                   
                }
            }
            else if (brush.cellCount != 0 && brush.cells[0].tile != null)
            {
                // ReSharper disable once Unity.NoNullPatternMatching
                if (brush.cells[0].tile is ITilePlus iTilePlusInstance)
                {
                    var       asTpt      = (TilePlusBase)iTilePlusInstance;
                    
                    const int truncateAt = 40;
                    EditorGUILayout.BeginHorizontal();
                    using (new EditorGUILayout.VerticalScope("Box"))
                    {
                        var desc          = iTilePlusInstance.Description;
                        if (desc == string.Empty)
                            desc = "Description: missing";
                        else if (desc.Length > truncateAt)
                            desc = desc.Substring(0, truncateAt - 1);
                        var customInfo = string.IsNullOrEmpty(iTilePlusInstance.CustomTileInfo) ? string.Empty : $"\nCustomInfo: {iTilePlusInstance.CustomTileInfo}";
                        var displayString = $"Name: {iTilePlusInstance.TileName}\nDescription:{desc}\nClearmode: {iTilePlusInstance.TileSpriteClear.ToString()}, ColliderMode: {iTilePlusInstance.TileColliderMode.ToString()} {customInfo}";
                        var guiContent = new GUIContent(displayString) { tooltip = "Basic info about the tile selected in the palette. First line: Name (Type) [State]" };
                        EditorGUILayout.HelpBox(guiContent);

                        if (brushplus != null &&  brushplus.m_ShowAssetInfo && iTilePlusInstance is TilePlusBase tpb)
                        {
                            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                            {
                                using (new EditorGUIUtility.IconSizeScope(new Vector2(buttonSize, buttonSize)))
                                {
                                    if (GUILayout.Button(Content.S_OpenInInspectorButtonGUIContent))
                                        TpEditorUtilities.OpenInspectorDelayed(tpb, null);
                                    if (GUILayout.Button(Content.S_BrInsFocusGUIContent))
                                        TpEditorUtilities.FocusProjectWindowDelayed(tpb);
                                    if (GUILayout.Button(Content.S_BrInsOpenInEditorGUIContent))
                                        TpEditorUtilities.OpenInIdeDelayed(tpb);
                                }
                            }
                        }

                        //show the custom content
                        using (new EditorGUILayout.VerticalScope("Box"))
                        {
                            var obj             = (Object)asTpt; 
                            var completionState = ImGuiTileEditor.GuiForTilePlus(obj, TppInspectorSpec.Brush, iTilePlusInstance.ParentScene,false);
                            GUI.skin = TpIconLib.TpSkin;
                            if (!completionState.m_FoundTaggedTile)
                                EditorGUILayout.LabelField("no custom data");
                        }
                    }

                    //if this is a tile with a gameObject to instantiate, try to show a preview
                    if(asTpt.gameObject != null)
                    {
                        var prefab      = asTpt.gameObject;
                        var infoHeader  = "Custom preview from TilePlus asset.";
                        var infoContent = string.Empty;
                        var preview     = iTilePlusInstance.PreviewIcon;
                        if (preview == null) //if no overriding PreviewIcon is found in the tileplus asset
                        {
                            var examFound      = false;
                            var subObjRenderer = prefab.GetComponentInChildren<Renderer>(); //find a renderer in the prefab.
                            // ReSharper disable once Unity.NoNullPatternMatching
                            if (subObjRenderer && subObjRenderer is not SpriteRenderer)
                            {
                                examFound   = true;
                                preview     = AssetPreview.GetAssetPreview(subObjRenderer.gameObject);
                                infoHeader  = "Preview from subObject:";
                                infoContent = subObjRenderer.ToString();
                            }
                            else
                            {
                                var spriteRenderer = prefab.GetComponentInChildren<SpriteRenderer>();
                                if (spriteRenderer)
                                {
                                    examFound = true;
                                    var sprite = spriteRenderer.sprite;
                                    preview     = AssetPreview.GetAssetPreview(sprite);
                                    infoHeader  = "Preview from sprite:";
                                    infoContent = sprite.ToString();
                                }
                            }

                            if (!examFound)
                            {
                                infoHeader  = "Preview from TilePlus asset:";
                                infoContent = prefab.ToString();
                                preview     = AssetPreview.GetAssetPreview(prefab);
                            }
                        }

                        // ReSharper disable once ConvertToUsingDeclaration
                        using (var h = new EditorGUILayout.VerticalScope("Box"))
                        {
                            GUI.Box(h.rect, GUIContent.none);

                            if (preview == null)
                            {
                                EditorGUILayout.LabelField($"no preview available for {prefab.name}");
                            }
                            else
                            {
                                EditorGUILayout.BeginVertical();
                                EditorGUILayout.LabelField(infoHeader,  GUILayout.ExpandWidth(true));
                                EditorGUILayout.LabelField(infoContent, GUILayout.ExpandWidth(true));
                                EditorGUILayout.EndVertical();
                                EditorGUILayout.BeginVertical();
                                GUILayout.Box(preview, GUILayout.Height(50 * previewSize), GUILayout.MaxWidth(50 * previewSize));
                                EditorGUILayout.Space(8f);
                                previewSize = GUILayout.HorizontalSlider(previewSize, 0.5f, 2f, GUILayout.Width(100));
                                EditorGUILayout.Space(18f);
                                EditorGUILayout.EndVertical();
                            }
                        }
                    }
                    EditorGUILayout.EndHorizontal();
                    //end of top pane
                }
                else
                {
                    var tBase = brush.cells[0].tile;
                    var typ   = tBase.GetType();
                    using (new EditorGUILayout.VerticalScope("Box"))
                    {
                        EditorGUILayout.HelpBox($"Normal tile asset ('{tBase.name}') of type ({typ}) : no special features", MessageType.Info, true);
                        EditorGUILayout.Space(2f);

                        if (brushplus != null)
                        {
                            if (brushplus.m_ShowAssetInfo)
                            {
                                using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandWidth(false)))
                                {
                                    using (new EditorGUIUtility.IconSizeScope(new Vector2(buttonSize, buttonSize)))
                                    {
                                        if (GUILayout.Button(Content.S_OpenInInspectorButtonGUIContent))
                                            TpEditorUtilities.OpenInspectorDelayed(tBase, null);
                                        if (GUILayout.Button(Content.S_BrInsFocusGUIContent))
                                            TpEditorUtilities.FocusProjectWindowDelayed(tBase);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            { 
                using (new EditorGUILayout.VerticalScope("Box"))
                {
                    EditorGUILayout.HelpBox("Nothing to paint!", MessageType.Info, true);
                    EditorGUILayout.Space(2f);
                }
            }
            
            EditorGUILayout.Separator();
            //start of bottom pane with function toggles
            BrushPlus!.m_ShowBrushToggles = EditorGUILayout.BeginFoldoutHeaderGroup(BrushPlus.m_ShowBrushToggles, "Brush Toggles"); 
            if (BrushPlus.m_ShowBrushToggles)
            {
                // ReSharper disable once ConvertToUsingDeclaration
                using (var h = new EditorGUILayout.VerticalScope("Box",GUILayout.ExpandWidth(true)))
                {
                    GUI.Box(h.rect, GUIContent.none);
                    //var len = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = 200;
                    BrushPlus.m_FloodFillPreview = EditorGUILayout.Toggle("Show Flood-fill preview",
                        BrushPlus.m_FloodFillPreview,
                        GUILayout.ExpandWidth(true));
                    
                    var currentValue = config.NoOverwriteFromPalette;
                    var newValue = EditorGUILayout.Toggle(new GUIContent("No Overwrites from Palette", "Don't allow tile overwrites from Palette"),
                                                              currentValue,
                                                              GUILayout.ExpandWidth(true));
                    if (newValue != currentValue)
                        config.NoOverwriteFromPalette = newValue;
                    
                    BrushPlus.m_ShowAssetInfo = EditorGUILayout.Toggle("Show asset information Toolbar",
                        BrushPlus.m_ShowAssetInfo,
                        GUILayout.ExpandWidth(true));
                }
            }
            EditorGUILayout.EndFoldoutHeaderGroup();
            
            EditorGUILayout.Separator();
            GUI.skin = null;
        }

        
        #endregion

        
        
        #region GuiContent
        private static class Content
        {
            public static readonly GUIContent S_BrInsFocusGUIContent            = new(TpIconLib.FindIcon(TpIconType.UnityOrbitIcon),"Focus Project Window on Tile asset");
            public static readonly GUIContent S_BrInsOpenInEditorGUIContent     = new(TpIconLib.FindIcon(TpIconType.UnityTextAssetIcon),  "Open TilePlus script in Editor");
            public static readonly GUIContent S_OpenInInspectorButtonGUIContent = new(TpIconLib.FindIcon(TpIconType.InfoIcon),"Open in Inspector");
        }

        
        #endregion
    }
    
    
    
}
