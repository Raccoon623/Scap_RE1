// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-05-2023
// ***********************************************************************
// <copyright file="TpTemplateCreator.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Template creator tool</summary>
// ***********************************************************************

using System.Collections.Generic;
using System.IO;
using System.Linq;
using TilePlus.Editor.Painter;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UIElements;

#nullable enable

namespace TilePlus.Editor
{
    /// <summary>
    /// Template creator for use with TpZoneLayout
    /// </summary>
    public class TpTemplateCreatorWindow : EditorWindow
    {
        #region privateFields

        private static   TpTemplateCreatorWindow?    s_Instance;
        private          ObjectField?                gridUiField;
        private          TpHelpBox?                  gatherDataHelpBox1;
        private          TpToggleLeft?               createSelectorAsset;
        private          TpToggleLeft?               hideFromPainter;
        private readonly Dictionary<string, Tilemap> maps = new();
        [SerializeField]
        private Grid? m_Grid;

        private int        step;
        
        private Button? activateButton;

        #endregion

        #region init

        /// <summary>
        /// Open the Template Creator window
        /// </summary>
        [MenuItem("Tools/TilePlus/Template Tool", false, 10001)]
        public static void ShowWindow()
        {
            if (RawInstance != null) //ie window already created
            {
                GetWindow<TpTemplateCreatorWindow>();
                return;
            }

            s_Instance              = GetWindow<TpTemplateCreatorWindow>();
            s_Instance.titleContent = new GUIContent("Tile+ Template Tool", TpIconLib.FindIcon(TpIconType.TptIcon));
            s_Instance.minSize      = new Vector2(384, 256);
        }

        #endregion


        #region events

        private void OnEnable()
        {
            step                                   =  0;
            EditorApplication.playModeStateChanged += OnplayModeStateChanged;
            maps.Clear();
        }

        private bool disabledAlready;
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnplayModeStateChanged;
            Unregister();
        }

        private void OnDestroy()
        {
            Unregister();
        }
        
        private void Unregister()
        {
            if(disabledAlready)
                return;
            disabledAlready                        =  true;
            maps.Clear();
            step        = 0;
        }



        private void OnInspectorUpdate()
        {
            var cs = TilePlusPainterConfig.PainterFabAuthoringChunkSize;
            
            if (step == 0)
            {
                activateButton?.SetEnabled(maps.Count != 0);
                if (gatherDataHelpBox1 == null)
                    return;
                
                var origin = TilePlusPainterConfig.FabAuthWorldOrigin;
                gatherDataHelpBox1.text = $"ChunkSize: {cs}, World Origin: {origin}";
            }
        

            if (step == 1)
            {
                var activate = false;
                if (GridSelection.active)
                {
                    //a selection must be > 1 chunk and a multiple of the chunk size and square.
                    var size = GridSelection.position.size;
                    if (size.x == size.y && size.x > cs && size.x % cs == 0)
                        activate = true;
                }
                
                activateButton?.SetEnabled(activate);

            }
        }
        
        private void OnplayModeStateChanged(PlayModeStateChange _)
        {
            DelayedCloseWindow();
        }

        private void DelayedCloseWindow(int delayInMsec = 200)
        {
            TpLib.DelayedCallback(this, this.Close, "Template-Tool-Auto-Close", delayInMsec);
        }

