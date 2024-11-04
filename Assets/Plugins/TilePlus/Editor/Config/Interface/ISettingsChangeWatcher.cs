// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 01-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 12-18-2022
// ***********************************************************************
// <copyright file="ISettingsChangeWatcher.cs" company="Jeff Sasmor">
//     Copyright (c) Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Interface for notification about Settings changes in TilePlusConfig</summary>
// ***********************************************************************

namespace TilePlus.Editor
{
    /// <summary>
    /// Interface for notification of TilePlusConfig and TilePlusPainterConfig setting changes.
    /// </summary>
        public interface ISettingsChangeWatcher
    {
        /// <summary>
        /// Called when TilePlusConfig settings change.
        /// </summary>
        /// <param name="change">what changed</param>
        /// <param name="changes">class instance with old and new values as object</param>
        void OnSettingsChange(string change, ConfigChangeInfo changes);

    }
}
