// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-05-2023
// ***********************************************************************
// <copyright file="TpPainterModifiersEditorWindow.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using static TilePlus.Editor.TpIconLib;
using Button = UnityEngine.UIElements.Button;
#nullable enable

namespace TilePlus.Editor.Painter
{
    
    /// <summary>
    /// An Editor Window for the TpPainterModifiers Scriptable Object
    /// </summary>
    public class TpPainterModifiersEditorWindow : EditorWindow, IHasCustomMenu
    {
        #region enum
        private enum TransformComponent
        {
            Position,
            Rotation,
            Scale
        }
        
        #endregion
        
        #region const
        private const float SearchFieldRadius = 4;
        private const float ListItemHeight = 16;
        private const string HelpContent = "Selected Modifier is used when <b>Apply Modifications</b> shortcut is used (default: ALT+V).\n"
                                           + "The <color=red>NAME</color> field is optional but allows searching for names with the search field near the bottom of the window\n"
                                           + "<color=red>APPLY</color> dropdown determines what is affected.\n"
                                           + "<b><i>Note that Color changes apply only to Tiles</i></b>\n"
                                           + "Use 'Defaults' to apply all the time for Prefabs or Tiles\n"
                                           + "Only one Modifier can be used for a Prefab or Tile default.\n"
                                           + "Use this window's window menu to Save and Load modifers to a file.\n"
                                           + "<color=red><b>**Ensure that LockTransform/LockColor tile flags set appropriately**\n"
                                           + "Click the button to close this field.</b></color>";
        #endregion
        
        #region privateFields
        private static TpPainterModifiersEditorWindow? s_Instance;
        private        bool                            isBinding;
        private        TpListView?                     listView;
        private        Button?                         newItemButton;
        private const  string                          AddItemNormalPrompt = "Add...";
        private const  string                          AddItemCopyPrompt   = "Add from Clipboard";
        [SerializeField]
        private string m_SearchString = string.Empty; 
        private readonly GUIContent cantDeleteGuiContent   = new GUIContent("Sorry! V'Ger can't delete the only entry!");
        private readonly GUIContent tooManyItemsGuiContent = new GUIContent("You can't have more than 32 items in the Painter Transforms List. Please remove some first.");

        #endregion

        #region properties

        /// <summary>
        /// The editor Window instance
        /// </summary>
        public static TpPainterModifiersEditorWindow? Instance => s_Instance;

        private List<ModifierWrapper> ListItems => TpPainterModifiers.instance.m_PTransformsList;

        #endregion
        
        #region ctor

        /// <summary>
        /// Open the TilePlus Modifiers editor window
        /// </summary>
        [MenuItem("Window/TilePlus/Tile+Painter Modifiers", false, 100001)]
        [MenuItem("Tools/TilePlus/Tile+Painter Modifiers", false, 1)]
        public static void ShowWindow()
        {
            s_Instance              = GetWindow<TpPainterModifiersEditorWindow>();
            s_Instance.titleContent = new("Painter Modifiers", FindIcon(TpIconType.TptIcon));
            s_Instance.minSize      = new(400 , 384);
        }

