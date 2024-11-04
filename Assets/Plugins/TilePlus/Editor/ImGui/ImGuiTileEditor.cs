// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-10-2022
// ***********************************************************************
// <copyright file="ImGuiTileEditor.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Generates IMGUI selection and brush inspectors</summary>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

#nullable enable

namespace TilePlus.Editor
{
    /// <summary>
    /// Specifies which inspector is in use.
    /// </summary>
    public enum TppInspectorSpec
    {
        /// <summary>
        /// The selection inspector (or Utility Window)
        /// </summary>
        Selection,
        /// <summary>
        /// The brush inspector
        /// </summary>
        Brush
    }
    
    /// <summary>
    /// Completion state for the Sorter (external entry point for all callers) and for the display generator
    /// </summary>
    public readonly struct CompletionState
    {
        /// <summary>
        /// found props with appropriate tags? Also is false if the tile was not a TPT tile.
        /// </summary>
        public readonly bool m_FoundTaggedTile;

        /// <summary>
        /// Inhibit auto-save if true. Use for certain fields (Range sliders or CustomGui). 
        /// </summary>
        public readonly bool m_InhibitAutoSave;

        /// <summary>
        /// Refresh the tile or not
        /// </summary>
        public readonly bool m_RefreshTile;

        /// <summary>
        /// UpdateTplib true means call TpLib.UpdateInstance
        /// </summary>
        public readonly bool m_UpdateTileInstance;

        /// <summary>
        /// If UpdateTpLib is true, then this holds the names of the edited fields.
        /// </summary>
        public readonly string[] m_FieldNames;

        /// <summary>
        /// The completion state of the process.
        /// </summary>
        /// <param name = "foundTaggedTile">Found a tile with TPT tags. If false can also mean that the tile being inspected was not using ITilePlus or was not TilePlusBase, or was not subclassed from TIlePlusBase</param>
        /// <param name = "refreshTile" >Refresh the tile using tilemap.Refreshtile</param>
        /// <param name = "updateTileInstance">update the tile instance using TpLib.UpdateInstance</param>
        /// <param name = "fieldNames">Field names to update, ignored if updateTileInstance == false</param>
        /// <param name = "inhibitAutoSave" >if true, don't use autosave (if enabled from Config panel)</param>
        public CompletionState(bool      foundTaggedTile,
                               bool      inhibitAutoSave,
                               bool      refreshTile,
                               
                               bool         updateTileInstance,
                               string[]? fieldNames = null)
        {
            m_FoundTaggedTile   = foundTaggedTile;
            m_InhibitAutoSave    = inhibitAutoSave;
            m_RefreshTile        = refreshTile;
            m_UpdateTileInstance = updateTileInstance;
            m_FieldNames         = fieldNames!;
        }
    }

    /// <summary>
    /// This static class formats and displays information about Tile class instances in
    /// the IMGUI environment. This is controlled by several attributes that you can find
    /// just below the class declaration below.
    /// A limited number of field types are handled. Changes are written to the tile instance
    /// in the tilemap. The changes are saved when you save the scene.
    /// Because we're affecting the tilemap, no assetdatabase functionality is involved at all.
    /// </summary>
    [InitializeOnLoad]
    public static class ImGuiTileEditor
    {
        #region staticInit
        static ImGuiTileEditor()
        {
            //set up namespaces that are accepted
            s_Namespaces.Add("TilePlus"); //always this one
            
        }
        #endregion

        
        #region variables-props-classes
        //all the namespaces for speedy namespace checking
        private static readonly HashSet<string> s_Namespaces = new();

        //is the application playing?
        private static bool s_IsPlaying;

        //config instance handle
        private static TilePlusConfig? s_Config;

        //in safe play mode?        
        private static bool s_SafePlay;

        //changed to public in V141
        /// <summary>
        /// Completion state for failure
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public static readonly CompletionState S_FailureCompletionState = new(false,
                                                                              false,
                                                                              false,
                                                                              false);

        /// <summary>
        /// Get the class names in use for this pass of the Editor
        /// </summary>
        public static List<string> CurrentClassNames => s_CurrentClassnames;
        
        /// <summary>
        /// The currently-examined TilePlus tile GUID. Will be null if current tile is not a TilePlus tile
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public static Guid? CurrentTilePlusGuid { get; set; }

        private static readonly List<string> s_CurrentClassnames = new();

        private static readonly List<string> s_ModifiedFieldNames = new();

        /*every time a tile instance is examined, a dictionary of its types and per-class-info
         is created and cached in this dictionary so that it can be reused the next time that
         the same tile instance or a different instance with the same type heirarchy is encountered.
         Note that this isn't persisted in any way but instead recreated the first time that a
         type is encountered
        */
        private static readonly Dictionary<Type, Dictionary<Type, PerClassInfo>> s_CachedInfoDicts = new(); 
        
        /// <summary>
        /// Used to cache fields and properties for each class we encounter
        /// so they don't have to be extracted each pass.
        /// </summary>
        private class PerClassInfo
        {
            public readonly List<FieldInfo>    m_FieldInfos      = new();
            public readonly List<PropertyInfo> m_PropertyInfos   = new();
            public readonly List<MethodInfo>   m_MethodInfos     = new();
            public readonly List<PropertyInfo> m_StringPropInfos = new();
        }
        //acceptable field attributes
        private static readonly HashSet<Type> s_ValidAttrsForFields = new()
                                                                      {
                                                                                typeof(TptShowAsLabelBrushInspectorAttribute),
                                                                                typeof(TptShowAsLabelSelectionInspectorAttribute),
                                                                                typeof(TptShowEnumAttribute),
                                                                                typeof(TptShowFieldAttribute),
                                                                                typeof(TptShowObjectFieldAttribute),
                                                                                typeof(TooltipAttribute)
                                                                            };

        //acceptable method attributes
        private static readonly HashSet<Type> s_ValidAttrForMethods = new()
                                                                      {
                                                                                typeof(TptShowCustomGUIAttribute),
                                                                                typeof(TptShowMethodAsButtonAttribute)
                                                                            };
        
        //editable field Types
        private static readonly HashSet<Type> s_EditableTypes = new()
                                                                {
                                                                          typeof(bool), typeof(int), typeof(float), typeof(string), typeof(Vector3),
                                                                          typeof(Vector3Int), typeof(Vector2Int), typeof(Vector2), typeof(Color)
                                                                      };

        //GUI-related constants, fields, and properties

        private static GUIContent? s_ArrowRightIcon;
        private static GUIContent? s_OpenInInspectorButtonGUIContent;
        private static GUIContent? s_RuntimeGoFlagGuiContent;
        private static GUIContent? s_KeepGoOnEraseFlagGuiContent;
        private static GUIContent? s_ColorFlagGuiContent;
        private static GUIContent? s_TransFlagGuiContent;
        private static GUIContent? s_ColorGuiContent;
        private static GUIContent? s_PosGuiContent;
        private static GUIContent? s_RotGuiContent;
        private static GUIContent? s_ScaleGuiContent;                 
        
        private const           float   SpacePixels      = 5;
        private const           float   LineWidth        = 1;
        private static readonly char[]  s_DelimForParens = {'('};
        private static readonly char[]  s_DelimForDot    = {'.'};
        private static readonly char[]  s_DelimForComma  = {','};
        private static          Vector2 s_ButtonSize     = new(15, 15);
        private const           float   ButtonSizeScale  = 0.9f;
        private static          bool    s_AllowRangeSliders;
        private static          bool    s_ClassHeaders;
        private static readonly Type    s_TypeofTilePlusBase = typeof(TilePlusBase);

        private static GUISkin? s_TpSkin;
        private static GUISkin TpSkin
        {
            get
            {
                if (s_TpSkin != null)
                    return s_TpSkin;
                s_TpSkin = TpEditorUtilities.GetSkin();
                return s_TpSkin == null ? GUI.skin : s_TpSkin;
            }
        }
        #endregion
        
        #region sorter

