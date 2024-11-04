#if TPT_DOTWEEN || DOTWEEN
// ***********************************************************************
// Assembly         : TilePlus
// Author           : Jeff Sasmor
// Created          : 01-31-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 01-31-2023
// ***********************************************************************
// <copyright file="DOTweenAdapter.cs" company="Jeff Sasmor">
//     Copyright (c) 2023 Jeff Sasmor. All rights reserved.
// </copyright>
// ***********************************************************************
using System.Collections.Generic;
using DG.Tweening;

#nullable enable

namespace TilePlus
{
    /// <summary>
    /// Per-tile DOTween controller.
    /// Tiles: create a field and instance
    ///
    /// private DotweenAdapter dtAdapter = new();
    /// Note that a Tile script's OnDisable should be used to call
    /// dtAdapter.OnDisableHandler().
    ///
    /// That's especially important if Dotween's safe mode isn't in use.
    /// It also returns pooled items to their pools so there aren't memory leaks.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    
    public class DOTweenAdapter
    {
        
        #region privatefields
        //all tweeners used by this tile
        private List<Tweener>? tweeners;
        //all sequences used by this tile
        private List<Sequence>? sequences;

        private readonly List<AugmentedDOTweenCallback> activeCallbacks = new();
        
        #endregion
        
        #region publicproperties

        /// <summary>
        /// Returns a list of tweeners. Can be null.
        /// </summary>
        /// <remarks>DO NOT hold references to this list in your code
        /// as the pooling won't work correctly. </remarks>
        public List<Tweener>? Tweeners => tweeners;

        /// <summary>
        /// Returns a list of sequences. Can be null.
        /// </summary>
        /// <remarks>DO NOT hold references to this list in your code
        /// as the pooling won't work correctly. </remarks>
        public List<Sequence>? Sequences => sequences;

        /// <summary>
        /// How many active augmented callbacks
        /// </summary>
        public int ActiveAugCallbacks => activeCallbacks.Count;
        
        #endregion
        
        #region cleanup
        
        /// <summary>
        /// Remove a callback instance from the active callbacks list.
        /// </summary>
        /// <param name="callback"></param>
        public void RemoveFromCallbackList(AugmentedDOTweenCallback callback)
        {
            activeCallbacks.Remove(callback);
            DOTweenPools.ReturnCallback(callback);
        }
        
        /// <summary>
        /// Tiles using this adapter must call this method in the OnDisable handler.
        /// </summary>
        public void OnDisableHandler()
        {
            KillAll();
            if (sequences != null)
                DOTweenPools.ReturnSequenceList(sequences);
            sequences = null;
            if(tweeners != null)
                DOTweenPools.ReturnTweenerList(tweeners);
            tweeners = null;
        }
        
        
        /// <summary>
        /// Kills all sequences and tweens for a tile.
        /// Releases tweener and/or sequences lists to the host's pooler.
        /// </summary>
        // ReSharper disable once MemberCanBePrivate.Global
        public void KillAll(bool complete = false)
        {
            //NB - null checks are important since KillAll could potentially be
            //called when tweeners and/or sequences = null; e.g. no sequences used.
            //Copies of list are used because if complete=true then the tweeners list
            //is modified by the action of the OnComplete methods.
            if (tweeners != null)
            {
                var num          = tweeners.Count;
                if (num != 0)
                {
                    var tweenersCopy = DOTweenPools.GetTweenerList;
                    tweenersCopy.AddRange(tweeners);
                    for (var i = 0; i < num; i++)
                    {
                        if (tweenersCopy[i].IsActive())
                        {
                            if (!complete)
                                tweenersCopy[i].onComplete?.Invoke();
                            tweenersCopy[i].Kill(complete);
                        }
                    }
                    DOTweenPools.ReturnTweenerList(tweenersCopy);
                }
            }
            if (sequences != null)
            {
                var num = sequences.Count;
                if (num != 0)
                {
                    var seqCopy = DOTweenPools.GetSequenceList;
                    seqCopy.AddRange(sequences);
                    for (var i = 0; i < num; i++)
                    {
                        if (seqCopy[i].IsActive())
                        {
                            if (!complete)
                                seqCopy[i].onComplete?.Invoke();
                            seqCopy[i].Kill(complete);
                        }
                    }
                    DOTweenPools.ReturnSequenceList(seqCopy);
                }
                
            }

            DOTweenPools.ReturnCallbacks(activeCallbacks);
            activeCallbacks.Clear();
            

        }
        #endregion
        
        #region tweens

        /// <summary>
        /// Add a tween to this manager (not needed (ie DO NOT) if the tween is part of a sequence)
        /// </summary>
        /// <param name="tweener">the tweener</param>
        /// <param name="autoPlay">play it? default=true</param>
        /// <remarks>if the tween has an OnComplete then it's hijacked to
        /// allow auto-removal from the tweeners list. Otherwise an
        /// OnComplete action is added to also allow auto-removal.</remarks>
        public void AddTween(Tweener tweener, bool autoPlay = true)
        {
            tweeners ??= DOTweenPools.GetTweenerList; //if tweeners list does not exist get one from the tweener-list pool
            tweeners.Add(tweener); //add to the list
          

            //there's already an autocomplete so hijack it
            if (tweener.onComplete != null)
            {
                var callback = DOTweenPools.GetCallback;
                activeCallbacks.Add(callback);
                callback.m_Original              = tweener.onComplete;
                callback.m_Tweener               = tweener;
                callback.m_Sequence              = null;
                callback.m_ThisTpDoTweenInstance = this;
                tweener.OnComplete(callback.OnComplete);

            }
            else //just add an OnComplete
                tweener.OnComplete(() => DeleteTween(tweener));

            if (autoPlay)
                tweener.Play();
        }
    

        /// <summary>
        /// Delete a tweener from this manager
        /// </summary>
        /// <param name="tweener">the tweener to delete</param>
        /// <returns>false if tween could not be removed (it wasn't registered)</returns>
        public bool DeleteTween(Tweener? tweener)
        {
            if (tweener == null || tweeners == null)
                return false;
            var success = tweeners.Remove(tweener);
            tweener.onComplete = null;
            return success;
        }
        #endregion
        
        #region sequences

        /// <summary>
        /// Add a sequence to this manager. Does not auto-play
        /// </summary>
        /// <param name="sequence">a sequence instance</param>
        public void AddSequence(Sequence sequence)
        {
            sequences ??= DOTweenPools.GetSequenceList;
            sequences.Add(sequence);

            if (sequence.onComplete != null)
            {
                var callback = DOTweenPools.GetCallback;
                activeCallbacks.Add(callback);
                callback.m_Original              = sequence.onComplete;
                callback.m_Sequence              = sequence;
                callback.m_Tweener               = null;
                callback.m_ThisTpDoTweenInstance = this;
                sequence.OnComplete(callback.OnComplete);
            }
            else
                sequence.OnComplete(() => DeleteSequence(sequence));
        }

      
        /// <summary>
        /// delete a sequence from this manager.
        /// </summary>
        /// <param name="sequence"></param>
        /// <returns>false if sequence could not be removed (it wasn't registered)</returns>
        /// <remarks>Although public, not for use outside of the plugin</remarks>
        public bool DeleteSequence(Sequence? sequence)
        {
            if (sequence == null || sequences == null)
                return false;
            var success = sequences.Remove(sequence);
            sequence.onComplete = null;
            return success;
            
        }
        
        #endregion
        
        
    
        
        
        
        
    }
    
    
   
    
}


#endif

