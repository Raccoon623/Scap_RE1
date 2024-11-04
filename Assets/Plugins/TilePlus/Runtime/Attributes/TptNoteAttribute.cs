// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 11-08-2021
// ***********************************************************************
// <copyright file="TpNoteAttribute.cs" company="Jeff Sasmor">
//     Copyright (c) 2021 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System;

namespace TilePlus
{
    /// <summary>
    /// Add a note to a field or method. Choice of embedding a string
    /// or using a property to provide the string. Always displayed as
    /// a 'helpbox' above the field or method button.
    ///  </summary>
    /// <remarks>Selection Inspector ONLY, ALWAYS hidden in Play mode</remarks>
    public class TptNoteAttribute : Attribute
    {
        /// <summary>
        /// true if a property is used to provide the note. 
        /// </summary>
        public readonly bool m_UseProperty;
        /// <summary>
        /// The name of the property providing the string or the string if m_UseProperty is false.
        /// </summary>
        public readonly string m_NoteOrProperty;

        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="useProperty">If true, 'noteOrProperty' is the name of a public property that will provide the string to display</param>
        /// <param name="noteOrProperty">The note or the name of a property that will provide the note.</param>
        /// 
        public TptNoteAttribute(bool useProperty, string noteOrProperty)
        {
            m_NoteOrProperty     = noteOrProperty;
            m_UseProperty        = useProperty;
        }
        
        
    }
}