        /// <summary>
        /// create IMGUI for attributes on Tiles.
        /// Note that this isn't for general purpose use
        /// on objects in the AssetDatabase. It modifies
        /// fields in TilePlus tiles, which are Scene objects.
        /// Note that any caller using whichInspector = TppInspectorSpec.Selection
        /// should examine CompletionState.m_RefreshType in the returned CompletionState struct and
        /// a. refresh the tile
        /// b. if TilePlusConfig.AutoSave is true, save the scene. That can't be done here
        /// Note that Gui.skin is set to null upon return;
        /// 
        /// </summary>
        /// <param name="target">tile to be inspected</param>
        /// <param name="whichInspector">Brush or Selection inspector as caller</param>
        /// <param name="activeScene">Scene struct for scene of object being inspected.</param>
        /// <param name = "noEdit" >disallow editing, usually because obj is within a prefab or similar. Ignored if config allows prefab editing</param>
        /// <param name = "map" >Parent tilemap. Only used for Tile-type tiles and not for TPT</param>
        /// <param name = "position" >tile tilePosition. Only used for Tile-type tiles and not for TPT</param>
        /// <returns>a CompletionState instance with info for caller.</returns>
        /// <remarks>NOTE that when the target is not a TPT tile, a display similar to the standard Selection
        /// Inspector is used. Completion state should be IGNORED. Note that this only works when whichInspector is Selection.</remarks>
        public static CompletionState GuiForTilePlus(
                UnityEngine.Object target, //141 - changed to UnityEngine.Object for more appropriate null detection.
                TppInspectorSpec   whichInspector,
                Scene              activeScene,
                bool               noEdit,
                Tilemap?           map      = null,
                Vector3Int         position = default)
        {
            CurrentTilePlusGuid = null;
            
            //first time thru, do this. Can't do in constructor due to use of Scriptable Singleton. Throws errs from Unity internals.
            if (s_Config == null)
            {
                s_Config = TilePlusConfig.instance;
                if (s_Config == null)
                    return S_FailureCompletionState;

                if (s_Config.InformationalMessages)
                    Debug.Log("Configuring ImGuiTileEditor Icons");

                InitIcons();
                
                if (s_Config.InformationalMessages)
                    Debug.Log($"Adding namespaces: {s_Config.NameSpaces}");
                var cleanNamespaces = s_Config.NameSpaces.Trim();          //remove whitespace
                //1.4.3: add trim to ns. Extra whitespace in config will make the ns unusable!
                foreach (var ns in cleanNamespaces.Split(s_DelimForComma)) //split
                    s_Namespaces.Add(ns.Trim());                           //add each namespace. Note that this is a HashSet not a list.
                var buttonSize = s_Config.SelInspectorButtonSize * ButtonSizeScale;
                s_ButtonSize.x      = buttonSize;
                s_ButtonSize.y      = buttonSize;
                s_AllowRangeSliders = s_Config.SlidersAllowed;
                s_ClassHeaders      = s_Config.ClassHeaders;
                s_SafePlay          = s_Config.SafePlayMode;

            }

            if (s_Config.AllowPrefabEditing)
                noEdit = false;

            s_IsPlaying = Application.isPlaying;
            
            if (s_IsPlaying && s_SafePlay)
            {
                EditorGUILayout.HelpBox("Safe Play mode in effect. Your tiles are untouched! ", MessageType.Info, true);
                return S_FailureCompletionState;
            }

            //+ 141: null detection improvements.
            // ReSharper disable once UseNullPropagation
            if(target == null) //need to test for Unity null too
                return S_FailureCompletionState;
            //+1.5 added simple 'normal' selection inspector for standard unity Tiles.
            // ReSharper disable once Unity.NoNullPatternMatching
            if (target is not TilePlusBase tpb)
            {
                
                if (whichInspector != TppInspectorSpec.Selection)
                    return S_FailureCompletionState;
                // ReSharper disable once Unity.NoNullPatternMatching
                if (map != null && target is TileBase unityTile) 
                    return TileSelectionInspector (unityTile, map, position);
                return S_FailureCompletionState;
            }

            if(whichInspector == TppInspectorSpec.Selection)
                CurrentTilePlusGuid = tpb.TileGuid;
            //for this target, get it's type, fields, properties, methods
            var typ = tpb.GetType();
            
            /*If this next bit seems confusing:
             * Since 99 pct of the time the Immediate-mode GUI is presenting the same things
             * repeatedly, caching the dictionary for a type saves reallocating memory for
             * the dictionary and its lists every pass thru.
             */
            //it's possible that there's already an appropriate dictionary for this top-level type
            if (s_CachedInfoDicts.TryGetValue(typ, out var dict))
            {
                //if found, clear the internal lists.
                foreach (var list in dict.Values)
                {
                    list.m_FieldInfos.Clear();
                    list.m_MethodInfos.Clear();
                    list.m_PropertyInfos.Clear();
                }
            }
            else //create one for this Type and add to the cache
            {
                dict = new Dictionary<Type, PerClassInfo>();
                s_CachedInfoDicts.Add(typ, dict);
                
                //use the type information to setup the dictionary keys and empty perclassinfo instances.
                dict.Add(typ, new PerClassInfo()); //add the top-level class
                
                //Added .IsSubclassOf checks to ensure type scan doesn't go past TilePlusBase.
                //avoids displaying Tile, Tilebase, etc is the Tile type is itself TilePlusBase. 
                Type? thisType;
                var  lastType = typ;
                do
                {
                    thisType = lastType?.BaseType; //get base type of the last type (first pass this is the top-level class)
                    if (thisType != null)         //if not null add to the dictionary
                    {
                        if (thisType == s_TypeofTilePlusBase || thisType.IsSubclassOf(s_TypeofTilePlusBase))
                            dict.Add(thisType, new PerClassInfo());
                    }
                    lastType = thisType;                                        //persist current state
                } while (thisType != null && thisType != s_TypeofTilePlusBase); //test for completion
            }

            //get the properties info for this object. Filter out those properties
            //not in the s_NameSpaces HashSet
            var propInfos = typ.GetProperties()  //get properties
                               .Where(x => x.DeclaringType != null &&   //with a declaring type 
                                           s_Namespaces.Contains(x.DeclaringType.Namespace)).ToArray(); //and the type's namespace is valid
            //get all bool properties for use in CanShowxxx tests.
            var allBoolProperties = propInfos.Where(x => x.PropertyType == typeof(bool));
            var allStringProperties = propInfos.Where(x => x.PropertyType == typeof(string));

            IEnumerable<PropertyInfo> filteredProps;
            //if any properties, trim propInfos by only allowing those in the s_ValidAttrsForProperties HashSet.
            //for the brush inspector, only accept the "show as label brush inspector" attribute
            if (whichInspector == TppInspectorSpec.Selection)
            {
                filteredProps = (from pi in propInfos
                                 let attrTypes = pi.CustomAttributes.Select(attr => attr.AttributeType)
                                                   .Where(attrTyp => attrTyp == typeof(TptShowAsLabelSelectionInspectorAttribute))
                                 where attrTypes.Any()
                                 select pi);
                filteredProps = filteredProps.Where(pi => CanShowThisProperty(whichInspector, allBoolProperties, pi,tpb));
            }
            else
            {
                filteredProps = (from pi in propInfos
                                 let attrTypes = pi.CustomAttributes.Select(attr => attr.AttributeType)
                                                   .Where(attrTyp => attrTyp == typeof(TptShowAsLabelBrushInspectorAttribute))
                                 where attrTypes.Any()
                                 select pi);
            }
            
            
            //add to dictionary properties by declaring class
            foreach (var pi in filteredProps)
            {
                var propType = pi.DeclaringType;
                if (propType != null) // && dict.ContainsKey(propType)) //key should always exist due to prefill of dictionary keys
                    dict[propType].m_PropertyInfos.Add(pi);
            }

            //add string properties
            foreach (var pi in allStringProperties)
            {
                var propType = pi.DeclaringType;
                if (propType != null)
                    dict[propType].m_StringPropInfos.Add(pi);
            }
            
            //fields are only used in the Selection inspector.
            //Similarly, filter out fields that aren't in the proper
            //namespace or not in the list of valid attrs.
            if (whichInspector == TppInspectorSpec.Selection)
            {
                //get the fields info for this object. Note:public fields only
                var fieldInfos = typ.GetFields()
                                .Where(x => x.DeclaringType != null &&
                                            s_Namespaces.Contains(x.DeclaringType.Namespace));

                //trim the array of fieldinfos to include only those in the s_ValidAttrsForFields HashSet
                fieldInfos = (from fi in fieldInfos
                              let attrTypes = fi.CustomAttributes.Select(attr => attr.AttributeType)
                                                .Where(attrTyp => s_ValidAttrsForFields.Contains(attrTyp))
                              where attrTypes.Any()
                              select fi);
                fieldInfos = fieldInfos.Where(fi => CanShowThisField(allBoolProperties, fi,tpb));
                
                //and sort by declaring class
                foreach(var fi in fieldInfos)
                { 
                    var fieldType = fi.DeclaringType;
                    if (fieldType != null)
                        dict[fieldType].m_FieldInfos.Add(fi);
                }

                //methods are only used in the selection inspector
                //filter out methods not in proper namespace, or in the list of acceptable attrs
                var methodInfos = typ.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                                     .Where(x => x.DeclaringType != null && 
                                                 s_Namespaces.Contains(x.DeclaringType.Namespace));
                //trim array to include only those with attrs in s_ValidAttrForMethods
                methodInfos = (from mi in methodInfos
                               let attrTypes = mi.CustomAttributes.Select(attr => attr.AttributeType)
                                                 .Where(attrTyp => s_ValidAttrForMethods.Contains(attrTyp))
                               where attrTypes.Any()
                               select mi);
                
                methodInfos = methodInfos.Where(mi => CanShowThisMethod(allBoolProperties, mi,tpb));
                
                //sort by declaring class
                foreach(var mi in methodInfos)
                {
                    var methodType = mi.DeclaringType;
                    if (methodType != null) // && dict.ContainsKey(methodType))
                        dict[methodType].m_MethodInfos.Add(mi);
                }
            }

            //perhaps nothing to do
            if(dict.Count == 0)
                return S_FailureCompletionState; //note mode doesn't matter here
            

            //now process each declaring class' props,fields,methods.

            var firstLine = true;
            DrawUILine(2f,4f);

            //these should only be SET by a return value from the display generator. NEVER cleared. 
            //That way, if an earlier tile class (when the tile class is a subclass of something else that shows GUI)
            //any of these values that gets SET after the display for a subclass is generated never gets CLEARED
            //by the display generation of a base class.
            var foundTaggedTile    = false;
            var inhibitAutoSave    = false;
            var refreshTile        = false;
            var updateTileInstance = false;
                
            s_CurrentClassnames.Clear();
            s_ModifiedFieldNames.Clear();

            GUI.skin = TpSkin;
            
            //loop thru the dictionary 
            foreach ((var key, var perClassInfo) in dict)
            {
                var fInfos = perClassInfo.m_FieldInfos.ToArray(); 
                var pInfos = perClassInfo.m_PropertyInfos.ToArray();
                var pStrInfos = perClassInfo.m_StringPropInfos.ToArray();
                var mInfos = perClassInfo.m_MethodInfos.ToArray();

                //if brush inspector but no properties then skip this 
                if(whichInspector == TppInspectorSpec.Brush && pInfos.Length == 0 )
                    continue;
                
                if (firstLine) 
                    firstLine = false;
                else
                    DrawUILine();

                //use a foldout if classheaders are on for the selection inspector
                if (s_ClassHeaders && whichInspector == TppInspectorSpec.Selection)
                {
                    var classNameSplit       = key.ToString().Split(s_DelimForDot);
                    var className = classNameSplit[^1];
                    s_CurrentClassnames.Add(className);
                    var foldOpen = s_Config.GetFoldoutPref(className);
                    var fold     = EditorGUILayout.BeginFoldoutHeaderGroup(foldOpen, className);
                    if (fold != foldOpen)
                        s_Config.SetFoldoutPref(className, fold);

                    if (fold) //show this section
                    {
                        //format/display/etc
                        var result = GuiForTilePlusBase(fInfos,
                                                        pInfos,
                                                        pStrInfos,
                                                        mInfos,
                                                        tpb,
                                                        whichInspector,
                                                        activeScene,
                                                        noEdit);
                        if (result.m_UpdateTileInstance && !string.IsNullOrEmpty(result.m_FieldNames[0]))
                        {
                            updateTileInstance = true;
                            s_ModifiedFieldNames.Add(result.m_FieldNames[0]); //note there's never more than one
                        }

                        if (result.m_FoundTaggedTile)
                            foundTaggedTile = true;
                        if (result.m_InhibitAutoSave)
                            inhibitAutoSave = true;
                        if (result.m_RefreshTile)
                            refreshTile = true;
                    }

                    EditorGUILayout.EndFoldoutHeaderGroup();
                }
                else //no class headers, just format and display
                {
                    var result = GuiForTilePlusBase(fInfos,
                                                    pInfos,
                                                    pStrInfos,
                                                    mInfos,
                                                    tpb,
                                                    whichInspector,
                                                    activeScene,
                                                    noEdit);
                    if (result.m_UpdateTileInstance && !string.IsNullOrEmpty(result.m_FieldNames[0]))
                    {
                        updateTileInstance = true;
                        s_ModifiedFieldNames.Add(result.m_FieldNames[0]); //note there's never more than one
                    }

                    if (result.m_FoundTaggedTile)
                        foundTaggedTile = true;
                    if (result.m_InhibitAutoSave)
                        inhibitAutoSave = true;
                    if (result.m_RefreshTile)
                        refreshTile = true;
                }
            }

            GUILayout.Space(2);
            DrawUILine(2f,1f);
            GUI.skin = null;
            return updateTileInstance
                       ? new CompletionState(foundTaggedTile, inhibitAutoSave, refreshTile, true, s_ModifiedFieldNames.ToArray())
                       : new CompletionState(foundTaggedTile, inhibitAutoSave, refreshTile, false);
            

        }