        private void OnDisable()
        {
            DestroyImmediate(TpPainterModifiers.instance);
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();
            if (TpPainterModifiers.instance == null)
            {
                rootVisualElement.Add(new TpHelpBox("TpPainterModifiers asset not found!","error-message",HelpBoxMessageType.Error));
                return;
            }

            if(TpPainterModifiers.instance.m_PTransformsList.Count == 0)
                TpPainterModifiers.instance.m_PTransformsList.Add( new ModifierWrapper());
            
            var help = new TpHelpContainer(" Tile/Prefab Modifications: click the button for more information.",HelpContent);
            
            var container = new VisualElement()
                            {
                                name = "outer-container",
                                style =
                                {
                                    flexGrow                = 1,
                                    borderBottomColor       = Color.black,
                                    borderBottomWidth       = 2,
                                    borderBottomRightRadius = 8,
                                    borderBottomLeftRadius  = 8
                                }
                            };
            rootVisualElement.Add(container);
            container.Add(help);
            
            
            
            
            listView = new(ListItems,
                           ListItemHeight,
                           true,
                           MakeTrItem,
                           BindTrItem)
                       { 
                           style =
                           {
                               flexGrow = 1
                               
                           }
                       };
            //this next line helps the content container grow/shrink when the HelpContainer is opened/closed.
            listView.Q<VisualElement>("unity-content-container").style.flexGrow = 1;
            listView.reorderable                                                = true;
            listView.showAddRemoveFooter                                        = false;
            listView.selectionChanged += ListViewOnselectionChanged;
            listView.selectionType                                              = SelectionType.Single; 
            
            container.Add(listView);
            container.Add(new TpSpacer(10,10));
            var searchFieldContainer = new VisualElement
                                       {
                                           name = "search-field-container",
                                           style =
                                           {
                                               marginTop = 4,
                                               bottom = 15,
                                               minHeight         = 40,
                                               flexGrow          = 0,
                                               flexBasis         = 0,
                                               flexShrink = 0,
                                               borderBottomWidth = 1,
                                               borderBottomColor = Color.black,
                                               marginBottom      = 2
                                           }
                                       };
            var searchInnerContainer = new VisualElement { name = "search-field-inner-container", style = {minHeight = 18,flexGrow = 1, flexDirection = FlexDirection.Row } };

            var sf = new TextField(16,
                                   false,
                                   false,
                                   ' ')
                     {
                         style =
                         {
                             minHeight = 20,
                             flexGrow            = 1, borderBottomRightRadius = SearchFieldRadius, borderBottomLeftRadius = SearchFieldRadius, borderTopRightRadius = SearchFieldRadius,
                             borderTopLeftRadius = SearchFieldRadius
                         }
                     };

            void SearchFieldCallback(ChangeEvent<string> evt)
            {
                m_SearchString = evt.newValue;

                bool SearchFieldComparison(ModifierWrapper wrapper) => wrapper.m_ModName == m_SearchString;

                var index = ListItems.FindIndex(SearchFieldComparison);
                if (index != -1)
                    listView.ScrollToItem(index);
            }

            sf.RegisterValueChangedCallback(SearchFieldCallback);
            searchInnerContainer.Add(sf);
            searchInnerContainer.Add(new TpSpacer(4, 4));
            var clearTextButton = new Button(() =>
                                             {
                                                 sf.value                                   = string.Empty;
                                                 m_SearchString = string.Empty;
                                             }) { style = { backgroundImage = FindIcon(TpIconType.UnityXIcon) } };
            searchInnerContainer.Add(clearTextButton);

            searchFieldContainer.Add(searchInnerContainer);
            searchFieldContainer.Add(new Label("Search is case-sensitive")
                                     {
                                         style =
                                         {
                                             paddingTop              = 2,
                                             marginBottom = 4,
                                             scale                   = new StyleScale(new Vector2(0.8f, 0.8f)),
                                             alignSelf               = Align.Center,
                                             unityFontStyleAndWeight = new StyleEnum<FontStyle>(FontStyle.Italic)
                                         }
                                     });
            container.Add(searchFieldContainer);
            newItemButton = new Button(AddItemEvent)
                         {
                             style =
                             {
                                 flexGrow = 0, marginBottom = 2,marginTop = 1,marginLeft = 10,marginRight = 10
                             },
                             text    = "Add...",
                             tooltip = "Click to add a new item"
                         };
            container.Add(newItemButton);
            rootVisualElement.schedule.Execute(Updater).Every(100);
            
            var modIndex = TpPainterModifiers.instance.m_ActiveIndex;
            
            if (modIndex != -1 && modIndex < TpPainterModifiers.instance.m_PTransformsList.Count)
            {
                void Callback()
                {
                    listView.SetSelection(new[] { modIndex });
                }

                TpLib.DelayedCallback(TpPainterModifiers.instance, Callback, "T+P:MOD,DlySetSel",250);
            }

        }

        


        private void Updater()
        {
            if(newItemButton == null || listView == null)
                return;
            if(TilePlusPainterWindow.RawInstance == null || TpPainterModifiers.instance == null)
                return;
            var selected  = listView.selectedItem;
            var selection = selected != null;
            var selectedIndex = selection
                                    ? listView.selectedIndex
                                    : -1;
            

            
            var cContainer = listView.Q<VisualElement>("unity-content-container");
            var numItems   = 0;
            if (cContainer != null)
                numItems = cContainer.childCount;

            for(var i = 0; i < numItems; i++)
            {
                if (listView.GetRootElementForIndex(i) is not { } item)
                {
                    Debug.Log("Burp");
                    continue;
                }

                var c = i == selectedIndex
                            ? Color.white
                            : Color.black;
                var container = item.Q<VisualElement>("element-container");
                if (container == null)
                    continue;
                container.style.borderBottomColor = c;
                container.style.borderTopColor    = c;
                container.style.borderLeftColor   = c;
                container.style.borderRightColor  = c;

            }

            
            
            newItemButton.text = TpPainterState.Clipboard is { IsTile: true, AnyModifications: true }
                                     ? AddItemCopyPrompt
                                     : AddItemNormalPrompt;

        }
        #endregion
        
