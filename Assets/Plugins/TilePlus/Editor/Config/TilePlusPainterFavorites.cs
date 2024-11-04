// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-05-2024
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-25-2024
// ***********************************************************************
// <copyright file="TilePlusPainterFavorites.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;
#nullable enable

namespace TilePlus.Editor.Painter
{
    /// <summary>
    /// Favorites persistence
    /// </summary>
    [FilePath("TpConfig/TilePlusFavorites.asset", FilePathAttribute.Location.ProjectFolder)]
    public class TilePlusPainterFavorites : ScriptableSingleton<TilePlusPainterFavorites>
    {
        #region privateFields
        /// <summary>
        /// Max # of items in Favorites.
        /// </summary>
        private const int Favorites_Max_Size = 32;
        
        /// <summary>
        /// UnityEngine.Object favorites
        /// </summary>
        [SerializeField]
        private List<Object> m_PainterFavorites = new();

        /// <summary>
        /// TileCell (Pick) favorites
        /// </summary>
        [SerializeField]
        private List<TileCellsBundle> m_TileCellsBundles = new();
        
        [NonSerialized]
        private bool initialized;

        /// <summary>
        /// Number of items in the Favorites list.
        /// </summary>
        public int FavoritesListSize => m_WorkingSet.Count;

        /// <summary>
        /// This is the actual Favorites list that the Painter uses
        /// </summary>
        [SerializeField]
        private List<Object> m_WorkingSet = new();

        /// <summary>
        /// Get the favorites list (Objects)
        /// </summary>
        public List<Object> Favorites => m_WorkingSet; 
        
        #endregion
        
        #region events

        private void OnEnable()
        {
            initialized = false; //prob redundant
        }

        private void OnDisable()
        {
            if(TpLibEditor.Informational)
                TpLib.TpLog("TpPainterFavorites ScriptableSingleton released.");

        }

        #endregion
        
        #region init
        
         //unpack the two deserialized lists into the workingset list.
         //note: this should not be done in OnEnable: can cause race conditions causing recursion errors in JSON overwrite.
        internal void Initialize()
        {
            if(initialized)
                return;
            initialized = true;
            
            if (TpLibEditor.Informational)
                TpLib.TpLog("TilePlusPainterFavorites ScriptableSingleton Initializing.");
            //the clear is needed since m_WorkingSet actually does get serialized to the filesystem
            //but we don't want what's in there at this point as it's not clean - has nulls for Picks
            //and nulls for any TilePlus tiles that may have been placed in Favorites.
            m_WorkingSet.Clear();
            //load the TileCellBundles (picks) first. 
            //These need conversion to UnityEngine.Objects of type TileCellWrapper.
            m_WorkingSet.AddRange(m_TileCellsBundles.Select(x =>
                                                          {
                                                              var so = CreateInstance<TileCellsWrapper>();
                                                              so.m_Bounds                          = x.m_Bounds;
                                                              so.Cells                           = x.m_Cells;
                                                              so.UnpackCells();
                                                              so.m_MultipleTilesSelectionBoundsInt = x.m_MultipleTilesSelectionBoundsInt;
                                                              if (x.m_IconData == null)
                                                              {
                                                                  if(TpLib.Informational)
                                                                    TpLib.TpLog("Icon data not found??? Not necc an error...");
                                                                  return so;
                                                              }

                                                              var len = x.m_IconData.Length;
                                                              so.m_IconData = new byte[len];
                                                              Array.Copy(x.m_IconData, so.m_IconData, len);
                                                              //Debug.Log($"icon data dest len {so.m_IconData.Length.ToString()}");
                                                              return so;
                                                          }));
            //Add the normal favorites: tiles and prefabs
            m_WorkingSet.AddRange(m_PainterFavorites);
            
            //schedule a save if favorites list cleaning removed null objects.
            //Dupes really shouldn't exist at this point.
            if(CleanFavoritesList() > 0)
                RegisterSave();
        }
        #endregion
        
        

       #region addRemove
        /// <summary>
        /// Add an array of Objects to Favorites
        /// </summary>
        /// <param name="objects"></param>
        public static void AddToFavorites(Object[] objects)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (objects.Length == 1 && objects[0] != null)
            {
                //multiple selection
                // ReSharper disable once Unity.NoNullPatternMatching
                if(objects[0] is TileCellsWrapper wrapper)
                {
                    if (wrapper.Icon == null)
                        wrapper.Icon = TpImageLib.CreateMultipleTilesIcon(wrapper).sprite;
                    wrapper.CreateTileCellsBundle();
                }
            }


            instance.m_WorkingSet.InsertRange(0,objects);
            CleanFavoritesList(); //remove dupes or anything that evals to null
            RegisterSave();
            //if painter is open, update the palette list.
            if(TilePlusPainterWindow.RawInstance != null)
                TilePlusPainterWindow.RawInstance.UpdatePaletteView();

        }


        
        /// <summary>
        /// Remove an object from Favorites
        /// </summary>
        /// <param name="index">the index</param>
        public static void RemoveFromFavorites(int index)
        {
            if (index < 0 || index >= instance.m_WorkingSet.Count)
                return;
            instance.m_WorkingSet.RemoveAt(index);

            RegisterSave();
            UpdatePainter();
        }

