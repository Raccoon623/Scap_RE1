// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 05-04-2021
// ***********************************************************************
// <copyright file="TptShowMethodAsButtonAttribute.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
namespace TilePlus
{
    /// <summary>
    /// This attribute is used to display an invoke button for a method.
    /// Note that if the access type for the method isn't public or
    /// protected then it'll be ignored.
    /// </summary>
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class TptShowMethodAsButtonAttribute : TptAttributeBaseAttribute
    {
        /// <summary>
        /// Optional message to show in HelpBox just above the button.
        /// </summary>
        public readonly string m_Message;

        /// <summary>
        /// Create ShowMethodAsButton Attr
        /// </summary>
        /// <param name="message">Message to show the user</param>
        /// <param name="spaceMode">spaces or not</param>
        /// <param name="showMode">when-to-show control</param>
        /// <param name="visibilityProperty">string with property name when using a prop to control showing</param>
        /// <inheritdoc />
        public TptShowMethodAsButtonAttribute(string    message            = null,
                                              SpaceMode spaceMode          = SpaceMode.None,
                                              ShowMode  showMode           = ShowMode.Always,
                                              string    visibilityProperty = "")
        {
            m_SpaceMode          = spaceMode;
            m_ShowMode           = showMode;
            m_VisibilityProperty = visibilityProperty;
            if (message != null)
                m_Message = message;
        }
    }
}
