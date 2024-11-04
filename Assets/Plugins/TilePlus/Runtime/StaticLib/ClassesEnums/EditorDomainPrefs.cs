// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 03-25-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-16-2023
// ***********************************************************************
// <copyright file="EditorDomainPrefs.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************

#if UNITY_EDITOR
using System;
#nullable enable

namespace TilePlus
{
    /// <summary>
    /// Class EditorDomainPrefs.
    /// </summary>
    [Serializable] 
   public class EditorDomainPrefs
    {
        /// <summary>
        /// Gets a value indicating whether enter play mode options are enabled.
        /// </summary>
        /// <value><c>true</c> if enter play mode options enabled; otherwise, <c>false</c>.</value>
        public bool EnterPlayModeOptionsEnabled   { get; }
        /// <summary>
        /// Gets a value indicating whether domain reload is disabled.
        /// </summary>
        /// <value><c>true</c> if domain reload disabled; otherwise, <c>false</c>.</value>
        public bool DisableDomainReload           { get; }
        /// <summary>
        /// Gets a value indicating whether scene reloading is disabled.
        /// </summary>
        /// <value><c>true</c> if scene reload disabled; otherwise, <c>false</c>.</value>
        public bool DisableSceneReload            { get; }
        /* NB this was deprecated in 2022.2
         * /// <summary>
        /// Gets a value indicating whether scene backup unless dirty is disabled.
        /// </summary>
        /// <value><c>true</c> if scene backup unless dirty is disabled; otherwise, <c>false</c>.</value>
        public bool DisableSceneBackupUnlessDirty { get; }*/

        /// <summary>
        /// Initializes a new instance of the <see cref="EditorDomainPrefs"/> class.
        /// </summary>
        /// <param name="optsEnabled">if set to <c>true</c> [opts enabled].</param>
        /// <param name="disableDomainReload">if set to <c>true</c> [disable domain reload].</param>
        /// <param name="disableSceneReload">if set to <c>true</c> [disable scene reload].</param>
        public EditorDomainPrefs(bool optsEnabled, bool disableDomainReload, bool disableSceneReload)
        {
            this.EnterPlayModeOptionsEnabled   = optsEnabled;
            this.DisableDomainReload           = disableDomainReload;
            this.DisableSceneReload            = disableSceneReload;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var s = $"EnterPlayMode Options {(EnterPlayModeOptionsEnabled ? "Enabled" : "Disabled")} ::: "
                    + $"{(DisableDomainReload ? "Disable" : "Enable")} Domain reload, ";
            s += $"{(DisableSceneReload ? "Disable" : "Enable")} Scene reload, ";
            return s;
        }
    }
}
#endif
