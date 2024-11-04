// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 01-25-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 02-10-2023
// ***********************************************************************
// <copyright file="DOTweenPools.cs" company="">
//     Copyright (c)2023 Jeff Sasmor . All rights reserved.
// </copyright>
// <summary></summary>
// ***********************************************************************
#if TPT_DOTWEEN || DOTWEEN
#nullable enable

using System.Collections.Generic;
using DG.Tweening;
using UnityEngine.Pool;

namespace TilePlus
{

    // ReSharper disable once InconsistentNaming
    /// <summary>
    /// Pooling for DOTween adapter callback class instances
    /// </summary>
    public static class DOTweenPools
    {

        private static readonly ObjectPool<AugmentedDOTweenCallback> s_CallbacksPool =
            new(()=>new AugmentedDOTweenCallback(),
                null,
                (callback) => callback.Reset(),
                null,true,8);

        /// <summary>
        /// Get an AugmentedDOTweenCallback instance from the pooler
        /// </summary>
        public static AugmentedDOTweenCallback GetCallback => s_CallbacksPool.Get();

        /// <summary>
        /// Return an AugmentedDOTweenCallback to the pooler
        /// </summary>
        /// <param name="callback"></param>
        public static void ReturnCallback(AugmentedDOTweenCallback? callback)
        {
            if(callback != null)
                s_CallbacksPool.Release(callback);
        }

        /// <summary>
        /// Return a list of AugmentedDOTweenCallback to the pooler
        /// </summary>
        /// <param name="callbacks"></param>
        public static void ReturnCallbacks(List<AugmentedDOTweenCallback> callbacks)
        {
            var num = callbacks.Count;
            if (num == 0)
                return;
            for(var i = 0; i < num; i++)
                s_CallbacksPool.Release(callbacks[i]);
        }


        private static readonly ObjectPool<List<Sequence>> s_SequencePool = 
            new(() => new List<Sequence>(2),
                null,
                (list) => list.Clear(),
                null, true, 8);

        /// <summary>
        /// Get a sequence list from the pool.
        /// </summary>
        public static List<Sequence> GetSequenceList => s_SequencePool.Get();

        /// <summary>
        ///return a sequence list to the pool
        /// </summary>
        /// <param name="returnedList">the sequence list to return to pool</param>
        public static void ReturnSequenceList(List<Sequence>? returnedList)
        {
            if(returnedList != null)
                s_SequencePool.Release(returnedList);
        }

        private static readonly ObjectPool<List<Tweener>> s_TweenPool = 
            new(() => new List<Tweener>(2),
                null,
                (list) => list.Clear(),
                null, true, 8);


        /// <summary>
        /// Get a tweener list from the pool
        /// </summary>
        public static List<Tweener> GetTweenerList => s_TweenPool.Get();

        /// <summary>
        /// Return a tweener list to the pool
        /// </summary>
        /// <param name="returnedList">the list to return to the pool</param>
        public static void ReturnTweenerList(List<Tweener>? returnedList)
        {
            if(returnedList != null)
                s_TweenPool.Release(returnedList);
        }
        
    }
    
    /// <summary>
    /// Used to hijack a DOTween callback for tweens/sequences
    /// </summary>
    // ReSharper disable once InconsistentNaming
    public class AugmentedDOTweenCallback 
    {
        /// <summary>
        /// The original callback
        /// </summary>
        public TweenCallback? m_Original;
        /// <summary>
        /// reference to tweener (can be null)
        /// </summary>
        public Tweener? m_Tweener;
        /// <summary>
        /// reference to sequence (can be null)
        /// </summary>
        public Sequence? m_Sequence;
        /// <summary>
        /// reference to the TpDOTween instance that created this instance of AugmentedDOTweenCallback
        /// </summary>
        public DOTweenAdapter? m_ThisTpDoTweenInstance;

        
        /// <summary>
        /// Reset this instance: used by pooler
        /// </summary>
        public void Reset()
        {
            m_Original              = null;
            m_Tweener               = null;
            m_Sequence              = null;
            m_ThisTpDoTweenInstance = null;
        }
        
        /// <summary>
        /// OnComplete handler for tween or sequence
        /// </summary>
        public void OnComplete()
        {
            if(m_ThisTpDoTweenInstance == null)
               return;
            if (m_Tweener != null)
                m_ThisTpDoTweenInstance.DeleteTween(m_Tweener);
            else
                m_ThisTpDoTweenInstance.DeleteSequence(m_Sequence);
            m_Original?.Invoke(); //moved here.
            m_Original = null;
            m_Tweener  = null;
            m_Sequence = null;
            m_ThisTpDoTweenInstance.RemoveFromCallbackList(this);
            
        }
    }
}
#endif
