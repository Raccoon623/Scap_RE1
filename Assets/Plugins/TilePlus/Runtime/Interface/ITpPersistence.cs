// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 07-29-2021
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-02-2022
// ***********************************************************************
// <copyright file="ITpPersistence.cs" company="Jeff Sasmor">
//     Copyright (c) 2022 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
namespace TilePlus
{

    /// <summary>
    /// this is used to detect tiles that want to use this
    /// persistence scheme. It allows looking for tiles
    /// that implement this base interface without having
    /// to specify the in and out types.
    /// </summary>
    /// <remarks>Don't use as base for anything outside this file.</remarks>
    public interface ITpPersistenceBase { }

    /// <summary>
    /// Interface for built-in data save/restore scheme
    /// </summary>
    /// <typeparam name="TR">The type of the data returned by GetSaveData.</typeparam>
    /// <typeparam name="T">The type of the data sent to RestoreSaveData</typeparam>
    /// <remarks>Note that this is very similar to ISendMessage and
    /// uses the same abstract MessagePacket class as a data object.</remarks>
    public interface ITpPersistence<out TR, in T> : ITpPersistenceBase where T:MessagePacket<T> where TR:MessagePacket<TR> 
   {
        /// <summary>
        /// Implement to provide data to save
        /// </summary>
        /// <returns>TR.</returns>
        TR GetSaveData(object options = null);

        /// <summary>
        /// Implement to be sent data to restore
        /// </summary>
        /// <param name="dataToRestore">The data to restore.</param>
        void RestoreSaveData(T dataToRestore);
   }

}