        #endregion

        
        #region displayGenerator

        private static readonly string [] s_ModifiedFieldName = new[] { string.Empty };
        
        /*This section does the actual formatting and display of fields, properties, and methods.
         * Reflection is used to "peek and poke" the data as needed.         */
        private static CompletionState GuiForTilePlusBase(
                FieldInfo[]      fieldInfos,
                PropertyInfo[]  propInfos,
                PropertyInfo[]   allStringProps,
                MethodInfo[]     methodInfos,
                TilePlusBase     tpbInstance,
                TppInspectorSpec whichInspector,
                Scene            activeScene,
                bool             noEditing  
            )
        {
            //the return values
            var foundTaggedTile = false;  
            var doRefresh       = false;
            var updateTpLib     = false;
            var inhibitAutoSave = false;
            s_ModifiedFieldName[0] = string.Empty;


            //process properties first. 
            var numProps        = propInfos.Length;

            //note: Tooltip attribute isn't available for properties but you can add one with these attributes.
            for (var index = 0; index < numProps; index++)
            {
                //handle properties
                var propInfo = propInfos[index];
                
                //if (propInfo == null) break;
                bool      useHelpBox;
                bool      splitCamelCase;
                string    toolTip;
                SpaceMode spaceMode;
                
                //if selection inspector, look for show-label attr
                if (whichInspector == TppInspectorSpec.Selection)
                {
                    var selAttr = (TptShowAsLabelSelectionInspectorAttribute) Attribute.GetCustomAttribute(propInfo,
                        typeof(TptShowAsLabelSelectionInspectorAttribute),
                        true);
                    
                    //get attr's values
                    foundTaggedTile = true; //note that doRefresh is not set true as this is only a label.
                    useHelpBox      = selAttr.m_UseHelpBox;
                    splitCamelCase  = selAttr.m_SplitCamelCaseNames;
                    toolTip         = selAttr.m_Tooltip;
                    spaceMode       = selAttr.m_SpaceMode;
                }
                else //brush inspector, look for its show-label attr
                {
                    var brushAttr = (TptShowAsLabelBrushInspectorAttribute) Attribute.GetCustomAttribute(propInfo,
                        typeof(TptShowAsLabelBrushInspectorAttribute),
                        true);
                    
                    if (brushAttr == null)
                        continue;
                    
                    //get attr's values
                    foundTaggedTile = true;
                    useHelpBox      = brushAttr.m_UseHelpBox;
                    splitCamelCase  = brushAttr.m_SplitCamelCaseNames;
                    toolTip         = brushAttr.m_Tooltip;
                    spaceMode       = SpaceMode.None;
                }

                //display
                BeforeItem(spaceMode);
                
                var propName = ComposeName(propInfo.Name, splitCamelCase); //compose the prop name string
                var propValue    = propInfo.GetValue(tpbInstance); //value of the property
                //display the property as a string, or show that it's null
                var propString        = propValue != null ? propValue.ToString() : "(null or empty)";
                if (useHelpBox) //if attr specifies use a help box
                {
                    var guiContent = new GUIContent(propName + ": " + string.Join(",", propString));
                    if (toolTip != string.Empty)
                        guiContent.tooltip = toolTip;
                    EditorGUILayout.HelpBox(guiContent);
                }
                else //use a label field
                {
                    if (toolTip == string.Empty)
                        EditorGUILayout.LabelField(propName, propInfo.GetValue(tpbInstance).ToString());
                    else
                    {
                        var guiContent1 = new GUIContent(propName) {tooltip = toolTip};
                        var guiContent2 = new GUIContent(propString);
                        EditorGUILayout.LabelField(guiContent1, guiContent2, GUILayout.ExpandWidth(true));
                    }
                }

                AfterItem(spaceMode);
            }
            
            
            //handle methods
            
            var numMethods        = methodInfos.Length;
            var itp               = tpbInstance as ITilePlus;
            /*here we check for the proper inspector (which shouldn't be an issue after the prefiltering
              done in the caller), whether there are any methods, whether the tpbInstance (obj being inspected)
              is unlocked and not able to simulate, or if it is able to simulate, whether
              simulation is active.
            */
            if (!noEditing && whichInspector == TppInspectorSpec.Selection && numMethods != 0) //proper inspector AND stuff to process
            {
                for (var index = 0; index < numMethods; index++)
                {
                    var methodInfo = methodInfos[index];
                    //if (methodInfo == null) break;

                    //get the attr for a tile's custom-gui code, if any
                    var customGuiAttr = methodInfo.GetCustomAttribute<TptShowCustomGUIAttribute>();
                    if (customGuiAttr != null)
                    {
                        
                        foundTaggedTile = true; 
                        //NB 1.5
                        //if the custom-gui method has a return type other than CustomGuiReturn then the
                        //return value is ignored.
                        //if the refresh field in the return is true, then the scene is saved if
                        //autosave is active (config setting).
                        var output = methodInfo.Invoke(tpbInstance, new object[] { TpSkin, s_ButtonSize, noEditing });
                        if (output is CustomGuiReturn guiReturn)
                        {
                            if(guiReturn.m_Refresh)
                                doRefresh = true;
                            if (guiReturn.m_ModifiedField != string.Empty)
                            {
                                s_ModifiedFieldName[0] = guiReturn.m_ModifiedField;
                                updateTpLib       = true;
                            }

                            if (guiReturn.m_DelaySceneSave)
                                inhibitAutoSave = true;
                        }
                        else if(output != null)
                        {
                            EditorGUILayout.HelpBox($"On {methodInfo.Name}, did not return a CustomGuiReturn instance, so the effect of Refresh, Modified Field, and Scene Save are ignored.", MessageType.Error, true);
                            continue;
                        }

                        GUI.skin = TpSkin; //fixd 2.02 in case gui method changed the skin from TpSkin.
                       
                    }

                    if (tpbInstance.IsLocked || itp is { CanSimulate: true, IsSimulating: true }) //if locked tile OR can Simulate and is currently simulating, skip this
                        continue;                    
                    //process show-method-as-button attr
                    var mAttr = methodInfo.GetCustomAttribute<TptShowMethodAsButtonAttribute>();
                    
                    if (mAttr == null)
                        continue;

                    foundTaggedTile = true;
                     

                    //get params for the method
                    var methodName   = methodInfo.Name; //its name
                    var methodParams = methodInfo.GetParameters(); //the param list (which should be empty!)
                    //error if method has params
                    if (methodParams.Length > 0)
                    {
                        EditorGUILayout.HelpBox($"On {methodName}, ShowMethodAsButton can only be applied to methods with no parameters.", MessageType.Error, true);
                        continue;
                    }
                    
                    var spaceMode = mAttr.m_SpaceMode;
                    BeforeItem(spaceMode);
                    
                    //optional message for this method button is shown as a plain helpbox
                    var message = mAttr.m_Message;
                    if(!string.IsNullOrWhiteSpace(message))
                        EditorGUILayout.HelpBox(message,MessageType.None);

                    if (!s_IsPlaying)
                    {
                        //optional note for this method button
                        var methodNoteAttr = methodInfo.GetCustomAttribute<TptNoteAttribute>();
                        if (methodNoteAttr != null)
                        {
                            (var note, var isWarning) = GetNote(tpbInstance, allStringProps, methodNoteAttr);
                            EditorGUILayout.HelpBox(note, isWarning ? MessageType.Warning : MessageType.None, true);
                        }
                    }
                    
                    using (new EditorGUILayout.HorizontalScope("box", GUILayout.ExpandWidth(false)))
                    {
                        using (new EditorGUIUtility.IconSizeScope(s_ButtonSize))
                        {
                            if (GUILayout.Button(s_ArrowRightIcon)) //, GUILayout.MaxWidth(methodButtonSize), GUILayout.MaxHeight(methodButtonSize)))
                                methodInfo.Invoke(tpbInstance, null);
                        }
                        EditorGUILayout.LabelField($"Run '{methodName}'", GUILayout.ExpandWidth(true));
                    }
                    AfterItem(spaceMode);
                }
            }
            

            /*handle fields. This is quite a bit more complex since
            there are many different data types being handled and each is
            different.
            */
            var numFields           = fieldInfos.Length;

            for (var index = 0; index < numFields; index++)
            {
                var fieldInfo = fieldInfos[index];

                //if (fieldInfo == null) break;
                using (HashSetPool<Type>.Get(out var typeHash))
                {
                    //get a hash of the custom attributes for this object
                    //note: hash initialized (via UnionWith) with an enumerable of attribute Types
                    //the LINQ here does this;
                    // 1. filter out attributes that we don't use
                    // 2. select the attribute type which is added (via UnionWith) to the typeHash HashSet<Type>
                    // 3. returns a an enumerable of attribute types which are valid for this code, ie part of s_ValidAttrsForFields. 
                    typeHash.UnionWith(fieldInfo.CustomAttributes
                                                .Where(attr => s_ValidAttrsForFields.Contains(attr.AttributeType))
                                                .Select(attr => attr.AttributeType));

                    //get a tooltip if one exists
                    var toolTipString = string.Empty;
                    if (typeHash.Contains(typeof(TooltipAttribute)))
                    {
                        var toolTip = fieldInfo.GetCustomAttribute<TooltipAttribute>();
                        if (toolTip != null)
                            toolTipString = toolTip.tooltip;
                    }

                    //compose the name string
                    var nam = ComposeName(fieldInfo.Name, true);
                    //get the object for this field
                    var val       = fieldInfo.GetValue(tpbInstance); //object which has the attribute
                    var valIsNull = val == null;                     //note that it's null (or not).

                    //optional note for this field
                    if (!s_IsPlaying)
                    {
                        var fieldNoteAttr = fieldInfo.GetCustomAttribute<TptNoteAttribute>();
                        if (fieldNoteAttr != null)
                        {
                            (var note, var isWarning) = GetNote(tpbInstance, allStringProps, fieldNoteAttr);
                            EditorGUILayout.HelpBox(note, isWarning ? MessageType.Warning : MessageType.None, true);
                        }
                    }
                    
                    
                    //for types we don't handle, tag it as unsupported
                    var editableType = s_EditableTypes.Contains(fieldInfo.FieldType);

                    /*an unsupported field is a selection inspector field which has the show-field
                     attr but isn't editable. Later on, if this field is a UnityEngine.Object type
                     special handling is done to present this field. IE if you have a field that's
                     for an asset type but it isn't an editable field (bool, int, float, etc) then
                     this is what happens. 
                     */
                    var unsupportedField = whichInspector == TppInspectorSpec.Selection       &&
                                           typeHash.Contains(typeof(TptShowFieldAttribute)) &&
                                           !editableType;

                    //process "show as label" type attrs for either inspector
                    if (unsupportedField                                                       || //sel insp but not supported type
                        typeHash.Contains(typeof(TptShowAsLabelSelectionInspectorAttribute)) ||
                        typeHash.Contains(typeof(TptShowAsLabelBrushInspectorAttribute)))
                    {
                        //init some state variables that may get altered as attrs are evaluated
                        var useHelpBox     = false; //use an helpbox instead of a label
                        var splitCamelCase = false; //split camel case
                        var found          = false; //set true if there's something to display
                        var spaceMode      = SpaceMode.None;

                        //set up for each situation
                        if (whichInspector == TppInspectorSpec.Selection)
                        {
                            if (unsupportedField)
                            {
                                var selAttr = (TptShowFieldAttribute)Attribute.GetCustomAttribute(fieldInfo,
                                    typeof(TptShowFieldAttribute),
                                    true);
                                if (selAttr != null) 
                                {
                                    found          = true;
                                    useHelpBox     = false;
                                    splitCamelCase = true;
                                    spaceMode      = selAttr.m_SpaceMode;
                                }
                            }
                            else
                            {
                                var selAttr = (TptShowAsLabelSelectionInspectorAttribute)Attribute.GetCustomAttribute(fieldInfo,
                                    typeof(TptShowAsLabelSelectionInspectorAttribute),
                                    true);
                                if (selAttr != null)
                                {
                                    found          = true;
                                    useHelpBox     = selAttr.m_UseHelpBox;
                                    splitCamelCase = selAttr.m_SplitCamelCaseNames;
                                    spaceMode      = selAttr.m_SpaceMode;
                                }
                            }
                        }
                        else //brush inspector
                        {
                            var brushAttr = (TptShowAsLabelBrushInspectorAttribute)Attribute.GetCustomAttribute(fieldInfo,
                                typeof(TptShowAsLabelBrushInspectorAttribute),
                                true);
                            if (brushAttr != null)
                            {
                                foundTaggedTile = true;
                                useHelpBox      = brushAttr.m_UseHelpBox;
                                splitCamelCase  = brushAttr.m_SplitCamelCaseNames;
                                spaceMode       = SpaceMode.None;
                                found           = true;
                            }
                        }

                        //now display the info
                        if (s_IsPlaying)
                            spaceMode = SpaceMode.None;
                        if (found) //if there's something to display
                        {
                            foundTaggedTile = true; //return value to caller: yes there's something to display
                            if (useHelpBox)
                                splitCamelCase = false; //this is done in the attributes' constructor though...

                            //compose the name
                            var fieldName = ComposeName(fieldInfo.Name, splitCamelCase);

                            BeforeItem(spaceMode);


                            if (useHelpBox)
                                EditorGUILayout.HelpBox(new GUIContent(fieldName + ": " +
                                                                       string.Join(",", valIsNull ? "(null)" : val?.ToString())));
                            else
                            {
                                if (unsupportedField)
                                {
                                    if (valIsNull) //if value is null display a warning
                                    {
                                        var fieldType   = fieldInfo.FieldType.ToString();
                                        var niceType    = fieldType.Split('.');
                                        var displayName = niceType.Length == 0 ? "???" : niceType[^1];
                                        EditorGUILayout.HelpBox(
                                            $"[ERROR: field of type \"{displayName}\" named \"{fieldName}\" is null]",
                                            MessageType.Error);
                                    }

                                    else //show a button for opening inspector
                                    {
                                        var objNameString = val!.ToString().Split(s_DelimForParens)[0];
                                        using (new EditorGUILayout.HorizontalScope("box", GUILayout.ExpandWidth(false)))
                                        {
                                            using (new EditorGUIUtility.IconSizeScope(s_ButtonSize))
                                            {
                                                if (!s_IsPlaying && val is UnityEngine.Object unityObj)
                                                {
                                                    if (GUILayout.Button(s_OpenInInspectorButtonGUIContent))
                                                        TpEditorUtilities.OpenInspectorDelayed(unityObj, tpbInstance);
                                                }

                                                var unsupportedGuiContent = new GUIContent(fieldName, toolTipString);
                                                var objGuiContent         = new GUIContent(objNameString);
                                                EditorGUILayout.LabelField(unsupportedGuiContent, objGuiContent, GUILayout.ExpandWidth(true));
                                            }
                                        }
                                    }
                                }
                                else
                                    EditorGUILayout.LabelField(fieldName, valIsNull ? "(null)" : val!.ToString());
                            }
                            AfterItem(spaceMode);
                        }
                    }

                    //process TppShowField attributes for valid field types
                    if (whichInspector != TppInspectorSpec.Selection)
                        continue;

                    //create the GuiContent
                    var guiContent = toolTipString != string.Empty
                        ? new GUIContent(nam, toolTipString)
                        : new GUIContent(nam);

                    //might have to force showing as label for certain conditions
                    var showAsLabel = s_IsPlaying || noEditing || valIsNull; //|| tpbInstance.IsLocked;

                    //process show as field attr
                    if (typeHash.Contains(typeof(TptShowFieldAttribute)))
                    {
                        var fieldAttr = (TptShowFieldAttribute)Attribute.GetCustomAttribute(fieldInfo,
                            typeof(TptShowFieldAttribute),
                            true);

                        if (fieldAttr.m_ForceShowField && !tpbInstance.IsLocked  && !noEditing) 
                            showAsLabel = false;
                        
                        var spaceMode = fieldAttr.m_SpaceMode;

                        BeforeItem(spaceMode);

                        if (valIsNull) //deal with null values
                        {
                            guiContent.text = $"{nam}: (null)";
                            EditorGUILayout.HelpBox(guiContent);
                            AfterItem(spaceMode);
                            continue;
                        }

                        //now handle possible field types
                        if (fieldInfo.FieldType == typeof(bool))
                        {
                            foundTaggedTile = true; //yes we are displaying something
                            var param = (bool)val!; //cast the value
                            if (showAsLabel)        //show this as a helpbox
                            {
                                guiContent.text = $"{nam}: {param.ToString()}";
                                EditorGUILayout.HelpBox(guiContent);
                                AfterItem(spaceMode);
                                continue;
                            }

                            //otherwise, use a toggle to display the value
                            var result = EditorGUILayout.Toggle(guiContent, param);
                            AfterItem(spaceMode);
                            if (result == param) //if no change then continue the loop
                                continue;
                            if (fieldAttr.m_UpdateTpLib)
                            {
                                updateTpLib       = true;
                                s_ModifiedFieldName[0] = fieldInfo.Name;
                            }

                            doRefresh = true;                        //if was a change then mark for scene save
                            fieldInfo.SetValue(tpbInstance, result); //and poke the value back into the target field
                        }
                        //process is the same for the rest of these with exceptions as noticed
                        else if (fieldInfo.FieldType == typeof(int))
                        {
                            foundTaggedTile = true;
                            var param = (int)val!;
                            if (showAsLabel)
                            {
                                guiContent.text = $"{nam}: {param.ToString()}";
                                EditorGUILayout.HelpBox(guiContent);
                                AfterItem(spaceMode);
                                continue;
                            }

                            //here sliders are an option
                            int result;
                            //sliders are used unless inhibited in the configuration settings. Both min and max ranges must be nonzero
                            var useRange = s_AllowRangeSliders && (fieldAttr.m_RangeMax != 0f || fieldAttr.m_RangeMin != 0f);
                            if (useRange)
                            {
                                var min = (int)fieldAttr.m_RangeMin;
                                var max = (int)fieldAttr.m_RangeMax;
                                result = EditorGUILayout.IntSlider(guiContent, param, min, max);
                            }
                            else //use a normal field if not using sliders
                                result = EditorGUILayout.DelayedIntField(guiContent, param);
                            AfterItem(spaceMode);
                            if (result == param)
                                continue;
                            if (useRange)
                                inhibitAutoSave = true;
                            if (fieldAttr.m_UpdateTpLib)
                            {
                                updateTpLib       = true;
                                s_ModifiedFieldName[0] = fieldInfo.Name;
                            }

                            doRefresh = true;
                            fieldInfo.SetValue(tpbInstance, result);
                        }
                        else if (fieldInfo.FieldType == typeof(float))
                        {
                            foundTaggedTile = true;
                            var param = (float)val!;
                            if (showAsLabel)
                            {
                                guiContent.text = $"{nam}: {param.ToString(CultureInfo.InvariantCulture)}";
                                EditorGUILayout.HelpBox(guiContent);
                                AfterItem(spaceMode);
                                continue;
                            }
                            //floats can also optionally use range sliders.
                            var useRange = s_AllowRangeSliders && (fieldAttr.m_RangeMax != 0f || fieldAttr.m_RangeMin != 0f);
                            var result = useRange
                                ? EditorGUILayout.Slider(guiContent,
                                    param,
                                    fieldAttr.m_RangeMin,
                                    fieldAttr.m_RangeMax)
                                : EditorGUILayout.DelayedFloatField(guiContent,
                                    param);

                            AfterItem(spaceMode);
                            if (Mathf.Approximately(result, param))
                                continue;
                            if (useRange)
                                inhibitAutoSave = true;
                            if (fieldAttr.m_UpdateTpLib)
                            {
                                updateTpLib       = true;
                                s_ModifiedFieldName[0] = fieldInfo.Name;
                            }

                            doRefresh = true;
                            fieldInfo.SetValue(tpbInstance, result);

                        }
                        else if (fieldInfo.FieldType == typeof(string))
                        {
                            foundTaggedTile = true;
                            var param = (string)val!;
                            if (showAsLabel)
                            {
                                guiContent.text = $"{nam}: {param}";
                                EditorGUILayout.HelpBox(guiContent);
                                AfterItem(spaceMode);
                                continue;
                            }
                            var result = EditorGUILayout.DelayedTextField(guiContent, param);
                            AfterItem(spaceMode);
                            if (result == param)
                                continue;
                            if (fieldAttr.m_UpdateTpLib)
                            {
                                updateTpLib       = true;
                                s_ModifiedFieldName[0] = fieldInfo.Name;
                            }

                            doRefresh = true;
                            fieldInfo.SetValue(tpbInstance, result);
                        }
                        else if (fieldInfo.FieldType == typeof(Vector3))
                        {
                            foundTaggedTile = true;
                            var param = (Vector3)val!;
                            if (showAsLabel)
                            {
                                guiContent.text = $"{nam}: {param.ToString()}";
                                EditorGUILayout.HelpBox(guiContent);
                                AfterItem(spaceMode);
                                continue;
                            }

                            var result = EditorGUILayout.Vector3Field(guiContent, param);
                            AfterItem(spaceMode);
                            if (result == param)
                                continue;
                            if (fieldAttr.m_UpdateTpLib)
                            {
                                updateTpLib       = true;
                                s_ModifiedFieldName[0] = fieldInfo.Name;
                            }

                            doRefresh = true;
                            fieldInfo.SetValue(tpbInstance, result);
                        }
                        else if (fieldInfo.FieldType == typeof(Vector3Int))
                        {
                            foundTaggedTile = true;
                            var param = (Vector3Int)val!;
                            if (showAsLabel)
                            {
                                guiContent.text = $"{nam}: {param.ToString()}";
                                EditorGUILayout.HelpBox(guiContent);
                                AfterItem(spaceMode);
                                continue;
                            }

                            var result = EditorGUILayout.Vector3IntField(guiContent, param);
                            AfterItem(spaceMode);
                            if (result == param)
                                continue;
                            if (fieldAttr.m_UpdateTpLib)
                            {
                                updateTpLib       = true;
                                s_ModifiedFieldName[0] = fieldInfo.Name;
                            }
                            doRefresh = true;
                            fieldInfo.SetValue(tpbInstance, result);
                        }
                        else if (fieldInfo.FieldType == typeof(Vector2))
                        {
                            foundTaggedTile = true;
                            var param = (Vector2)val!;
                            if (showAsLabel)
                            {
                                guiContent.text = $"{nam}: {param.ToString()}";
                                EditorGUILayout.HelpBox(guiContent);
                                AfterItem(spaceMode);
                                continue;
                            }

                            var result = EditorGUILayout.Vector2Field(guiContent, param);
                            AfterItem(spaceMode);
                            if (result == param)
                                continue;
                            if (fieldAttr.m_UpdateTpLib)
                            {
                                updateTpLib       = true;
                                s_ModifiedFieldName[0] = fieldInfo.Name;
                            }
                            doRefresh = true;
                            fieldInfo.SetValue(tpbInstance, result);
                        }
                        else if (fieldInfo.FieldType == typeof(Vector2Int))
                        {
                            foundTaggedTile = true;
                            var param = (Vector2Int)val!;
                            if (showAsLabel)
                            {
                                guiContent.text = $"{nam}: {param.ToString()}";
                                EditorGUILayout.HelpBox(guiContent);
                                AfterItem(spaceMode);
                                continue;
                            }

                            var result = EditorGUILayout.Vector2IntField(guiContent, param);
                            AfterItem(spaceMode);
                            if (result == param)
                                continue;
                            doRefresh = true;
                            if (fieldAttr.m_UpdateTpLib)
                            {
                                updateTpLib       = true;
                                s_ModifiedFieldName[0] = fieldInfo.Name;
                            }
                            fieldInfo.SetValue(tpbInstance, result);
                        }

                        else if (fieldInfo.FieldType == typeof(Color))
                        {
                            foundTaggedTile = true;
                            var param = (Color)val!;

                            if (showAsLabel)
                            {
                                guiContent.text = $"{nam}: {param.ToString()}";
                                EditorGUILayout.HelpBox(guiContent);
                                AfterItem(spaceMode);
                                continue;
                            }

                            var result = EditorGUILayout.ColorField(guiContent, param);
                            AfterItem(spaceMode);
                            if (result == param)
                                continue;
                            doRefresh = true;
                            if (fieldAttr.m_UpdateTpLib)
                            {
                                updateTpLib       = true;
                                s_ModifiedFieldName[0] = fieldInfo.Name;
                            }
                            fieldInfo.SetValue(tpbInstance, result);

                        }
                    }

                    //object fields can  have an optional inspector button
                    if (typeHash.Contains(typeof(TptShowObjectFieldAttribute)))
                    {
                        var objAttr = (TptShowObjectFieldAttribute)Attribute.GetCustomAttribute(fieldInfo,
                            typeof(TptShowObjectFieldAttribute),
                            true);

                        var spaceMode = objAttr.m_SpaceMode;
                        BeforeItem(spaceMode);

                        foundTaggedTile = true;

                        //showAsLabel needs a different approach here since this field can validly be null.
                        var useLabel = s_IsPlaying || noEditing ||  tpbInstance.IsLocked;

                        var param = (UnityEngine.Object)val!;
                        if (useLabel)
                        {
                            guiContent.text = $"{nam}: {(valIsNull ? "(null)" : param.ToString())}";
                            EditorGUILayout.HelpBox(guiContent);
                            AfterItem(spaceMode);
                            continue;
                        }

                        //for a non-null param, the inspector button is an option
                        if (objAttr.m_InspectorButton && param != null)
                        {
                            //var objNameString = val.ToString().Split(s_DelimForParens)[0];
                            using (new EditorGUILayout.HorizontalScope("box", GUILayout.ExpandWidth(false)))
                            {
                                using (new EditorGUIUtility.IconSizeScope(s_ButtonSize))
                                {

                                    if (GUILayout.Button(s_OpenInInspectorButtonGUIContent))
                                        TpEditorUtilities.OpenInspectorDelayed(param, tpbInstance);

                                    GUI.skin = null;

                                    var objType = objAttr.m_DesiredType;
                                    var result  = EditorGUILayout.ObjectField(guiContent, param, objType, objAttr.m_AllowSceneObjects);
                                    GUI.skin = TpSkin;
                                    if (result == param)
                                        continue;
                                    if (objAttr.m_UpdateTpLib)
                                    {
                                        updateTpLib       = true;
                                        s_ModifiedFieldName[0] = fieldInfo.Name;
                                    }
                                    doRefresh = true;
                                    fieldInfo.SetValue(tpbInstance, result);
                                }
                                AfterItem(spaceMode);
                            }
                        }
                        else //a plain object field
                        {
                            GUI.skin = null;

                            var objType = objAttr.m_DesiredType;
                            var result  = EditorGUILayout.ObjectField(guiContent, param, objType, objAttr.m_AllowSceneObjects);
                            GUI.skin = TpSkin;
                            AfterItem(spaceMode);
                            if (result == param)
                                continue;

                            if (objAttr.m_UpdateTpLib)
                            {
                                updateTpLib       = true;
                                s_ModifiedFieldName[0] = fieldInfo.Name;
                            }
                            doRefresh = true;
                            fieldInfo.SetValue(tpbInstance, result);
                        }
                    }

                    //Show enums. 
                    // ReSharper disable once InvertIf
                    if (typeHash.Contains(typeof(TptShowEnumAttribute)))
                    {
                        var enumAttr = (TptShowEnumAttribute)Attribute.GetCustomAttribute(fieldInfo,
                            typeof(TptShowEnumAttribute),
                            true);

                        var spaceMode = enumAttr.m_SpaceMode;
                        BeforeItem(spaceMode);

                        foundTaggedTile = true;
                        var param = (Enum)val!;
                        if (showAsLabel) //show it as a label
                        {
                            guiContent.text = $"{nam}: {(valIsNull ? "(null)" : param.ToString())}";
                            EditorGUILayout.HelpBox(guiContent);
                            AfterItem(spaceMode);
                            continue;
                        }

                        //or use an enum popup
                        var result = EditorGUILayout.EnumPopup(guiContent, param);
                        AfterItem(spaceMode);
                        if (result.Equals(param))
                            continue;

                        if (enumAttr.m_UpdateTpLib)
                        {
                            updateTpLib       = true;
                            s_ModifiedFieldName[0] = fieldInfo.Name;
                        }
                        doRefresh = true;
                        fieldInfo.SetValue(tpbInstance, result);
                    }
                }
            }

            //mark scene dirty if a change was made but NOT if playing or if noEditing is true (noEditing is true when obj is within a prefab)
            if (doRefresh && !s_IsPlaying && !noEditing)
                EditorSceneManager.MarkSceneDirty(activeScene);
            return new CompletionState(foundTaggedTile, inhibitAutoSave, doRefresh, updateTpLib, s_ModifiedFieldName);

        }
        #endregion

