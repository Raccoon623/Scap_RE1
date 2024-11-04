// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 04-24-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 04-21-2023
// ***********************************************************************
// <copyright file="TpPainterGridSelPanel.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

// ReSharper disable MemberCanBeMadeStatic.Local

#nullable enable
namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// information about what is selected in the list.
    /// </summary>
    [Serializable]
    public class SelectionElement
    {
        /// <summary>
        /// The Selection Bounds
        /// </summary>
        [SerializeField]
        public BoundsInt m_BoundsInt = new(new Vector3Int(0,0,0), new Vector3Int(1,1,1)); //size will be 1,1,1 if default ctor used

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="boundsInt">the selection bounds</param>
        public SelectionElement(BoundsInt boundsInt)
        {
            m_BoundsInt = boundsInt;
            var size = m_BoundsInt.size;
            if(size.z <= 0)
                size.z           = 1; 
            m_BoundsInt.size = size;
        }

        /// <summary>
        /// Base Ctor
        /// </summary>
        public SelectionElement()
        {
        }

    }
    
    
    
    /// <summary>
    /// Visual Element for the Grid Selection panel 
    /// </summary>
    public class TpPainterGridSelPanel : VisualElement
    {
        /// <summary>
        /// name used when saving a bundle from palette.
        /// </summary>
        public const string PaletteSaveName = "PALETTE";
        
        #region privateFields
        //UiElements refs
        private readonly TpListView listView;
        private readonly Button     overlayButton;
        private readonly Button     gridSelButton;
        private readonly Button     mapClearButton;
        private readonly Button     makeFabButton, makeBundleButton;
        private readonly Button     clearButton;
        private readonly Button     deselectButton;
        private readonly Button     listViewDeleteButton;
        private readonly Button     applyTransformButton;
        private readonly Label      mapStatusLabel;

        private List<SelectionElement>   selectionElements = new();
        private bool                     selecting;

         
        private readonly string basicHelpText = "Create and manage Grid Selections and Marquees. Click the button for more information";
        
        #endregion
        
        #region privateProperties
       
        private string ExpandedHelpText
        {
            get
            {
                var binding = TpPainterShortCuts.MarqueeDragTooltip;  //ShortcutManager.instance.GetShortcutBinding("TilePlus/Painter/MarqueeDrag [C]");
                return $"GridSelection management. When this panel is visible, select a Tilemap, then add a GridSelection with the Palette or hold down the shortcut key ({binding}) while dragging.\n\n"
                       + "To use, select a BoundsInt below, then click OVERLAY to just show the bounds, or GRID SELECTION to also create a new Grid Selection.\n"
                       + "\nIt's active until DESELECT is clicked, the Painter isn't the active Tool, or something else creates a Grid Selection."
                       + "\n\nIf you create a Grid Selection using the GRID SELECTION button or if there's an active one (say, created using the Palette) then the\n"
                       + "Clear Map, Create TileFab, Create Bundle, and Apply Mod buttons will be active. Clear map uses the active Grid Selection to invoke the Clear Selected Tilemaps menu function.\n"
                       + "Create TileFab emulates the Bundle Tilemaps menu function.\n" 
                       + "Create Bundle bundles just one Tilemap. \n"
                       + "Apply Mod applies the selected Painter Modifiers mod, or the zeroth (first) mod when none are selected. \n\n"
                       + "This tool works best when the Tilemap has an origin of (0,0,0) or integer offsets from zero such as (1,1,0)."
                       + "\n<color=red><b>Click the button to close this field.</b></color>";
            }
        }

        #endregion
        

        #region publicFields
        /// <summary>
        /// The active grid selection. 
        /// </summary>
        internal SelectionElement? m_ActiveGridSelection;
        #endregion
        
        #region Ctor
        #pragma warning disable CS8618
        /// <summary>
        /// Ctor for panel
        /// </summary>
        public TpPainterGridSelPanel()
            #pragma warning restore CS8618
        {
            m_ActiveGridSelection = null;
           
            selectionElements.Clear();
            foreach(var item in TpPainterGridSelections.instance.m_GridSelectionWrappers)
                selectionElements.Add(new SelectionElement(item.m_BoundsInt));
                
            style.flexGrow   = 1;
            style.flexShrink = 1;
            
            style.borderLeftWidth   = 4;
            style.borderRightWidth  = 4;
            style.borderBottomWidth = 2;
            style.borderTopWidth    = 2;
            style.borderBottomColor = Color.black;
            style.borderLeftColor = Color.black;
            style.borderRightColor = Color.black;
            style.borderTopColor = Color.black;
            
            style.paddingBottom           = 20;
            style.paddingLeft             = 2;
            style.paddingTop              = 2;
            style.paddingRight            = 2;


            Add(new TpHelpContainer(basicHelpText, ExpandedHelpText));
            Add(new TpSpacer(8, 20));
            
            var container = new VisualElement() {name ="outer-container",  style = { flexGrow = 1} };
            Add(container);
            
            container.Add(listView = new TpListView(selectionElements,
                                                   32,
                                                   false,
                                                   MakeItem,
                                                   BindItem));
            //listView.reorderable         = true;
            listView.showAddRemoveFooter = true;
            listView.selectionType       = SelectionType.Single;
            //this next line helps the content container grow/shrink when the HelpContainer is opened/closed.
            listView.Q<VisualElement>("unity-content-container").style.flexGrow = 1;

            
            var scrollView = this.Q<ScrollView>(null, "unity-scroll-view");
            if(scrollView != null)
                scrollView.style.borderBottomWidth = 2;
            
            //next 2 lines handy for debug only
            /*listView.showFoldoutHeader             = true; 
            listView.showBoundCollectionSize       = true;*/
            
            listView.itemsRemoved     += ListViewOnitemsRemoved;
            listView.itemIndexChanged += ListViewOnitemIndexChanged;
            
            listView.Q<Button>("unity-list-view__add-button").visible =  false;
            listViewDeleteButton = listView.Q<Button>("unity-list-view__remove-button");
           
            container.Add(new TpSpacer(20,10));

            var buttonContainerContainer = new VisualElement() {name="button-container-container", style = 
                                                                   {
                                                                       minHeight      = 20,
                                                                       flexGrow       = 1,
                                                                       paddingBottom  = 10,
                                                                       justifyContent = Justify.FlexEnd
                                                                   }
                                                               };
            mapStatusLabel = new Label(){name = "map-status-label"};
            buttonContainerContainer.Add(mapStatusLabel);
            container.Add(buttonContainerContainer);
            var buttonContainer = new VisualElement() {name = "button-container",  style =
                                                      {
                                                          justifyContent = Justify.FlexStart,
                                                          minHeight = 20,
                                                          flexGrow = 0, 
                                                          flexDirection = FlexDirection.Row,
                                                          flexWrap = new StyleEnum<Wrap>(Wrap.Wrap)
                                                          
                                                      } };
            buttonContainerContainer.Add(buttonContainer);
            clearButton= new Button(() =>
                            {
                                TpLib.DelayedCallback(TilePlusPainterWindow.RawInstance, () =>
                                {
                                    if (!EditorUtility.DisplayDialog("Confirm", "Please confirm clearing this list", "YEP", "NOPE!"))
                                        return;
                                    Deselect();
                                    TpPainterGridSelections.instance.m_GridSelectionWrappers.Clear();
                                    TpPainterGridSelections.instance.SaveData();
                                    selectionElements.Clear();
                                    listView.Rebuild();

                                },"T+P:GridSel-Clear");
                            }){style = {flexGrow = 0}, text = "Clear", tooltip = "Delete all the items above. Confirmation required."};
            buttonContainer.Add(clearButton);
            deselectButton = new Button(Deselect){style = {flexGrow = 0}, text = "Deselect", tooltip = "Clear the overlay or Grid Selection"};
            buttonContainer.Add(deselectButton);
            
            overlayButton        = new Button(Overlay){ text     = "Overlay", style        = { flexGrow = 0, }, tooltip = "Show as an overlay, do not create Grid Selection."};
            gridSelButton        = new Button(GridSel){ text     = "Grid Selection", style = { flexGrow = 0, }, tooltip =  "Show as an overlay and create a Grid Selection."};
            mapClearButton       = new Button(MapClear) { text   = "Clear map", style      = { flexGrow = 0, }, tooltip = "Erase an area on the selected Tilemap as defined by the active Grid Selection." };
            makeFabButton        = new Button(MakeBundle) { text  = "Create TileFab", style = { flexGrow = 0, } ,tooltip = "Create a TileFab from the area defined by the active Grid Selection."};
            makeBundleButton     = new Button(MakeBundleOneMap) { text = "Create Bundle", style  = { flexGrow = 0, } ,tooltip = "Create a Tile Bundle from the area defined by the active Grid Selection on the current Tilemap."};
            applyTransformButton = new Button(ApplyMod) {text    = "Apply Mod", style      = { flexGrow = 0, } ,tooltip = "Apply custom modification from the selected Painter Modifier to the area defined by the active Grid Selection. Doesn't affect Prefabs. This can take some time to execute for larger selections!!"}; 
            
            buttonContainer.Add(overlayButton);
            buttonContainer.Add(gridSelButton);
            buttonContainer.Add(mapClearButton);
            buttonContainer.Add(makeFabButton);
            buttonContainer.Add(makeBundleButton);
            buttonContainer.Add(applyTransformButton);
        }

        #endregion
        
        #region Updater
        
        private IVisualElementScheduledItem? schItem;
        /// <summary>
        /// Enable/Disable the internal scheduled updates.
        /// </summary>
        /// <param name="enable"></param>
        internal void EnableScheduledUpdate(bool enable)
        {
            if (enable)
            {
                if (schItem == null)
                {
                    schItem = schedule.Execute(UpdateEvent);
                    schItem.Every(500);
                }
                schItem.Resume();
            }
            else
                schItem?.Pause();
        }
        
        
        private void UpdateEvent()
        {
            if (!TpPainterState.InGridSelMode)
                return;
            var selected      = listView.selectedItem;
            var selection     = selected != null;
            var selectedIndex = selection ? listView.selectedIndex : -1;
            var cContainer = listView.Q<VisualElement>("unity-content-container");
            var numItems   = 0;
            if (cContainer != null)
                numItems = cContainer.childCount;

            for(var i = 0; i < numItems; i++)
            {
                if (listView.GetRootElementForIndex(i) is TpListBoxItem item)
                {
                    item.SetColor(i == selectedIndex
                                      ? Color.white
                                      : Color.black);
                }

            }

            listViewDeleteButton.SetEnabled(listView.itemsSource.Count != 0); 
            
            var paintTarget       = TpPainterState.PaintableMap;
            var showActionButtons = paintTarget is { Valid: true } && selection;
            overlayButton.SetEnabled(showActionButtons);
            gridSelButton.SetEnabled(showActionButtons);

            var validGridSel = TpPainterGridSelectionUtils.ValidGridSelection;
            var isPalette    = validGridSel.map != null && TpLib.IsTilemapFromPalette(validGridSel.map);

            mapClearButton.SetEnabled(validGridSel.validTarget && !isPalette);
            makeFabButton.SetEnabled(validGridSel.validTarget && !isPalette);
            makeBundleButton.SetEnabled(validGridSel.validTarget && !isPalette);
            clearButton.SetEnabled(listView.itemsSource.Count != 0);
            deselectButton.SetEnabled(m_ActiveGridSelection != null);

            var modsActive = TpPainterModifiers.instance != null && TpPainterModifiers.instance.CurrentModWrapper != null &&
                             TpPainterModifiers.instance.CurrentModWrapper.AnyEnabledActions;

            var modStatus = modsActive
                                ? $"Active Mod:{TpPainterModifiers.instance!.CurrentModWrapper!.m_ModName}"
                                : string.Empty;
            mapStatusLabel.text = GridSelection.active
                                      ? $"Target Tilemap: {(TpPainterState.PaintableMap != null ? TpPainterState.PaintableMap.Name : "None")}\n{modStatus}"
                                      : string.Empty;
            
            applyTransformButton.SetEnabled(modsActive && (validGridSel.validTarget 
                                                           && (GridSelection.position.size.x > 1 || GridSelection.position.size.y > 1)));
        }
        
        #endregion
        
        #region handlers
        
        private void MapClear()
        {
            var paintTarget = TpPainterState.PaintableMap;
            if(paintTarget != null && paintTarget.Valid && paintTarget.ParentGridTransform != null )
                TpEditorUtilities.ClearSelectedTilemaps(GridSelection.target/*paintTarget.TargetTilemap!.gameObject */,GridSelection.position, true);
        }
        private void MakeBundle()
        {
            TpPrefabUtilities.MakeBundle();
        }
        
        private void GridSel()
        {
            var paintTarget = TpPainterState.PaintableMap;
            if(paintTarget is not { Valid: true })
                return;

            if(listView.selectedItem is not SelectionElement item)
                return;
            
            //don't want to add another GridSelection frivolously.
            //Actually this creates other issues, eg click Overlay then this code fails... 
            /*if(m_ActiveGridSelection !=null && m_ActiveGridSelection.m_BoundsInt  == item.m_BoundsInt) 
                return;*/
            Tilemap? map;
            if ((map = paintTarget.TargetTilemap) != null)
            {
                Selecting = true;
                var bounds = item.m_BoundsInt;
                var gridLayout  = map.layoutGrid;
                bounds.position += gridLayout.LocalToCell(map.transform.localPosition);
                GridSelection.Select(map.gameObject, bounds);
                TilePlusPainterWindow.RawInstance!.ForcePainterTool(true);
                Selecting = false;
            }

             
            m_ActiveGridSelection = item;

        }

        private void Overlay()
        {
            var paintTarget = TpPainterState.PaintableMap;
            if(paintTarget is not { Valid: true })
                return;

            if(listView.selectedItem is not SelectionElement item)
                return;
             
            m_ActiveGridSelection = item;
            
        }

        async void MakeBundleOneMap()
        {
            await TpPainterGridSelectionUtils.BundleOneMap();
        }
        
        private void ApplyMod()
        {
            var paintTarget = TpPainterState.PaintableMap;
            if (paintTarget == null)
                return;
            Tilemap? map;
            if((map = paintTarget.TargetTilemap) == null)
                return;

            var mod = TpPainterModifiers.instance.CurrentModWrapper;
            if(mod == null)
                return;
            if(!mod.AnyEnabledActions)
                return;
            var color      = mod.m_Color;
            var trans      = mod.m_Matrix;
            var applyColor = mod.AffectsColor;
            var applyTrans = mod.AffectsTransform;

            var tiles = map.GetTilesBlock(GridSelection.position);
            for (var i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i];
                if (tile == null )
                    continue;
                // ReSharper disable once Unity.NoNullPatternMatching
                if(tile is not Tile t)
                    continue;
                if ((!applyColor || (t.flags & TileFlags.LockColor) == 0) && (!applyTrans || (t.flags & TileFlags.LockTransform) == 0))
                    continue;
                if (TpLibEditor.Informational)
                    TpLib.TpLog(InappropriateTile);
                TpLib.DelayedCallback(null, () => { EditorUtility.DisplayDialog("!!Tile Flags!!", InappropriateTile, "Continue"); }, "T+P:SV:Inapprop flags for modded clipbd.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(new Object[] { map, map.gameObject }, "Tile+Painter[GameObject]: GridSelPanel mod");
            
            TpLib.InhibitOnTpLibChanged = true; //avoid a zillion OnTpLibChanged events.
            foreach (var pos in GridSelection.position.allPositionsWithin)
            {
                if (applyColor)
                    map.SetColor(pos, color);

                if (applyTrans)
                    map.SetTransformMatrix(pos, trans);
            }    
            TpLib.InhibitOnTpLibChanged = false;
            //send ONE OnTpLibChanged event.
            TpLib.ForceOnTpLibChanged(TpLibChangeType.Modified, true, Vector3Int.zero, null);
        }

        
        private const string InappropriateTile = "Grid Selection has tiles that can't be modified.\n" 
                                                 + "At least one tile has the LockColor and/or LockTransform flags set but modifications to the transform and/or color were made. " 
                                                 + "This is incompatible.\nPlease see the FAQ: ‘I Can’t Paint Modified Tiles’ in the Painter User Guide.\n";
                                                 

        #endregion
       
        #region control
        internal void Deselect()
        {
            //mod - why do the below? Removed 7 Mar 24 
            /*if(selectionElements.Count != 0)
                listView.SetSelectionWithoutNotify(new []{-1});*/
            m_ActiveGridSelection = null;
            Selecting = true; //interlock to prevent false messages like "duplicate selection"
            GridSelection.Clear();
            Selecting = false;
        }

        private void ListViewOnitemIndexChanged(int idBeingMoved, int destinationId)
        {
            TpPainterGridSelections.instance!.m_GridSelectionWrappers = selectionElements
                                                      .Select(se => new GridSelectionWrapper(se.m_BoundsInt))
                                                      .ToList();
            TpPainterGridSelections.instance.SaveData();
            listView.Rebuild();
        }

       

        private void ListViewOnitemsRemoved(IEnumerable<int> objs)
        {
            selectionElements                       = (List<SelectionElement>)listView.itemsSource;
            TpPainterGridSelections.instance!.m_GridSelectionWrappers = selectionElements.Select((se) => new GridSelectionWrapper(se.m_BoundsInt)).ToList();
            TpPainterGridSelections.instance.SaveData();
        }

        private bool Selecting { get; set; }
        

        internal void AddGridSelection(BoundsInt boundsInt, bool silent = false)
        {
            if (Selecting)
            {
                Selecting = false;
                return;
            }
            //ignore one-spot grid selections.
            if (boundsInt.size is { x: 1, y: 1 })
                return;

            if (TpPainterGridSelections.instance!.m_GridSelectionWrappers.Count >= 64)
                TpPainterGridSelections.instance.m_GridSelectionWrappers.RemoveAt(0);

            if (boundsInt.size == Vector3Int.zero || boundsInt.size == new Vector3Int(0,0,1))
            {
                if(!silent)
                    TilePlusPainterWindow.RawInstance!.ShowNotification(new GUIContent("BadGridSelection! Grid Selection size was zero."));
                return;
            }

            //no dupes
            if (TpPainterGridSelections.instance.m_GridSelectionWrappers.Any(gs => gs.m_BoundsInt == boundsInt))
                return;
            
            TpPainterGridSelections.instance.m_GridSelectionWrappers.Add(new GridSelectionWrapper(boundsInt));
            TpPainterGridSelections.instance.SaveData();
            selectionElements.Clear();
            foreach(var item in TpPainterGridSelections.instance.m_GridSelectionWrappers)
                selectionElements.Add(new SelectionElement(item.m_BoundsInt));

            listView.Rebuild();
        }


        
        
        #endregion
        
        
        #region make_bind

        private VisualElement MakeItem()
        {
            var container = new TpListBoxItem("selection-element", Color.black){style = 
                                                                               { 
                                                                                   marginTop = 2, 
                                                                                   marginBottom = 2}
                                                                               };
            container.Add(new Label(){name="field-label", style = {flexGrow = 0}});
            var field = new BoundsIntField()
                        {
                            focusable = false,
                            style =
                            {
                                flexGrow = 1
                            },
                            name  = "boundsint-field",
                            value = new BoundsInt(Vector3Int.zero, Vector3Int.forward)
                        }; //v3i.forward makes the size = 0,0,1
            
            container.Add(field); 
            return container;
        }

        
        private void BindItem(VisualElement ve, int index)
        {
            var field = ve.Q<BoundsIntField>("boundsint-field");
            var label = ve.Q<Label>("field-label");
            label.text  = (index + 1).ToString();
            field.SetValueWithoutNotify(selectionElements[index].m_BoundsInt);
        }

        #endregion
    
    }
}
