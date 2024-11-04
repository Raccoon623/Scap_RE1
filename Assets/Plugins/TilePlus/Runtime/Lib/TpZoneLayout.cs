// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 03-25-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 04-01-2023
// ***********************************************************************
// <copyright file="TpZoneLayout.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace TilePlus
{
    /* Constraints for this chunk layout.
     1.	Chunks (TileFabs) all need to be all the same size: square, with even-number dimensions such as 64 x 64. 
        All layers of the chunk should be from Tilemaps with the same cell size parameters and origin.
     2.	The smallest chunk size is 4x4. Smaller chunks == more memory use and slower execution of adding/deleting chunks during UpdateTick.
     3.	The camera needs to be Orthographic in the provided implementation.
     */
    /// <summary>
    /// This is a simple chunking system based on TileFabs.
    /// </summary>
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class TpZoneLayout : MonoBehaviour
    {
        #region enum

        /// <summary>
        /// Return value from UpdateTick load filter
        /// </summary>
        /// 
        public enum LoadFilterResult
        {
            /// <summary>
            /// Fill this zone. It's OK!
            /// </summary>
            FillZone,
            /// <summary>
            /// Leave this zone empty. It may be filled later though...
            /// </summary>
            LeaveZoneEmpty,
            /// <summary>
            /// Leave this zone empty but mark it reserved so it stays empty.
            /// </summary>
            /// <remarks>Only as long as it is within the CamViewport+Padding.</remarks>
            MarkZoneFilledButLeaveEmpty,
            /// <summary>
            /// Fill the Zone and mark it immortal. It'll never be deleted.  
            /// </summary>
            FillZoneAndMarkImmortal
        }
        
        #endregion
        
        #region publicFields

        /// <summary>
        /// For debugging.
        /// </summary>
        [Tooltip("If unchecked, this Layout won't be used.")]
        public bool m_Active = true;

        /// <summary>
        /// Chunk size used for this layout
        /// </summary>
        [Tooltip("Chunk size used for this layout")]
        public int m_ChunkSize = 16;

        /// <summary>
        /// The camera that will be used determine what's loaded and unloaded.
        /// </summary>
        [Tooltip("Camera to use for chunking viewport calculations")]
        public Camera? m_ReferenceCamera;

        /// <summary>
        /// world origin.
        /// </summary>
        [Tooltip("world origin. Will be changed to be aligned to the super-grid defined by the chunk size of the Tilefabs")]
        public Vector3Int m_WorldOrigin;

        /// <summary>
        /// Parent Grid for all tilemaps used by this layout
        /// </summary>
        [Tooltip("Parent Grid for all tilemaps used by this layout")]
        public Grid? m_Grid;

        /// <summary>
        /// Extra padding around camera viewport.
        /// </summary>
        [Tooltip("Extra padding around the camera viewport in CHUNKS")]
        public Vector2Int m_CameraViewPadding = new(2, 2);

        /// <summary>
        /// The name to use when creating a zone manager. Must be unique.
        /// </summary>
        [Tooltip("The name to use when creating a zone manager. Must be unique.")]
        public string m_ZoneManagerName = "";

        /// <summary>
        /// A selector to use for this layout.
        /// </summary>
        [Tooltip("A Chunk Selector")]
        public TpChunkSelectorBase? m_ChunkSelector;

        /// <summary>
        /// If true, a marquee is drawn for the camera rect
        /// </summary>
        [Tooltip("Show a gizmo for the camera Rect.")]
        public bool m_ShowCameraRect;

        /// <summary>
        /// For diags, this inhibits tilefab loading so only the camera rect info is visible.
        /// </summary>
        [Tooltip("If this is checked, ShowCameraRect is also 'on' but no loading. Used to see how the camera viewport is subdivided.")]
        public bool m_DoNotLoad;

        /// <summary>
        /// Show tilefab load results in console
        /// </summary>
        [Tooltip("Show tilefab load results in console")]
        public bool m_DebugMessages;


        #endregion

        #region publicProperties


        /// <summary>
        /// This can be useful in a Selector
        /// </summary>
        /// <value>The zones outside viewport.</value>
        public List<ZoneReg> ZonesOutsideViewport => outside;

        #endregion

        #region private

        //a list with loading results from when a zone is loaded
        /// <summary>
        /// The load results
        /// </summary>
        private List<TilefabLoadResults> loadResults = new(16);

        //list of viewport subdivision RectInt Locators
        /// <summary>
        /// The viewport subdivisions
        /// </summary>
        private List<RectInt> viewportSubdivisions = new(32);

        /// <summary>
        /// The zone manager
        /// </summary>
        private TpZoneManager? zoneManager;

        //locators outside the viewport+padding
        /// <summary>
        /// The inside
        /// </summary>
        private HashSet<RectInt> inside = new(128);

        //ZoneRegs outside the viewport+padding
        /// <summary>
        /// The outside
        /// </summary>
        private List<ZoneReg> outside = new(128);

        //ZoneRegs to be removed
        /// <summary>
        /// The zones to remove
        /// </summary>
        private readonly List<ZoneReg> zonesToRemove = new(128);
        //Zones to be added.
        /// <summary>
        /// The zones to load
        /// </summary>
        private readonly List<TileFabLoadParams> zonesToLoad = new(128);

        #endregion

        #region setup

        /// <summary>
        /// Startup the chunking system: base class accepts ortho cam only!
        /// </summary>
        /// <param name="zm">Zone manager instance used by this layout is placed in this out parameter</param>
        /// <param name="managedTilemaps">Tilemaps to be used by this layout. If not provided,
        /// maps are taken from Tilemap children to m_Grid field in component.</param>
        /// <param name="padding">[Nullable] extra size added to Camera viewport, can help hide
        /// load/unload transitions from Camera viewport. If null, uses value from this component.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        /// <remarks>Using this method a second time resets all chunk-related information.
        /// How to calc size of the world:
        /// example sizeOfWorld: If size of chunks is 64, then the TileFabs must be 64x64
        /// (although this is not enforced). This means that the number of Grid units is 64 x 64 for
        /// each TileFab. You probably want to set some limit of how far to move in any direction. Let's
        /// say you didn't want to more more than 1024 Grid units in any direction so that would mean
        /// that the world was 1024 x 1024 Grid units. Divide that by 64 to get 16. 16 x 16 is the grid
        /// of TileFabs, that is a total of 256 TileFabs.
        /// In essence you are using the TileFabs as part of a larger grid.</remarks>
        // ReSharper disable once MemberCanBeProtected.Global
        public virtual bool Initialize(out TpZoneManager? zm,
                                       Tilemap[]?         managedTilemaps = null,
                                       Vector2Int?        padding = null)
        {

            if (m_ReferenceCamera == null || m_Grid == null || m_ChunkSelector == null)
            {
                Debug.LogError("Missing Camera, Chunk Selector, or Grid Reference.");
                zm = null;
                return false;
            }

            //this is important: don't change here. If you want to change cam position do it later than this.
            m_ReferenceCamera.transform.position = m_Grid.CellToWorld(m_WorldOrigin);
            
            if (!m_ReferenceCamera.orthographic)
            {
                Debug.LogError("Camera must be orthographic!!");
                zm = null;
                return false;
            }
            
            if (m_ChunkSize < 4)
            {
                Debug.LogError("Chunk size is too small. Must be >= 4 (a 4x4 TileFab)");
                zm = null;
                return false;
            }

            if (string.IsNullOrWhiteSpace(m_ZoneManagerName))
            {
                Debug.LogError("A Unique Zone manager name must be provided ");
                zm = null;
                return false;
            }

            if (managedTilemaps == null)
            {
                //create the mapName-to-mapInstance dict.
                managedTilemaps = m_Grid.GetComponentsInChildren<Tilemap>();
                if (managedTilemaps == null)
                {
                    Debug.LogError("No Tilemaps found!!");
                    zm = null;
                    return false;
                }
            }

            //if there isn't an existing ZM, create an instance of one.
            //here's how to get a zone manager instance. 
            //First, see if there is one already. The names have to be unique.
            if (!TileFabLib.GetNamedInstance(m_ZoneManagerName, out zm))
            {
                //create dictionary
                var nameToMapDict = managedTilemaps.ToDictionary(k => k.name, v => v);
                if (!TileFabLib.CreateZoneManagerInstance(out zm, m_ZoneManagerName, nameToMapDict))
                {
                    Debug.LogError("Could not get a ZoneManager instance!! Did you supply a UNIQUE name string??");
                    zm = null;
                    return false;
                }

                zoneManager =zm;
            }

            if (zm == null)
            {
                Debug.LogError($"Could not get a ZoneManager instance!! ZoneManager option in TileFabLib is: {(TileFabLib.ZoneManagerCreationEnabled?"ON":"OFF")} ");
                return false;
            }
            zoneManager = zm;

            var pad             = padding ?? m_CameraViewPadding;
            var        camViewDims     = TpZoneManagerLib.OrthographicCameraViewDimensions(m_ReferenceCamera, m_ChunkSize, pad);
            var        numChunksInView = TpZoneManagerLib.NumberOfChunksInCameraView(camViewDims, m_ChunkSize);
            if (numChunksInView < 0) //nothing to do, so return the ZM instance and quit.
                numChunksInView = 4;           
            zoneManager.Initialize(m_ChunkSize, m_WorldOrigin, numChunksInView);
            m_ChunkSelector.Initialize(zm);
            return true;
        }

        #endregion

        #region events
        /// <summary>
        /// Called before layout is being destroyed.
        /// </summary>
        private void OnDisable()
        {
            TileFabLib.DeleteNamedInstance(m_ZoneManagerName);
        }

        #endregion



        #region layout

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// Call this whenever you want to update the chunking system, eg after the Player moves
        /// </summary>
        /// <param name="worldPosition">Current Camera position. Note that if cam pos is lerped to player player's transform.position could work better.</param>
        /// <param name="padding">Increase the size implied by the cam view in CHUNKSIZE units.
        /// usually this is desireable, by at least one unit, to provide a border around
        /// the camera view so no flickering is observed as TileFabs are loaded and unloaded.
        /// --IF NULL -- the padding is from a field in the component</param>
        /// <param name="unloadingFilter">A func taking a ZoneRef and returning a bool.
        /// This filter is used when removing zones OUTSIDE the viewport area.
        /// The ZoneRef has the 'offset' within it: the position of the Tilefab that will be deleted as
        /// well as the ZoneManager instance and a user object passed to this method when invoked.
        /// This is invoked when deleting: return False to inhibit deletions. Note tha this loadingFilter
        /// is not used when the ZoneRef is marked immortal.
        /// This loadingFilter is useful if there are areas that you want the chunking system to avoid deleting and
        /// you don't want to mark them immortal.</param>
        /// <param name = "loadingFilter" >Same as unloadingFilter but used when ADDING chunks INSIDE the viewport area.</param>
        /// <param name = "userData" >passed to the Filter - this can be anything that you want.</param>
        /// <returns>False if something went wrong or if nothing was done, otherwise true.</returns>
        /// <remarks>in spite of the name, this doesn't need to be called on every Update!! For
        /// example, it doesn't make sense to repeatedly call this when within one Zone ie within a square area
        /// defined by the chunkSize, as the result would be the same each time. It'd
        /// also be really inefficient.
        /// ALSO: PLEASE NOTE that it your responsibility to ensure that the supplied TileFab reference has the same chunk size.
        /// IE, if the chunksize is set at 32 don't provide a 16x16 or 64 x 64 chunk as a return value for this Func. It's
        /// called many times so precalculate when possible.</remarks>
        public virtual bool UpdateTick(Vector3              worldPosition,
                                       Vector2Int?          padding = null,
                                       Func<ZoneReg,object,TpZoneManager, bool>? unloadingFilter  = null,
                                       Func<object,TpZoneManager, TileFabLoadParams, LoadFilterResult>? loadingFilter  = null,
                                       object? userData = null)

        {
            if (zoneManager == null || m_ChunkSelector == null || m_Grid == null)
                return false;
            
            if (m_ReferenceCamera == null || !m_ReferenceCamera.orthographic || zoneManager == null)
                return false;

            //size of a default locator. this was determined by the sizeOfChunks param to Initialize
            var size = zoneManager.DefaultLocator.size.x;

            //dimensions of a rectangle where the camera is: fuzzy, will usually be bigger which is fine.
            var p           = padding ?? m_CameraViewPadding; //use supplied padding or the padding value set in this component.
            var camViewDims = TpZoneManagerLib.OrthographicCameraViewDimensions(m_ReferenceCamera, size, p);

            //adjust position to properly place the origin at the lower-left corner of the RectInt locator we will obtain
            var position         = m_Grid.WorldToCell(worldPosition);
            var adjustedPosition = new Vector3Int(position.x - (camViewDims.x / 2), position.y - (camViewDims.y / 2));

            //now create a RectInt locator with this position and the camera view size
            //this next line gets a Rect with the LLCorner at the position and a size of the
            //camera viewport (rounded up with optional padding).
            //Note that this method aligns the adjustedPosition to be on-sGrid. This is required because
            //zone regs are based on a RectInt and the super-grid coordinates align with the positions of the RectInt.
            var viewPortLocator = zoneManager.GetLocatorForGridPosition(adjustedPosition, camViewDims);

            //this fills in the viewportSubdivisions list with all possible RectInt locators for the entire viewport.
            TpZoneManagerLib.SubdivideRectInt(viewPortLocator, size, ref viewportSubdivisions!, zoneManager);

            //this gets a HashSet of all zoneRegs inside the viewportLocator and a
            //list of all zoneRegs outside the viewportLocator
            if(!zoneManager.FindRegionalZoneRegs(viewPortLocator, ref inside, ref outside))
                return false;
            
            //here, numChunksInView is the number of chunks you'd expect to find in a fully-filled in area. Note
            //that this may include immortal chunks (which can't be deleted)
            var expectedChunksInView = viewportSubdivisions.Count;
            if (expectedChunksInView < 0)
                return false;

            //in-editor show a marquee for 2 seconds.
            #if UNITY_EDITOR
            if (m_ShowCameraRect || m_DoNotLoad)
            {
                var marqBounds = new BoundsInt((Vector3Int)viewPortLocator.position, (Vector3Int)viewPortLocator.size);
                TpEditorBridge.TilemapMarquee(m_Grid, marqBounds, Color.blue, -2, false, null);

                foreach (var locator in viewportSubdivisions)
                {
                    marqBounds = new BoundsInt((Vector3Int)locator.position, (Vector3Int)locator.size);
                    TpEditorBridge.TilemapMarquee(m_Grid, marqBounds, Color.red, -2, false, null);
                }
            }
            #endif

            /*algo is:
             Delete all registrations(chunks) that were NOT part of the view+padding area unless loadingFilter says not to.
             If there are fewer registrations (chunks) then add a chunk where there isn't one unless selector says not to.
            */
            
            // delete all outside the viewport
            //get pooled lists
            zonesToRemove.Clear();
            
            var hasFilter = unloadingFilter != null;
            foreach (var reg in outside)
            {
                if (reg.imm)
                    continue;

                if (hasFilter)
                {
                    if (unloadingFilter!(reg,userData!,zoneManager))
                        zonesToRemove.Add(reg);
                }
                else
                    zonesToRemove.Add(reg);
            }

            var numZonesRemoved = zonesToRemove.Count;
            if (numZonesRemoved != 0)
                zoneManager.UnloadZones(zonesToRemove);
            
            var errors  = 0;
            
            zonesToLoad.Clear();
            hasFilter = loadingFilter != null;
            // ReSharper disable once ForeachCanBeConvertedToQueryUsingAnotherGetEnumerator
            foreach (var locator in viewportSubdivisions)
            {
                //if the 'inside' hashset contains the locator then we don't need to do anything else.
                if (inside.Contains(locator))
                    continue;
                //double-check.
                if (zoneManager.HasZoneRegForLocator(locator)) //something already exists there
                    continue;

                var loadParams = m_ChunkSelector.Selector(locator, this, zoneManager.MonitoredTilemapDict);
                
                if(loadParams == null)
                    continue;
                if (!loadParams.Valid) //this could be an error, too. But for this we'll ignore it w warning.
                {
                    errors++;
                    continue;
                }

                if (hasFilter)
                {
                    var action = loadingFilter!(userData!, zoneManager, loadParams);
                    switch (action)
                    {
                        case LoadFilterResult.LeaveZoneEmpty:
                            continue;
                        case LoadFilterResult.MarkZoneFilledButLeaveEmpty:
                            loadParams.m_Reserved = true;
                            break;
                        case LoadFilterResult.FillZoneAndMarkImmortal:
                            loadParams.m_Immortal = true;
                            break;
                    }
                    //last enum value test not needed, drop out of this block.
                }

                zonesToLoad.Add(loadParams);
            }

            if (errors != 0)
                Debug.LogWarning($"{errors} errors occurred during exec of 'locator' callback in UpdateTick");


            var status    = true;
            var numToLoad = zonesToLoad.Count;
            loadResults.Clear();
            if (numToLoad != 0 && !m_DoNotLoad)  
                status = TileFabLib.LoadTileFabs(zonesToLoad, ref loadResults, zoneManager);
            if (!m_DebugMessages || !status)
                return errors != 0;
            Debug.Log($"Loading completed OK? {status} with {errors} Selector errors. Unloaded: {numZonesRemoved}, Loaded: {numToLoad} ");
            foreach (var stat in loadResults)
                Debug.Log(stat);

            return errors != 0;


        }
        #endregion
    }
   
}