        #region utils
        private static bool CanShowThisProperty(TppInspectorSpec whichInspector, IEnumerable<PropertyInfo> allBoolProperties, PropertyInfo pi, TilePlusBase target )
        {
            var attrs    = Attribute.GetCustomAttributes(pi, typeof(TptAttributeBaseAttribute), true);
            //note that GetCustomAttributes returns an empty array when none are found.
            var numAttrs = attrs.Length;
            switch (numAttrs)
            {
                case 0:
                    return true; //??
                case 1:
                    return attrs[0] != null && CanShowBase(allBoolProperties, (TptAttributeBaseAttribute)attrs[0], target);
                default:
                {
                    for (var i = 0; i < numAttrs; i++)
                    {
                        var attr = (TptAttributeBaseAttribute)attrs[i];
                        if(attr == null)
                            continue;
                        if (whichInspector == TppInspectorSpec.Brush && attr.GetType() == typeof(TptShowAsLabelBrushInspectorAttribute))
                            return true;
                        return CanShowBase(allBoolProperties, attr, target);
                    }

                    break;
                }
            }

            return false;
        }

        private static bool CanShowThisField(IEnumerable<PropertyInfo> allBoolProperties, FieldInfo fi, TilePlusBase target)
        {
            var attr = (TptAttributeBaseAttribute) Attribute.GetCustomAttribute(fi,
                typeof(TptAttributeBaseAttribute),
                true);
            
            return attr != null && CanShowBase(allBoolProperties, attr, target);
        }

