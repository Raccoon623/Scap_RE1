using TilePlus;
using UnityEngine;

namespace TilePlusDemo
{
    
    /// <summary>
    /// A simple demo illustrating one way to save and restore data.
    /// </summary>
    [CreateAssetMenu(fileName = "SaveRestoreDemo.asset", menuName = "TilePlus/Demo/Create SaveRestoreDemo tile", order = 1000)]
    public class SaveRestoreDemo : TilePlusBase, ITpPersistence<DemoData,DemoData>
    {
        /// <summary>
        /// Definition for the enum in this example
        /// </summary>
        public enum EnumDef
        {
            A,
            B,
            C
        }

        [TptShowEnum]
        public EnumDef m_EnumItem;
        [TptShowField]
        public bool    m_BoolItem;
        [TptShowField]
        public int     m_IntItem;
        [TptShowField]
        public string  m_StringItem;
        [TptShowField]
        public float   m_FloatItem;

        public override string ToString()
        {
            return $"Enum: {m_EnumItem}, bool: {m_BoolItem}, int: {m_IntItem}, string: {m_StringItem}, float: {m_FloatItem}";
        }
        
        /// <summary>
        /// Clear the data of this instance
        /// </summary>
        public void ClearData()
        {
            m_EnumItem   = EnumDef.A;
            m_BoolItem   = false;
            m_IntItem    = 0;
            m_StringItem = string.Empty;
            m_FloatItem  = 0.0f;
        }
        
        /// <summary>
        /// Implementation of ITpPersistence 
        /// </summary>
        /// <returns>DemoData instance</returns>
        
        public DemoData GetSaveData(object _)
        {
            return new DemoData
            {
                m_BoolSaveData   = m_BoolItem,
                m_IntSaveData    = m_IntItem,
                m_StringSaveData = m_StringItem,
                m_FloatSaveData  = m_FloatItem,
                m_EnumSaveData   = (int) m_EnumItem,
                m_Guid           = TileGuidString
            };
        }

        /// <summary>
        /// Implementation of ITpPersistence 
        /// </summary>
        public void RestoreSaveData(DemoData d)
        {
            m_BoolItem   = d.m_BoolSaveData;
            m_IntItem    = d.m_IntSaveData;
            m_StringItem = d.m_StringSaveData;
            m_FloatItem  = d.m_FloatSaveData;
            m_EnumItem   = (EnumDef) d.m_EnumSaveData;
        }
    }

    /// <summary>
    /// Data packet for this class
    /// </summary>
    public class DemoData: MessagePacket<DemoData>
    {
        public string m_Guid;
        public int    m_EnumSaveData;
        public bool   m_BoolSaveData;
        public int    m_IntSaveData;
        public string m_StringSaveData;
        public float  m_FloatSaveData;

        
    }
    
    
}