        #region internalEvents
        
        private void ListViewOnselectionChanged(IEnumerable<object> obj)
        {
            var item  = obj.First() as ModifierWrapper;
            var index =TpPainterModifiers.instance.m_PTransformsList.FindIndex((wrapper) => wrapper == item);
            if (index == -1)
                return;
            TpPainterModifiers.instance.m_ActiveIndex = index;
            TpPainterModifiers.instance.SaveData();
        }

        
        private void AddItemEvent()
        {
            var numMods = TpPainterModifiers.instance.m_PTransformsList.Count;
            if (numMods  >= 32)
            {
                ShowNotification(tooManyItemsGuiContent);
                return;
            }
            
            var newMod = new ModifierWrapper();

            if (numMods > 1)
            {
                var allNames = TpPainterModifiers
                               .instance.m_PTransformsList.Where(m => !string.IsNullOrEmpty(m.m_ModName))
                               .Select(wrapper => wrapper.m_ModName)
                               .ToArray();
                var newName = ObjectNames.GetUniqueName(allNames, "Modifier");
                newMod.m_ModName = newName;
            }
            
            
            if (TilePlusPainterWindow.RawInstance != null && TpPainterState.Clipboard is { IsTile: true, AnyModifications: true } clipboard)
            {
                newMod.m_Color  = clipboard.AColor;
                newMod.m_Matrix = clipboard.transform;
            }
            TpPainterModifiers.instance.m_PTransformsList.Add(newMod);
            TpPainterModifiers.instance.SaveData();
            listView!.Rebuild();

        }

        #endregion

