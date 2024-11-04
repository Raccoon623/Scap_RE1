using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using TilePlus;


namespace TilePlusDemo
{
    /// <summary>
    /// A simple demo of saving and restoring data from tiles.
    /// </summary>
    public class SaveRestoreDemoController : MonoBehaviour
    {
        private       SaveRestoreDemo demoTileA;  //cached "A" tile instance
        private       SaveRestoreDemo demoTileB;  //cached "B" tile instance
        private const string          RecordSeparator = "<RS>"; //separates JSON records


        private IEnumerator Start()
        {
            /*wait a few frames: this is done to allow the Tilemaps in the scene to
             run thru their own Start(). This calls StartUp on all the tiles, and the
             TilePlus tiles will register themselves in TpLib.
            */
            yield return null;
            yield return null;
            while (!TpLib.TpLibIsInitialized) //wait till ready
                yield return null;

            //this is very simple: just get all (2) tiles of type SaveRestorDemo, check for matching tag, and
            //init the references in this monobehaviour.
            var demoTiles = new List<TilePlusBase>();
            TpLib.GetAllTilesOfType(null, typeof(SaveRestoreDemo), ref demoTiles);
            foreach (var tile in demoTiles)
            {
                //Switch on the tiles' Tag property, which returns the Tag set into the tile's instance data
                switch (tile.Tag) 
                {
                    case "A":
                        demoTileA = (SaveRestoreDemo) tile;
                        break;
                    case "B":
                        demoTileB = (SaveRestoreDemo) tile;
                        break;
                }
            }
        }

        /// <summary>
        /// Clear A's data when button clicked
        /// </summary>
        public void ClearTileA()
        {
            demoTileA.ClearData();
            Debug.Log($"Tile A cleared: {demoTileA} ");
        }

        /// <summary>
        /// Clear B's data when button clicked
        /// </summary>
        public void ClearTileB()
        {
            demoTileB.ClearData();
            Debug.Log($"Tile B cleared: {demoTileB} ");
        }

        /// <summary>
        /// Show the internal data from each tile instance.
        /// Button-click target.
        /// </summary>
        public void ShowCustomData()
        {
            var tiles = new List<TilePlusBase>();

            TpLib.GetAllTilesOfType(null, typeof(SaveRestoreDemo), ref tiles);
            var demoTiles = tiles.Cast<SaveRestoreDemo>();
            foreach (var tile in demoTiles)
                Debug.Log($"Data from tile {tile.TileName}: {tile}");
        }


        /// <summary>
        /// Save tiles' data to a JSON file when a button is clicked
        /// </summary>
        public void SaveData()
        {
            var demoTiles = new List<TilePlusBase>();
            TpLib.GetAllTilesWithInterface<ITpPersistenceBase>(ref demoTiles);
            if (demoTiles.Count == 0)
            {
                Debug.LogError("Could not find tiles for saving data");
                return;
            }
            
            var json      = string.Empty;

            //scan thru the tiles. 
            foreach (var tile in demoTiles)
            {
                var t = tile as SaveRestoreDemo;
                if (!t)
                    continue;                           //ignore wrong type (redundant here but good practice)
                var data = t.GetSaveData(null);             //get the data packet of type DemoData
                json += RecordSeparator;                //first insert the record separator
                json += JsonUtility.ToJson(data, true); //json-formatted representation of DemoData instance
                json += "\n";
            }
            
            //now just save the JSON-ized data
            var path = Path.Combine(Application.persistentDataPath, "SaveRestoreDemo.txt");
            try
            {
                File.WriteAllText(path, json);
                Debug.Log($"data saved to:{path}");
            }
            catch (Exception e)
            {
                Debug.LogError("data save failed. " + e.Message);
            }
        }

        /// <summary>
        /// Restore data to tiles when button is clicked.
        /// </summary>
        public void RestoreData()
        {
            //get the JSON formatted data from the file system
            var    path = Path.Combine(Application.persistentDataPath, "SaveRestoreDemo.txt");
            string jsonString;
            try
            {
                jsonString = File.ReadAllText(path);
            }
            catch (FileNotFoundException)
            {
                Debug.Log("File not found: " + path);
                return;
            }
            catch (Exception e)
            {
                Debug.LogError("data load failed. " + e.Message);
                return;
            }

            Debug.Log($"Loading from {path}...");

            //split the JSON string into sections for each tile.
            var sections = jsonString.Split(new[] {RecordSeparator}, StringSplitOptions.RemoveEmptyEntries);
            
            //for each section, unpack the JSON in that section and send it to tiles. 
            foreach (var section in sections)
            {
                var data = JsonUtility.FromJson<DemoData>(section); //unpack a section into a DemoData instance
                var tile = TpLib.GetTilePlusBaseFromGuid(data.m_Guid); //find the tile.
                if(tile == null)
                    continue;
                //this next line is optional (aside from the cast)
                //for this example because we know that the tile is the correct type.
                //but for cases where there are different types of tiles it would matter. 
                // ReSharper disable once Unity.NoNullPatternMatching
                if(tile is ITpPersistence<DemoData,DemoData> t)
                    t.RestoreSaveData(data); //The tile rejects the section if the GUID doesn't match.
            }
            
            //print the tiles' instance data to the console
            ShowCustomData();
        }
    }
}