        #endregion
        
        
        #region properties
        /// <summary>
        /// Get the window instance directly. will not open any window. Use with care...
        /// </summary>
        // ReSharper disable once ConvertToAutoPropertyWithPrivateSetter
        // ReSharper disable once MemberCanBePrivate.Global
        public static TpTemplateCreatorWindow RawInstance => s_Instance!;

        
        #endregion

       
        #region GUI
        private void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.Add(new TpHeader("Tile+ Template Tool", "title-content"));
            
            
            switch (step)
            {
                case 0:
                    GatherData();
                    break;
                case 1:
                    RecordPrompts();
                    break;
                case 2:
                    Save();
                    break;
            }            

        }


        private VisualElement StandardButtons(string nextStepPrompt)
        {
            var container = new VisualElement()
                            {
                                style =
                                {
                                    height = 20,
                                    flexGrow      = 1,
                                    flexDirection = FlexDirection.Row
                                    
                                },
                                name = "button-container"
                            };
            container.Add(RestartButton());
            activateButton = NextButton(nextStepPrompt); 
            container.Add(activateButton);
            return container;
        }
        
        private Button RestartButton(string uiName = "restart-button",float flxgrow = 1f)
        {
             return new Button(Restart)
                    {
                        style =
                        {
                            height        = 20,
                            flexGrow      = flxgrow,
                            flexDirection = FlexDirection.Row
                        },
                        text = "Restart",tooltip = "Restart the Wizard",
                        name = uiName
                    };
        }

        private void Restart()
        {
            step = 0;
            TpLib.DelayedCallback(s_Instance, () =>
                                              {
                                                  rootVisualElement.Clear();
                                                  CreateGUI();
                                              },"TemplToolWizRestart",100);
        }
        
        private Button NextButton(string prompt, string uiName = "next-button", float flxGrow = 1)
        {
            return new Button(() =>
                              {
                                  activateButton = null;
                                  step++;
                                  TpLib.DelayedCallback(s_Instance,
                                                        CreateGUI,
                                                        "TemplToolWizNextStep",
                                                        100);
                                                
                                                
                       })
                   {
                       style =
                       {
                           height        = 20,
                           flexGrow      = flxGrow,
                           flexDirection = FlexDirection.Row
                       },
                       text = prompt, tooltip = "Advance to next step",
                       name = uiName
                   };
        }
        
        
        private void GatherData()
        {
            var cs     = TilePlusPainterConfig.PainterFabAuthoringChunkSize;
            var origin = TilePlusPainterConfig.FabAuthWorldOrigin;
            gatherDataHelpBox1 = new TpHelpBox($"Chunk Snapping ON, ChunkSize: {cs}, World Origin: {origin}", "gatherdataphase-help"){BackGroundAlpha = 0};
            rootVisualElement.Add(gatherDataHelpBox1);
            rootVisualElement.Add(new TpSpacer(8, 20));                

            var container = new VisualElement() { style = { flexDirection = FlexDirection.Row, alignContent = Align.Stretch } };
            
            rootVisualElement.Add(container);
            var infobox = new TpListBoxItem("gather-infobox", Color.red)
                          {style = {flexDirection = FlexDirection.Column}};

            var helpbox =
                new  TpHelpBox("Drag the parent Grid of the Tilemaps that you want to use into the above field.",
                          "grid-help-text")
                {
                    BackGroundAlpha = 0, style = { unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft) }
                    
                };
            
            infobox.Add(helpbox);
            infobox.Add(new TpSpacer(4,20));
            helpbox =
                new TpHelpBox("This tool uses ChunkSize and World Origin from the Tile+Painter settings panel.\n\nWhen you're sure that these are set up correctly, Click NEXT.",
                              "grid-help-text-2")
                {
                    BackGroundAlpha = 0, style = { unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft)}

                };
            infobox.Add(helpbox);
            
            rootVisualElement.Add(infobox);
            
            //if the grid was already set need to update maps dict
            if (m_Grid != null)
            {
                maps.Clear();
                foreach (var m in m_Grid.GetComponentsInChildren<Tilemap>())
                    maps.Add(m.name, m);
            }
            
            
            gridUiField = new ObjectField("Parent Grid") { style = { flexGrow = .75f }, allowSceneObjects = true, objectType = typeof(Grid), name = "grid-object-field", value = m_Grid };
            gridUiField.RegisterValueChangedCallback(evt =>
                                                     {
                                                         evt.StopImmediatePropagation();

                                                         var g = evt.newValue as Grid;
                                                         if (g==null)
                                                         {
                                                             m_Grid = null;
                                                                maps.Clear();
                                                             return;
                                                         }

                                                         foreach (var m in g.GetComponentsInChildren<Tilemap>())
                                                             maps.Add(m.name, m);
                                                         if (maps.Count != 0)
                                                         {
                                                             m_Grid = g;
                                                             return;
                                                         }

                                                         m_Grid = null;
                                                         Debug.LogError("There were no Tilemaps attached to that Grid. Try again.");
                                                     });
            activateButton = NextButton("NEXT");
            container.Add(gridUiField);
            container.Add(activateButton);
        }

        private void RecordPrompts()
        {

            createSelectorAsset = new TpToggleLeft("Create Template Selector Asset"){value = true, tooltip = "If checked, also create a ChunkTemplateSelector asset preset with the new Template."};
            rootVisualElement.Add(createSelectorAsset);
            hideFromPainter = new TpToggleLeft("Hide the created assets from Tile+Painter") { value = true, tooltip = "If checked, sets the 'Ignore in Painter' field in the TileFabs and Bundles so that they don't appear in Tile+Painter lists. You can change this manually later." };
            rootVisualElement.Add(hideFromPainter);
                
            rootVisualElement.Add(new TpHelpBox("Use the Palette OR the Tile+Painter GridSelectionPanel's shortcut key to create a Grid Selection around the painted TileFabs that you want to use for a Template.\n\n"
                                            + "Important: the selection must be SQUARE, a multiple of the Chunk Size, and the dimensions must be even numbers of tiles.\n\n"
                                            + "When ready, Click Make Template ","helpbox-record-prompts"){style = { unityTextAlign = new StyleEnum<TextAnchor>(TextAnchor.MiddleLeft)}});
            rootVisualElement.Add( StandardButtons("Make Template"));
        }

       



        void Cancel()
        {
            maps.Clear();
            step = 0;
        }
        
        private void Save()
        {
            if (m_Grid == null)
            {
                EditorUtility.DisplayDialog("Error", "The Grid Selection became invalid: Try again!", "Continue");
                Cancel();
                Restart();
                return;
            }
            if (!GridSelection.active || GridSelection.grid.gameObject != m_Grid.gameObject)
            {
                EditorUtility.DisplayDialog("Mismatch", "The Grid Selection you made was not on the Grid as selected in this wizard. Try again!", "Continue");
                Cancel();
                Restart();
                return;
            }
            
            var path = TpEditorUtilities.GetPathFromUser("Select destination folder for saving the new TileFabs & Template.");
            if (string.IsNullOrEmpty(path))
            {
                Cancel();
                Restart();
                return;
            }

            var          r      = new RectInt((Vector2Int) GridSelection.position.position, (Vector2Int) GridSelection.position.size);
            var          subdivisions   = new List<RectInt>();
            const string zmName = "TempZmTemplateCreator";
            
            //have to turn on the ZoneManager for a bit
            var zmWasOn = TileFabLib.AnyActiveZoneManagers;
            if (!zmWasOn)
                TileFabLib.EnableZoneManagers();
            TileFabLib.CreateZoneManagerInstance(out var zm, zmName, maps);
            if (zm == null)
            {
                if (!zmWasOn)
                    TileFabLib.EnableZoneManagers(false);
                Cancel();
                Restart();
                return;
            }
            zm.Initialize(TilePlusPainterConfig.PainterFabAuthoringChunkSize,TilePlusPainterConfig.FabAuthWorldOrigin);

            TpZoneManagerLib.SubdivideRectInt(r, 16, ref subdivisions, zm); 

            var m    = maps.Values.ToArray();
            var grid = m_Grid.gameObject;
            var fabGuids = new List<string>(m.Length);
            var fabs = new List<TpTileFab>(m.Length);
            
            foreach (var div in subdivisions!)
            {
                //Debug.Log($"tc {div.ToString()}");
                var selBounds = new BoundsInt((Vector3Int) div.position, new Vector3Int(div.size.x, div.size.y, 1)); //can't use cast since z can't be zero.
                (_, _, _, var fab, var success,
                 var failureWasEmptyArea,_) = TpPrefabUtilities.Pack(grid.scene.ToString(),
                                                                   path,
                                                                   grid,
                                                                   selBounds,
                                                                   m, 
                                                                   true,
                                                                   false,TpPrefabUtilities.SelectionBundling.All,true,false,hideFromPainter!.value);
                if(!success && !failureWasEmptyArea)
                {
                    DisableZm();
                    Cancel();
                    Restart();
                    EditorUtility.DisplayDialog("OOPS!", "Some sort of error occurred when creating TileFabs -- try again", "Continue");
                    return;
                }

                fabs.Add(fab!);
                fabGuids.Add(fab != null
                             ? fab.AssetGuidString
                             : string.Empty);
                //Debug.Log($"Fab: {tileFabAssetName} at {tileFabAssetPath}. Fab Bounds: {fabBounds}");
            }

            DisableZm();


            var templatePathName = Path.Combine(path,"ChunkLayoutTemplate.asset");
            
            if (templatePathName == string.Empty)
            {
                Cancel();
                Restart();
                EditorUtility.DisplayDialog("Hmmmm...", "Your template TileFabs were created but you didn't create a template?", "Continue");
                return;
            }
            
            var template =ScriptableObject.CreateInstance<TpChunkLayoutTemplate>();
            if (template == null)
            {
               Debug.LogError($"Could not create an instance of TpChunkLayoutTemplate at {path}");
               Cancel();
               Restart();
               return;
            }

            template.m_TileFabGuids = fabGuids.ToArray();
            template.m_TileFabs = fabs.ToArray();
            Debug.Log(templatePathName);
            
            var objPath = AssetDatabase.GenerateUniqueAssetPath(templatePathName);
            AssetDatabase.CreateAsset(template, objPath);

            //create selector too?
            if (createSelectorAsset is { value: true })
            {
                var selector =ScriptableObject.CreateInstance<TpChunkTemplateSelector>();
                if (selector == null)
                {
                    Debug.LogError($"Could not create an instance of TpChunkTemplateSelector at {path}");
                    Cancel();
                    Restart();
                    return;
                }
                var selectorPathName = Path.Combine(path, "ChunkTemplateSelector.asset");
                selector.m_ChunkLayoutTemplate = template;
                objPath                        = AssetDatabase.GenerateUniqueAssetPath(selectorPathName);
                AssetDatabase.CreateAsset(selector, objPath);
            }
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        
            Cancel();
            Restart();
            return;


            void DisableZm()
            {
                if (zmWasOn)
                    TileFabLib.DeleteNamedInstance(zmName);
                else
                    TileFabLib.EnableZoneManagers(false);
            }
        }
        
        #endregion
    }
}