        /// <summary>
        /// Clear the Favorites list and Save it.
        /// </summary>
        public static void ClearFavorites()
        {
            instance.m_WorkingSet.Clear();
            RegisterSave();
            UpdatePainter();
        }

        #endregion
        
        #region utilities
        /// <summary>
        /// Clean the favorites list of nulls and dupes.
        /// </summary> 
        /// <returns>number of removed items</returns>
        public static int CleanFavoritesList() 
        {
            var before  = instance.m_WorkingSet.Count;
            instance.m_WorkingSet.RemoveAll((t) => t == null);

            using (ListPool<Object>.Get(out var tmp))
            {
                tmp.Clear(); //just to be sure.
                foreach (var o in instance.m_WorkingSet)
                {
                    if(o == null)
                        continue;
                    if (tmp.Contains(o))
                        continue;
                    //the above works for assets like tiles, bundles, and prefabs. For TileCellWrappers it is more work
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if (o is TileCellsWrapper w)
                    {
                        //if no cells skip this
                        if(w.Cells == null || w.Cells.Length == 0)
                            continue;
                        //now test each tile for null IFF it's a TilePlus tile (eg a TilePlus tile was saved)
                        var fail = false;
                        foreach (var cell in w.Cells.Where(cell => cell.IsTilePlus))
                        {
                            if(cell.TileBase == null)
                                fail = true;
                            break;
                        }

                        if (fail)
                            continue;
                        
                        if(tmp.Any(WrapperMatch)) //if a match was found then it's a dupe, skip this.
                            continue;             //otherwise drop down to tmp.Add(o)
                            
                        //don't move this. Rets true for a match
                        bool WrapperMatch(Object obj)
                        {
                            if (obj== null)
                                return false;
                            // ReSharper disable once Unity.NoNullPatternMatching
                            if (obj is not TileCellsWrapper z)
                                return false;
                            if (w.GetHashCode() == z.GetHashCode())
                                return true;
                            //this is like .Equals but safer w/null-detect.
                            return z.Cells != null &&
                                   w.Cells != null &&
                                   w.m_MultipleTilesSelectionBoundsInt == z.m_MultipleTilesSelectionBoundsInt &&
                                   w.m_Bounds == z.m_Bounds &&
                                   w.Cells.Length == z.Cells.Length; //note that this ignores the content of the cells which would be really slow in some cases.
                        }
                    }

                    tmp.Add(o); //this is the filtered output.
                }

                instance.m_WorkingSet.Clear();
                //check the size of the list
                instance.m_WorkingSet.AddRange(tmp.Count > Favorites_Max_Size
                                                   ? tmp.Take(Favorites_Max_Size)
                                                   : tmp); //copy the filtered range to workingset.
            }
            
            //was there any change? IE did the filtering remove any dupes?
            var after = instance.m_WorkingSet.Count;
            var diff  = before - after;
            if (diff == 0)
                return diff;
            if (TpLibEditor.Informational)
                TpLib.TpLog($"Clean Favorites List removed {diff} null and/or duplicate objects.");
            RegisterSave();
            UpdatePainter();
            return diff;
        }

        /// <summary>
        /// Inform painter of changes
        /// </summary>
        private static void UpdatePainter()
        {
            if (!EditorWindow.HasOpenInstances<TilePlusPainterWindow>())
                return;
            var win = TilePlusPainterWindow.RawInstance;
            if(win != null)
                win.OnProjectChange();
        }

        private static bool s_SaveRequested;
        private static void RegisterSave()
        {
            if(s_SaveRequested)
                return;
            s_SaveRequested               =  true;
            EditorApplication.delayCall += DoSave;
            return;

            void DoSave()
            {
                s_SaveRequested = false;
                //prepare for serialization by breaking up the workingset list into two lists
                //this is needed since TileCellWrappers are ScriptableObjects and we can't serialize these.

                CleanFavoritesList();
                instance.m_TileCellsBundles.Clear();
                instance.m_PainterFavorites.Clear();
                foreach (var o in instance.m_WorkingSet)
                {
                    if (o == null)
                        continue;
                    // ReSharper disable once Unity.NoNullPatternMatching
                    if (o is TileCellsWrapper wrapper)
                    {
                        // ReSharper disable once ConvertIfStatementToNullCoalescingExpression
                        if (wrapper.TileCellBundle != null)
                            instance.m_TileCellsBundles.Insert(0, wrapper.TileCellBundle);
                        else
                            instance.m_TileCellsBundles.Insert(0, new TileCellsBundle(wrapper));
                    }

                    else
                        instance.m_PainterFavorites.Insert(0, o);
                }

                instance.Save(true);
            }
        }
        #endregion
    }
}
