// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 03-25-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 02-07-2023
// ***********************************************************************
// <copyright file="CustomGui.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

namespace TilePlus
{
    /// <summary>
    /// Used to provide the return value from methods tagged with TptShowCustomGUI
    /// </summary>
    /// <remarks>Note that TilePlusBase has the property 'NoActionRequiredCustomGuiReturn' which is
    /// a do-nothing return value. That is, don't refresh, save the scene, or update any fields.</remarks>
    public readonly struct CustomGuiReturn
    {
        /// <summary>
        /// Refresh the tile?
        /// </summary>
        public readonly bool m_Refresh;

        /// <summary>
        /// Save the scene?
        /// </summary>
        public readonly bool m_DelaySceneSave;

        /// <summary>
        /// Any modified fields? If the field had a TPT tag then put the field name here so that inspectors will refresh.
        /// </summary>
        public readonly string m_ModifiedField;



        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="refresh">cause a tile refresh</param>
        /// <param name="delaySceneSave">cause a delayed scene save</param>
        /// <param name="modifiedField">specify any modified field. If null, string.Empty is substituted.</param>
        public CustomGuiReturn(bool refresh, bool delaySceneSave = false, string modifiedField = null)
        {
            m_Refresh        = refresh;
            m_DelaySceneSave = delaySceneSave;
            m_ModifiedField  = modifiedField ?? string.Empty;

        }
    }
}