        #region makeBind
        /// <summary>
        /// List item
        /// </summary>
        /// <returns></returns>
        // ReSharper disable once AnnotateNotNullTypeMember
        private VisualElement MakeTrItem()
        {
            //container
            var ve = new VisualElement()
                     {
                         name = "element-container", style =
                         {
                             minHeight         = ListItemHeight * 4,
                             paddingBottom     = 2,
                             paddingTop        = 2,
                             flexDirection     = FlexDirection.Row,
                             borderBottomColor = Color.black,
                             borderTopColor    = Color.black,
                             borderLeftColor   = Color.black,
                             borderRightColor  = Color.black,
                             borderBottomWidth = 1,
                             borderTopWidth    = 1,
                             borderLeftWidth   = 1,
                             borderRightWidth  = 1,flexGrow = 1, flexShrink = 1
                         }
                     };
            // ReSharper disable once HeapView.CanAvoidClosure
            
           
            //transform fields
            var modContainer = new VisualElement()
                            {
                                name = "modification-container",
                                style =
                                {
                                    minHeight = ListItemHeight * 4,
                                    flexGrow  = 1, flexShrink = 1,
                                    alignSelf = Align.Center,
                                    alignContent = Align.Center,
                                    borderBottomColor = Color.black,
                                    borderLeftColor   = Color.black,
                                    borderRightColor  = Color.black,
                                    borderTopColor    = Color.black,
                                    borderTopWidth    = 1,
                                    borderBottomWidth = 1,
                                    borderLeftWidth   = 1,
                                    borderRightWidth  = 1
                                    
                                }
                            };
            
            
            modContainer.RegisterCallback<ClickEvent,(VisualElement,ListView)>(SelectItemCallback,new ValueTuple<VisualElement, ListView>(ve,listView!));

            var nameTextField = new TextField("Name", 32, false, false, '.')
                                          
                            {
                                name = "name-textfield",
                                tooltip = "The name field is for convenience only"
                            };
            nameTextField.Q<Label>().style.minWidth = 60;
            modContainer.Add(nameTextField);

            void NameFieldCallback(ChangeEvent<string> evt, VisualElement element)
            {
                if (evt.newValue == evt.previousValue)
                    return;
                var index = (int)element.userData;
                if (index > TpPainterModifiers.instance.m_PTransformsList.Count)
                    return;
                TpPainterModifiers.instance.m_PTransformsList[index].m_ModName = evt.newValue.Trim();
                if (!isBinding)
                    TpPainterModifiers.instance.SaveData();
            }

            nameTextField.RegisterCallback<ChangeEvent<string>, VisualElement>(NameFieldCallback, ve);

            var notValidForPrefabs = new VisualElement()
                                     {
                                         name = "not-valid-prefabs",
                                         style = {display = DisplayStyle.None, flexShrink = 1}
                                     };
            modContainer.Add(notValidForPrefabs);
            var editActionsDrop = new EnumFlagsField("Apply", TpPainterModifiers.EditActionsInit)
                                  {
                                      name = "edit-actions-drop",
                                      tooltip = "Choose one or more actions to apply when this mod is active" 
                                  };
            editActionsDrop.Q<Label>().style.minWidth = 60;

            void EditActionsDropCallback(ChangeEvent<Enum> evt, VisualElement element)
            {
                if (evt.newValue.Equals(evt.previousValue))
                    return;
                var index = (int)element.userData;
                if (index > TpPainterModifiers.instance.m_PTransformsList.Count)
                    return;
                TpPainterModifiers.instance.m_PTransformsList[index].m_EditActions = (EditActions)evt.newValue;
                if (!isBinding)
                    TpPainterModifiers.instance.SaveData();
            }

            editActionsDrop.RegisterCallback<ChangeEvent<Enum>,VisualElement>(EditActionsDropCallback,ve );
            
            notValidForPrefabs.Add(editActionsDrop);

            var colorField = new ColorField("Color"){label = "Color", name = "color-field", 
                                                        value = Color.white,tooltip = "Color to be used when Apply flag for color is active",
                                                        style =
                                                    {
                                                        flexGrow = 1,
                                                        flexShrink = 1
                                                    }};
            colorField.Q<Label>().style.minWidth = 60;
            notValidForPrefabs.Add(colorField);
            colorField.RegisterValueChangedCallback(evt =>
                                                           {
                                                               if(evt.newValue.Equals(evt.previousValue))
                                                                   return;

                                                               var index = (int)ve.userData;
                                                               if (index > TpPainterModifiers.instance.m_PTransformsList.Count)
                                                                   return;
                                                               TpPainterModifiers.instance.m_PTransformsList[index].m_Color = evt.newValue;
                                                               if(!isBinding)
                                                                    TpPainterModifiers.instance.SaveData();
                                                           });

            
            
            var posField = new Vector3Field("Position") { name = "position",tooltip = "Position mod used when Apply flag for transform is active" };
            posField.Q<Label>().style.minWidth = 60;
            posField.RegisterCallback<ChangeEvent<Vector3>>((evt) => ChangeItem(TransformComponent.Position, evt.newValue));
            posField.Add(new Button(()=>ResetTransformItem(TransformComponent.Position)){text = "R", tooltip = "Reset Position"});
                
                
            var rotField = new Vector3Field("Rotation") { name = "rotation", tooltip= "Rotation mod used when Apply flag for transform is active"};
            rotField.Q<Label>().style.minWidth = 60;
            rotField.RegisterCallback<ChangeEvent<Vector3>>((evt) => ChangeItem(TransformComponent.Rotation, evt.newValue));
            rotField.Add(new Button(()=>ResetTransformItem(TransformComponent.Rotation)){text = "R",tooltip = "Reset Rotation"});

            var scaleField = new Vector3Field("Scale") { name = "scale", tooltip = "Scale mod used when Apply flag for transform is active"};
            scaleField.Q<Label>().style.minWidth = 60;
            scaleField.RegisterCallback<ChangeEvent<Vector3>>((evt) => ChangeItem(TransformComponent.Scale, evt.newValue));
            scaleField.Add(new Button(()=>ResetTransformItem(TransformComponent.Scale)){text = "R",tooltip = "Reset Scale"});

            
            modContainer.Add(posField);
            modContainer.Add(rotField);
            modContainer.Add(scaleField);
            
            ve.Add(modContainer);

            //right side of the element has radio buttons and the delete button.
            var buttonImage = TpIconLib.FindIcon(TpIconType.UnityXIcon);
            //var h           = buttonImage.height;
            //var w           = buttonImage.width;

            var button = new Button(DeleteItemClickEvent)
                         {
                             name = "delete", tooltip = "Delete this entry", style =
                             {
                                 backgroundImage = buttonImage, alignSelf = Align.Center, flexGrow = 0, height = 15,
                                 width           = 15
                             }
                         };
            if (!EditorGUIUtility.isProSkin)
                button.style.backgroundColor = Color.black; 

            var radio = new RadioButtonGroup("Defaults",
                                             new List<string>()
                                             {
                                                 "Neither",
                                                 "Tiles",
                                                 "Prefabs"
                                             })
                        /* the tooltip makes it difficult to operate the radio buttons! {
                            tooltip = "Set a default modification for ALL Tiles or Prefabs."
                        }*/;
            radio.Q<Label>().style.minWidth = 60;
            radio.SetValueWithoutNotify(0);
            radio.RegisterCallback<ChangeEvent<int>>(RadioPick);
            var radioLabel = radio.Q<Label>();
            radio.name                    = "radio-group";
            radioLabel.style.paddingLeft  = 8;
            radio.style.flexDirection     = FlexDirection.Column;
            radio.style.paddingBottom     = 2;
            radio.style.borderBottomColor = Color.black;
            radio.style.borderLeftColor   = Color.black;
            radio.style.borderRightColor  = Color.black;
            radio.style.borderTopColor    = Color.black;
            radio.style.borderTopWidth    = 1;
            radio.style.borderBottomWidth = 1;
            radio.style.borderLeftWidth   = 1;
            radio.style.borderRightWidth  = 1;
            radio.Add(new Label("Prefabs:\nTransform only"));

            ve.Add(radio);
            ve.Add(button);

            return ve;

            //0 for pos, 1 for rot, 2 for scale.
            void ResetTransformItem(TransformComponent which)
            {
                var index = (int)ve.userData;
                
                if (index > TpPainterModifiers.instance.m_PTransformsList.Count)
                    return;
                var transform = TpPainterModifiers.instance.m_PTransformsList[index].m_Matrix;

                GetTransformComponents(transform, out var pos, out var rot, out var scale);

                transform = which switch
                            {
                                TransformComponent.Position => Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(rot), scale),
                                TransformComponent.Rotation => Matrix4x4.TRS(pos,          Quaternion.identity,   scale),
                                TransformComponent.Scale    => Matrix4x4.TRS(pos,          Quaternion.Euler(rot), Vector3.one),
                                _                           => transform
                            };

                TpPainterModifiers.instance.m_PTransformsList[index].m_Matrix = transform;
                
                TpPainterModifiers.instance.SaveData();
                if(listView != null) //unlikely!
                    listView.RefreshItems();
            }