        private static bool CanShowThisMethod(IEnumerable<PropertyInfo> allBoolProperties, MethodInfo mi, TilePlusBase target)
        {
            var attr = (TptAttributeBaseAttribute) Attribute.GetCustomAttribute(mi,
                typeof(TptAttributeBaseAttribute),
                true);
            return attr != null && CanShowBase(allBoolProperties, attr, target);
        }


        private static bool CanShowBase(IEnumerable<PropertyInfo> allBoolProperties,  TptAttributeBaseAttribute baseAttr, TilePlusBase target )
        {
            /*if (baseAttr == null) //should be checked already but let's be sure
                return false;*/
            
            var showMode = baseAttr.m_ShowMode;
            
            switch (showMode)
            {
                case ShowMode.Always:
                    return true;
                case ShowMode.InPlay:
                    return s_IsPlaying;
                case ShowMode.NotInPlay:
                    return !s_IsPlaying;
                case ShowMode.Property:
                {
                    var propName = baseAttr.m_VisibilityProperty.Trim();
                    var invert   = false;
                    if(string.IsNullOrWhiteSpace(propName))
                    {
                        EditorGUILayout.HelpBox($"Empty name for visibility property in Type: {target.GetType()}, attribute={baseAttr}!",MessageType.Warning);
                        return false;
                    }
                    if (propName.Length > 1 && propName[0] == '!')
                    {
                        invert   = true;
                        propName = propName.Substring(1);
                    }

                    var p = allBoolProperties.FirstOrDefault(x => x.Name == propName);
                    
                    if(p == null)
                    {
                        EditorGUILayout.HelpBox($"Cannot find property named ({propName}) in Type: {target.GetType()}, attribute={baseAttr}. Is its access mode 'public'?",MessageType.Warning);
                        return true;
                    }
                    else if(invert)
                        return (!(bool) p.GetValue(target));
                    else
                        return ((bool) p.GetValue(target));
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }


        private static void BeforeItem(SpaceMode spaceMode)
        {
            switch (spaceMode)
            {
                case SpaceMode.LineBefore or SpaceMode.LineBoth:
                    DrawUILine();
                    break;
                case SpaceMode.SpaceBoth or SpaceMode.SpaceBefore:
                    GUILayout.Space(SpacePixels);
                    break;
            }
        }
        
        private static void AfterItem(SpaceMode spaceMode)
        {
            switch (spaceMode)
            {
                case SpaceMode.LineAfter or SpaceMode.LineBoth:
                    DrawUILine();
                    break;
                case SpaceMode.SpaceBoth or SpaceMode.SpaceAfter:
                    GUILayout.Space(SpacePixels);
                    break;
            }
        }

        private static string SplitCamelCase(string input)
        {
            return Regex.Replace(input, "([A-Z])", " $1", RegexOptions.Compiled).Trim();
        }


        //HT to alexanderameye https://forum.unity.com/threads/horizontal-line-in-editor-window.520812/
        /// <summary>
        /// Draw a GUI line 
        /// </summary>
        /// <param name="padding">extra space</param>
        /// <param name="height">height (think width) of the line</param>
        // ReSharper disable once MemberCanBePrivate.Global
        public static void DrawUILine(float padding = 2, float height = 0)
        {
            var thickness = height > 0 ? height : LineWidth;
            var r         = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
            r.height =  thickness;
            r.y      += padding / 2;
            r.x      -= 2;
            r.width  += 6;
            EditorGUI.DrawRect(r, Color.gray); 
        }
        
        /// <summary>
        /// Change use of Class Headers in the display
        /// </summary>
        /// <param name="on">set on or off</param>
        /// 
        public static void ChangeClassHeaders(bool on)
        {
            s_ClassHeaders = on;
        }

        //format the property or field name, optional split for camelcase.
        private static string ComposeName(string s, bool split)
        {
            var len = s.Length;
            if (s != string.Empty && len > 2 && s[0] == 'm' && s[1] == '_')
                s = s[2..];
            return !split ? s : SplitCamelCase(s);
        }

        private static (string note, bool isWarning) GetNote(TilePlusBase target, IEnumerable<PropertyInfo> allStringProps, TptNoteAttribute noteAttr)
        {
            if (/*noteAttr == null ||*/ s_IsPlaying)
                return (string.Empty, false);

            if (noteAttr.m_UseProperty)
            {
                var propName = noteAttr.m_NoteOrProperty;
                var p = allStringProps.FirstOrDefault(x => x.Name == propName);

                return p == null
                    ? ($"Cannot find property named ({propName}) : attribute=TptNoteAttribute : type={target.GetType()}. Is its access mode 'public'?", true)
                    : (p.GetValue(target).ToString(), false);
            }
            else
                return (noteAttr.m_NoteOrProperty, false);
        }

        private static void InitIcons()
        {
           s_ArrowRightIcon                  = new(TpIconLib.FindIcon(TpIconType.ArrowRight),"Run"); //note that since this icon only appears at runtime the tooltip is never seen.
           s_OpenInInspectorButtonGUIContent = new(TpIconLib.FindIcon(TpIconType.InfoIcon), "Open in Inspector");
           s_RuntimeGoFlagGuiContent         = new("GameObj RuntimeOnly", "Instantiate the GameObject only during Play.");
           s_KeepGoOnEraseFlagGuiContent     = new("GameObj Retain", "Keep the GO during play even if the tile is deleted or replaced.");
           s_ColorFlagGuiContent             = new("Lock Color",     "Lock/Unlock color in the tilemap.");
           s_TransFlagGuiContent             = new("Lock Transform", "Lock/Unlock the Transform in the tilemap.");
           s_ColorGuiContent                 = new GUIContent("Color",    "Set tile sprite color on the tilemap and within the tile.");
           s_PosGuiContent                   = new GUIContent("Position", "Set tile sprite tilePosition on the tilemap and within the tile.");
           s_RotGuiContent                   = new GUIContent("Rotation", "Set tile sprite rotation on the tilemap and within the tile.");
           s_ScaleGuiContent                 = new GUIContent("Scale",    "Set tile sprite scale on the tilemap and within the tile.");
        }
        
        
        #endregion
        
        #region StandardInspector

         private static CompletionState TileSelectionInspector(TileBase tile,Tilemap map, Vector3Int tilePosition) 
        {
            EditorGUILayout.LabelField("Name", tile.name,GUILayout.ExpandWidth(true));
            
            EditorGUILayout.Separator();
            if (!s_IsPlaying) //much of this is hidden in play mode
            {
                using (new EditorGUIUtility.IconSizeScope(s_ButtonSize))
                {

                    var flagsFromTilemap = map.GetTileFlags(tilePosition);

                    var cFlag         = (flagsFromTilemap & TileFlags.LockColor) != 0;
                    var tFlag         = (flagsFromTilemap & TileFlags.LockTransform) != 0;
                    
                    var colorFlag = EditorGUILayout.ToggleLeft(s_ColorFlagGuiContent, cFlag);
                    if (colorFlag != cFlag)
                    {
                        if (colorFlag)
                            flagsFromTilemap |= TileFlags.LockColor;
                        else
                            flagsFromTilemap &= ~TileFlags.LockColor;

                        var f = flagsFromTilemap;
                        TpLib.DelayedCallback(null,() => map.SetTileFlags(tilePosition, f), "T+Base-CFlag");
                    }

                    var transformFlag = EditorGUILayout.ToggleLeft(s_TransFlagGuiContent, tFlag);
                    if (transformFlag != tFlag)
                    {
                        if (transformFlag)
                            flagsFromTilemap |= TileFlags.LockTransform;
                        else
                            flagsFromTilemap &= ~TileFlags.LockTransform;
                        
                        var f = flagsFromTilemap;
                        TpLib.DelayedCallback( null,() => map.SetTileFlags(tilePosition, f), "T+Base-TFlag");
                    }

                    //Note that this gameObject is an asset reference.
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if (tile!=null && tile is Tile t && t.gameObject != null ) //if this is Tile or subclass and it has a GO, show these too.
                    {
                        var runtimeGoFlag     = (flagsFromTilemap & TileFlags.InstantiateGameObjectRuntimeOnly) != 0;
                        var keepGoOnEraseFlag = (flagsFromTilemap & TileFlags.KeepGameObjectRuntimeOnly) != 0;
                        
                        var rtGoFlag          = EditorGUILayout.ToggleLeft(s_RuntimeGoFlagGuiContent, runtimeGoFlag);
                        if (rtGoFlag != runtimeGoFlag)
                        {
                            if (rtGoFlag)
                                flagsFromTilemap |= TileFlags.InstantiateGameObjectRuntimeOnly;
                            else
                                flagsFromTilemap &= ~TileFlags.InstantiateGameObjectRuntimeOnly;

                            var f = flagsFromTilemap;
                            TpLib.DelayedCallback(null,() => map.SetTileFlags(tilePosition, f), "T+Base-RtGoFlag");
                        }

                        var rtGoKeepFlag = EditorGUILayout.ToggleLeft(s_KeepGoOnEraseFlagGuiContent, keepGoOnEraseFlag);
                        if (rtGoKeepFlag != keepGoOnEraseFlag)
                        {
                            if (rtGoKeepFlag)
                                flagsFromTilemap |= TileFlags.KeepGameObjectRuntimeOnly;
                            else
                                flagsFromTilemap &= ~TileFlags.KeepGameObjectRuntimeOnly;

                            var f = flagsFromTilemap;
                            TpLib.DelayedCallback(null,() => map.SetTileFlags(tilePosition, f), "T+Base-KeepGoFlag");
                        }
                    }

                    EditorGUILayout.Separator();
                    
                    //GUI for color: hidden if tile locks color or tileflags is set to lock color
                    if ((flagsFromTilemap & TileFlags.LockColor) != TileFlags.LockColor)
                    {
                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Reset"))
                            map.SetColor(tilePosition, Color.white);

                        GUI.skin = null; //avoids skin problems with colorfield popup
                        EditorGUI.BeginChangeCheck();
                        var colorFromTilemap = map.GetColor(tilePosition);
                        var newColor         = EditorGUILayout.ColorField(s_ColorGuiContent, colorFromTilemap);
                        GUI.skin = TpSkin;
                        if (EditorGUI.EndChangeCheck())
                            map.SetColor(tilePosition, newColor);
                        EditorGUILayout.EndHorizontal();
                    }

                    //GUI for transform controls: hidden if tile is manipulating transforms
                    //or if tileflags is set to lock transform.
                    if ((flagsFromTilemap & TileFlags.LockTransform) != TileFlags.LockTransform)
                    {
                        TileUtil.GetTransformComponents(map, tilePosition, out var pos, out var rotation, out var scale);
                        
                        pos = TileUtil.RoundVector3(pos,4);
                        rotation = TileUtil.RoundVector3(rotation,4);
                        scale    = TileUtil.RoundVector3(scale,4);

                        EditorGUILayout.BeginHorizontal();
                        if (GUILayout.Button("Reset"))
                            map.SetTransformMatrix(tilePosition, Matrix4x4.identity);

                        EditorGUILayout.BeginVertical();
                        EditorGUI.BeginChangeCheck();

                        var newPosition = EditorGUILayout.Vector3Field(s_PosGuiContent, pos);
                        var newRotation = EditorGUILayout.Vector3Field(s_RotGuiContent, rotation);
                        var newScale = EditorGUILayout.Vector3Field(s_ScaleGuiContent, scale);
                        if (EditorGUI.EndChangeCheck() && newScale != Vector3.zero)
                        {
                            TileUtil.SetTransform(map,tilePosition,newPosition,newRotation,newScale);
                        }

                        EditorGUILayout.EndVertical();
                        EditorGUILayout.EndHorizontal();

                    }
                }
            }
            else
            {
                StandardRuntimeInspector(map,tilePosition);
            }

            return S_FailureCompletionState;

        }

         /// <summary>
         /// Shows transform and other info when editor is in Play mode
         /// </summary>
         /// <param name="map">tilemap</param>
         /// <param name="tilePosition">position</param>
         public static void StandardRuntimeInspector(Tilemap map, Vector3Int tilePosition)
         {
             TileUtil.GetTransformComponents(map, tilePosition, out var pos, out var rot, out var scl);

             EditorGUILayout.HelpBox($"Transform\nposition:{pos.ToString()}\nrotation:{rot.ToString()}\nscale:{scl.ToString()}", MessageType.None);

             var co = map.GetColor(tilePosition);
             EditorGUILayout.HelpBox($"Color {co.ToString()}", MessageType.None);

             var tileAnimFlags   = map.GetTileAnimationFlags(tilePosition);
             var paused          = (tileAnimFlags & TileAnimationFlags.PauseAnimation) != 0;
             var flags           = map.GetTileFlags(tilePosition);
             var k               = (int)flags;
             var fixedFlags      = (TileFlags)(k & 255); //some of the flags don't have enum values, ie the 'Animation On' flag as above.
             var undocAnimOnFlag =( k & 0x20000000) != 0;
             var s               = "Off";
             if (undocAnimOnFlag)
                 s = paused ? "Paused" : "Running";


             EditorGUILayout.HelpBox($"TileFlags: {Enum.Format(typeof(TileFlags),                            fixedFlags,    "f")} [{k:X}] {(undocAnimOnFlag?"[Animation Running]":string.Empty) }" , MessageType.None);
             EditorGUILayout.HelpBox($"Animation: [{s}] AnimFlags: [{Enum.Format(typeof(TileAnimationFlags), tileAnimFlags, "f")}]",                                                                 MessageType.None);
             
         }

        #endregion

    }
    
    
}