            void RadioPick(ChangeEvent<int> evt)
            {
                var radioIndex = evt.newValue; 

                foreach (var transformWrapper in TpPainterModifiers.instance.m_PTransformsList)
                {
                    if (transformWrapper.m_DefaultTarget == (DefaultTarget)radioIndex)
                        transformWrapper.m_DefaultTarget = DefaultTarget.None;
                }
                
                //now set the corresponding item to whatever the radio button is set to
                var index = (int)ve.userData;
                TpPainterModifiers.instance.m_PTransformsList[index].m_DefaultTarget = (DefaultTarget)radioIndex;
                TpPainterModifiers.instance.SaveData();
                listView!.RefreshItems();
            }

            void ChangeItem(TransformComponent which, Vector3 value)
            {
                var index     = (int)ve.userData;
                if (index > TpPainterModifiers.instance.m_PTransformsList.Count)
                    return;
                var transform = TpPainterModifiers.instance.m_PTransformsList[index].m_Matrix;
                
                GetTransformComponents(transform,out var pos, out var rot, out var scale);
                if (which == TransformComponent.Position)
                    TpPainterModifiers.instance.m_PTransformsList[index].m_Matrix = Matrix4x4.TRS(value, Quaternion.Euler(rot), scale);
                else if(which == TransformComponent.Rotation)
                    TpPainterModifiers.instance.m_PTransformsList[index].m_Matrix = Matrix4x4.TRS(pos, Quaternion.Euler(value), scale);
                else if(which == TransformComponent.Scale)
                    TpPainterModifiers.instance.m_PTransformsList[index].m_Matrix = Matrix4x4.TRS(pos, Quaternion.Euler(rot), value);
                if(!isBinding)
                    TpPainterModifiers.instance.SaveData();
            }
                
            void DeleteItemClickEvent()
            {
                if (TpPainterModifiers.instance.m_PTransformsList.Count < 2)
                {
                    ShowNotification(cantDeleteGuiContent);
                    return;
                }
                var index = (int)ve.userData;
                TpPainterModifiers.instance.m_PTransformsList.RemoveAt(index);
                TpPainterModifiers.instance.m_ActiveIndex = 0;
                TpPainterModifiers.instance.SaveData();
                
                listView!.Rebuild();
                listView.SetSelectionWithoutNotify(new []{0});
            }
            
            void GetTransformComponents(Matrix4x4   transform,
                                        out Vector3 tPosition,
                                        out Vector3 tRotation,
                                        out Vector3 tScale)
            {
                tPosition = transform.GetPosition();
                tRotation = transform.rotation.eulerAngles;
                tScale    = transform.lossyScale;
            }
            
            
            

        }

        private void SelectItemCallback(ClickEvent _, (VisualElement, ListView) args)
        {
            /*(var element, var listVu) = args;
            var selection = (int)element.userData;
            listVu.SetSelectionWithoutNotify(new[] { selection });
            SetSelectionInAsset(selection);*/
        }


        // ReSharper disable once AnnotateNotNullParameter
        private void BindTrItem(VisualElement ve, int index)
        {
            var items = ListItems;
            if (index >= items.Count)
                return;

            var posField           = ve.Q<Vector3Field>("position");
            var rotField           = ve.Q<Vector3Field>("rotation");
            var scaleField         = ve.Q<Vector3Field>("scale");
            var radio              = ve.Q<RadioButtonGroup>("radio-group");
            var notValidForPrefabs = ve.Q<VisualElement>("not-valid-prefabs");
            var editActionsDrop    = ve.Q<EnumFlagsField>("edit-actions-drop");
            var colorField         = ve.Q<ColorField>("color-field");
            var nameField          = ve.Q<TextField>("name-textfield");
            var item               = items[index];
            
            radio.SetValueWithoutNotify((int)item.m_DefaultTarget);
            notValidForPrefabs!.style.display = item.m_DefaultTarget == DefaultTarget.Prefabs
                                                    ? DisplayStyle.None
                                                    : DisplayStyle.Flex;
            var nameText = string.IsNullOrEmpty(item.m_ModName)
                               ? "Enter optional name"
                               : item.m_ModName;
            nameField.SetValueWithoutNotify(nameText);
            posField.userData   = index;
            rotField.userData   = index;
            scaleField.userData = index;

            var matrix = item.m_Matrix;
            ve.userData = (object)index;

            var pos   = matrix.GetPosition();
            var rot   = matrix.rotation.eulerAngles;
            var scale = matrix.lossyScale;

            var rPos      = TileUtil.RoundVector3(pos,   4);
            var rRotation = TileUtil.RoundVector3(rot,   4);
            var rScale    = TileUtil.RoundVector3(scale, 4);
            
            //SaveData for asset inhibited during BindItem to avoid extra callbacks.
            isBinding        = true; //inhibit auto-save
            posField.value   = rPos;
            rotField.value   = rRotation;
            scaleField.value = rScale;
            editActionsDrop.value = (Enum)item.m_EditActions;
            colorField.value = item.m_Color;
            //tileFlagsField.value = item.m_Flags;
            isBinding        = false; 
        }
        #endregion

        #region save_load
        /// <inheritdoc />
        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Save", "Save Modifiers to a file"), false, SaveModifiers);
            menu.AddItem(new GUIContent("Load", "Load Modifiers from a file"), false, LoadModifiers);
        }

        private void SaveModifiers()
        {
            var path     = EditorUtility.SaveFilePanel("Where to save", "", "PainterModifiers", "tpt");
            if (path == string.Empty)
                return;
            var json = EditorJsonUtility.ToJson( TpPainterModifiers.instance,true);
                
            try
            {
                File.WriteAllText(path, json);
            }
            catch (Exception e)
            {
                Debug.LogError("Saving Modifiers failed. " + e.Message);
            }
        }

        private void LoadModifiers()
        {
            var path = EditorUtility.OpenFilePanel("Load Modifiers", "", "tpt");
            if (path == string.Empty)
                return;

            try
            {
                var json = File.ReadAllText(path);
                if (json == string.Empty)
                {
                    ShowNotification(new GUIContent("Empty file!!"));
                    return;
                }

                Debug.Log("-->Ignore the 'ScriptableSingleton already exists' message on the next log entry.");
                var newInstance = CreateInstance<TpPainterModifiers>();
                EditorJsonUtility.FromJsonOverwrite(json, newInstance);
                var modifiers = newInstance.m_PTransformsList;
                TpPainterModifiers.instance.m_PTransformsList.Clear();
                TpPainterModifiers.instance.m_PTransformsList.AddRange(modifiers);
                TpPainterModifiers.instance.SaveData();
                DestroyImmediate(newInstance);
                listView?.Rebuild();
            }
            catch (Exception e)
            {
                Debug.LogError("Loading Modifiers failed. " + e.Message);
            }
        }
        
        
        #endregion
    }
}
