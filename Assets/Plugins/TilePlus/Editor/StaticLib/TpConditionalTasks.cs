// ***********************************************************************
// Assembly         : TilePlus.Editor
// Author           : Jeff Sasmor
// Created          : 02-01-2023
//
// Last Modified By : Jeff Sasmor
// Last Modified On : 02-01-2023
// ***********************************************************************
// <copyright file="TpConditionalTasks.cs" company="Jeff Sasmor">
//     Copyright (c) 2023 Jeff Sasmor. All rights reserved.
// </copyright>
// <summary>Conditional tasks for Editor-only use</summary>
// ***********************************************************************
#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace TilePlus.Editor
{
    /// <summary>
    /// A simple task manager to run delayed tasks with conditions,  and w/o coroutines.
    /// Note that in-editor, the Async is updated in time with EditorApplication.update but
    /// when PLAYING (even in editor) it occurs right after the normal Update. Usually
    /// works out fine, but must be aware of this.
    ///
    /// Note that tasks launched from here always use Application.exitCancellationToken. 
    /// The polling rate has a big impact.
    /// The default polling rate is 10 (that's the default value in the method call),
    /// which means that the condition is polled once every 10 frames.
    /// It should be observed that in the editor, EditorApplication.update is called A LOT.
    /// For example, on this dev's computer, that's about 800 Hz. 
    /// </summary>
   [InitializeOnLoad]
    public static class TpConditionalTasks
    {
        static TpConditionalTasks()
        {
            s_ConditionalCallbackIndex = 1;  //zero isn't used.
        }

        
        //this method is invoked when the Editor begins play mode
        /// due to the InitializeOnEnterPlayMode attribute.
        [InitializeOnEnterPlayMode] 
        private static void ResetOnEnterPlayMode(EnterPlayModeOptions playModeOptions)
        {
            if((playModeOptions & EnterPlayModeOptions.DisableDomainReload) != 0)
                Reset();
        }
        
        
        
        /// <summary>
        /// Reset and kill all tasks.
        /// </summary>
        [InitializeOnLoadMethod]
        public static void Reset()
        {
            int num;
            if ((num = s_ActiveConditionalCallbacks.Count) == 0)
                return;
            Debug.LogWarning($"Found {num} pending conditional callbacks (not an error): ");
            foreach (var cb in s_ActiveConditionalCallbacks.Values)
            {
                Debug.LogWarning(cb.Info);
                cb.Kill = true;
            }
        }
        
        #region private fields
        private static readonly Dictionary<ulong, TaskRunner> s_ActiveConditionalCallbacks = new();
        private static ulong s_ConditionalCallbackIndex;
        #endregion
        
        #region enum
        /// <summary>
        /// This is returned by the condition-tester to indicate how to proceed.
        /// </summary>
        public enum ContinuationResult
        {
            /// <summary>
            /// Exit because condition would cause null-ref exception like parent is null or other exception.
            /// </summary>
            Exit,
            /// <summary>
            /// Exit because condition timed-out
            /// </summary>
            Timeout,
            /// <summary>
            /// Exit because condition was met on entry
            /// </summary>
            ConditionSatisfiedOnEntry,
            /// <summary>
            /// Condition not satisfied
            /// </summary>
            Continue,
            /// <summary>
            /// Exit due to exception.
            /// </summary>
            Exception,
            /// <summary>
            /// Condition Satisfied, execute callback.
            /// </summary>
            Exec
        }
        
        #endregion

        #region taskCreation
        
        
        /// <summary>
        /// Execute a callback that's invoked after a condition is satisfied.  
        /// </summary>
        /// <param name = "parent" >Parent UnityEngine.Object. If non-null, checks for null before callback invoked.</param>
        /// <param name="actionOnComplete">Action to exec when the condition completes before the taskTimeoutFrames</param>
        /// <param name="condition">The condition as a Func that returns true when it is time to exec the Action.</param>
        /// <param name="info">Info for messages/debug</param>
        /// <param name="taskTimeoutFrames">Cancel without executing after this number of FRAMES (not msec).</param>
        /// <param name = "pollingInterval" >How often to poll the condition. Defaults to once every 10 frames. Acceptable: >= 10</param>
        /// <param name = "silent" >if true, no messages except errors</param>
        /// <returns>ID (or index #) of this callback instance. Can be used to lookup the taskRunner instance.</returns>
        /// <remarks>If the taskTimeoutFrames is exceeded then the Action is not executed.
        /// Also note that execution of the action occurs just AFTER monobehaviour update.
        /// thru the playerloop (Play mode) or the next EditorApplication.update invocation.
        /// Also: the frame count increments every #pollingInterval entries into the task runner by #pollingInterval.
        /// IE if the polling interval is 10 (default) then the internal FrameCount increments by 10 each time.
        /// </remarks>
        public static ulong ConditionalDelayedCallback(UnityEngine.Object? parent,
                                                       Action<ContinuationResult>?            actionOnComplete,
                                                       Func<int, bool>    condition,
                                                       string             info,
                                                       int                pollingInterval   = 10,
                                                       int                taskTimeoutFrames = 1000000, //changed in 2.1 int.MaxValue,
                                                       bool               silent            = false)
        {
            
            if (pollingInterval < 10)
               pollingInterval = 10;

            if (actionOnComplete == null)
            {
                TpLib.TpLogError($"Null actionOnComplete passed to ConditionalDelayedCallback! {info}. Task ignored");
                return 0;
            }
            
            var taskRunner = new TaskRunner(parent,actionOnComplete, condition, taskTimeoutFrames, pollingInterval, info,silent);
            if(parent != null)
                s_ActiveConditionalCallbacks.Add(taskRunner.Id, taskRunner);
            
            if (!silent && TpLibEditor.Informational)
                TpLib.TpLog($"Conditional Task push: {info} Polling interval: {pollingInterval}, ID: {taskRunner.Id} ");
            
            taskRunner.Exec();
            return taskRunner.Id;
        }
        
        #endregion

        #region utility
        
        /// <summary>
        /// Does the current object with Instance Id id have a conditional task running?
        /// </summary>
        /// <param name="id">ID returned from ConditionalDelayedCallback </param>
        /// <returns>true/false</returns>
        public static bool IsActiveConditionalTask(ulong id)
        {
            return s_ActiveConditionalCallbacks.ContainsKey(id);
        }
        
        /// <summary>
        /// Does the current object have a conditional task running?
        /// </summary>
        /// <param name="obj">A UnityEngine.Object instance</param>
        /// <returns>o for no such task or a ulong ID for the task</returns>
        /// <remarks>MUCH faster to use IsActiveConditionalTask if id is available.</remarks>
        public static ulong IsActiveConditionalTask(UnityEngine.Object? obj)
        {
            if (obj == null)
                return 0;
            foreach (var item in s_ActiveConditionalCallbacks.Values)
            {
                if (item.Parent == obj)
                    return item.Id;
            }

            return 0;
        }

        /// <summary>
        /// Kill all conditional tasks for this object. 
        /// </summary>
        /// <param name="obj">UnityEngine.Object</param>
        /// <returns>false if the passed-in obj is null or not a UnityEngine.Object</returns>
        /// <remarks>If you already have the IDs then calling KillConditionalTask(id) is faster. </remarks>
        public static void KillConditionalTasksForObject(UnityEngine.Object? obj)
        {
            if (obj == null)
                return;
            
            var ids = new List<ulong>(8);
            foreach (var item in s_ActiveConditionalCallbacks.Values)
            {
                if (item.Parent == obj)
                    ids.Add(item.Id);
            }

            if (ids.Count == 0)
                return;
            foreach (var id in ids)
                KillConditionalTask(id);

        }

        /// <summary>
        /// Get the taskRunner object for a task. May return null if it no longer exists (task complete) 
        /// </summary>
        /// <param name="id">Index value returned by ConditionalDelayedCallback</param>
        /// <param name = "instance" >taskRunner instance. If return of method is false then this is null</param>
        /// <returns>true if found.</returns>
        // ReSharper disable once MemberCanBePrivate.Global
        public static bool GetTaskRunner(ulong id, out TaskRunner? instance )
        {
            return s_ActiveConditionalCallbacks.TryGetValue(id, out instance);
        }

        /// <summary>
        /// Kill a conditional task.
        /// </summary>
        /// <param name="id"></param>
        // ReSharper disable once MemberCanBePrivate.Global
        public static void KillConditionalTask(ulong id)
        {
            if(id == 0)
                return;

            if (!GetTaskRunner(id, out var taskRunner))
            {
                if(TpLibEditor.Warnings)
                    TpLib.TpLogWarning($"Can't kill task with id {id}, not found");
                return;
            }

            if (taskRunner != null)
            {
                if(TpLibEditor.Warnings)
                    TpLib.TpLogWarning($"Killed task with id {id}");
                taskRunner.Kill = true;
            }
        }

        /// <summary>
        /// Returns a status string
        /// </summary>
        public static string ConditionalTaskInfo
        {
            get
            {
                var s       = string.Empty;
                var proxies = s_ActiveConditionalCallbacks.Values.ToList();
                var num     = proxies.Count;
                if (num == 0)
                    return "-None-";
                var limit = num < 50
                                ? num
                                : 50;
                for(var i = 0; i < limit; i++)
                {
                    var p = proxies[i];
                    s += $"ID {p.Id} Info: {p.Info} FrameCount: {p.FrameCount}\n";
                }

                return s;
            }
        }
        
        #endregion
        

        #region taskRunnerClass
        /// <summary>
        /// An instance of this class is created for every entry to ConditionalDelayedCallback. Unpooled.
        /// </summary>
        public class TaskRunner
        {
            

            /// <inheritdoc />
            public override string ToString()
            {
                return $"TaskRunner: info : {Info}";
            }

            /// <summary>
            /// The Object which called ConditionalDelayedCallback. Can be null.
            /// </summary>
            public UnityEngine.Object? Parent { get; }
            /// <summary>
            /// The Action to execute when the Condition returns true
            /// </summary>
            private Action<ContinuationResult>         ActionOnComplete      { get; }
            /// <summary>
            /// The Condition to test. Should return true when satisfied
            /// </summary>
            private Func<int,bool> Condition             { get; }
            /// <summary>
            /// If the Parent is ! null then we should re-test that that's still true prior to execing the ActionOnComplete
            /// </summary>
            private bool           TestParentForNull     { get; } //if true this means parent!=null
            /// <summary>
            /// Maximum number of frames before cancelling this task
            /// </summary>
            private int            TaskTimeoutFramesTime { get; }
            
            /// <summary>
            /// How often to poll the Condition
            /// </summary>
            private int TaskPollingInterval { get; }

            /// <summary>
            /// Inhibit info messages
            /// </summary>
            private bool   Silent { get; }
            /// <summary>
            /// An informational message identifying the caller.
            /// </summary>
            public  string Info   { get; }

            /// <summary>
            /// The Index of this task. Internally generated
            /// </summary>
            public ulong Id { get; }

            /// <summary>
            /// Count of frames since task launched
            /// </summary>
            public  int FrameCount { get; private set; }
            
            /// <summary>
            /// Kill flag
            /// </summary>
            public bool Kill { private get; set; }

            private ContinuationResult result;

            /// <summary>
            /// Runs task spawned from ConditionalDelayedCallback
            /// </summary>
            /// <param name = "parent" >Parent Object if appropriate. Use null for static classes.</param>
            /// <param name="actionOnComplete">ActionOnComplete to invoke when condition is met</param>
            /// <param name="condition">Condition to test</param>
            /// <param name="taskTimeoutFrames">Timeout after this number of UPDATES/FRAMES</param>
            /// <param name = "taskPollingInterval" >See description in ConditionalDelayedCallback()</param>
            /// <param name = "info" >Info string for debug</param>
            /// <param name = "silent" >if true, no messages except errors</param>
            public TaskRunner(UnityEngine.Object? parent,
                                 Action <ContinuationResult>            actionOnComplete,
                                 Func<int, bool>    condition,
                                 int                taskTimeoutFrames,
                                 int                taskPollingInterval,
                                 string             info,
                                 bool               silent)
            {
                TestParentForNull     = parent != null;
                Parent                = parent;
                ActionOnComplete      = actionOnComplete;
                Condition             = condition;
                TaskTimeoutFramesTime = taskTimeoutFrames;
                TaskPollingInterval   = taskPollingInterval >= 10 ? taskPollingInterval : 10;
                Info                  = $"{info}: timeout: {taskTimeoutFrames} polling: {TaskPollingInterval}";
                Silent                = silent;
                Id                    = s_ConditionalCallbackIndex++;
            }

            /// <summary>
            /// Execute this task
            /// </summary>
            public async void Exec()
            {
                // ReSharper disable once RedundantAssignment
                var initialTest = false;
                try
                {
                    initialTest = Condition(0);
                }
                catch (Exception e)
                {
                    if (s_ActiveConditionalCallbacks.ContainsKey(Id))
                        s_ActiveConditionalCallbacks.Remove(Id);
                    TpLib.TpLogError($"Initial test of Conditional Delayed callback ID [{Id}] for [{Info}] had an exception: {e}. Exiting... Task not started");
                    ActionOnComplete(ContinuationResult.Exception);
                    return;
                }
                
                //condition could already be satisfied
                if(!initialTest) 
                {
                    result = ContinuationResult.Exec;
                    
                    result = await Task.Run(TestCondition, Application.exitCancellationToken);

                    if (s_ActiveConditionalCallbacks.ContainsKey(Id))
                        s_ActiveConditionalCallbacks.Remove(Id);

                    if (result != ContinuationResult.Exec)
                    {
                        if (result == ContinuationResult.Timeout)
                        {
                            if (TpLibEditor.Informational)
                                TpLib.TpLog($"Exit due to timeout. {Info} Frames: {FrameCount}  Cont Result {result.ToString()}");
                            ActionOnComplete(ContinuationResult.Timeout);
                        }
                        else
                        {
                            if (TpLibEditor.Informational)
                                TpLib.TpLogError($"Abnormal Exit: task {Info} SKIPPED -- NFrames: [{FrameCount}]  Cont result {result.ToString()}, ID [{Id}] ");
                            ActionOnComplete(result);
                        }

                        return;
                    }
                    if (!Silent)
                    {
                        if (TpLibEditor.Informational)
                            TpLib.TpLog($"TaskManager normal exit: Info:[{Info}] NFrames: [{FrameCount}] ID [{Id}]  : Exec callback...");
                    }
                }
                else
                {
                    if (s_ActiveConditionalCallbacks.ContainsKey(Id))
                        s_ActiveConditionalCallbacks.Remove(Id);
                    if (!Silent)
                    {
                        if (TpLibEditor.Informational)
                            TpLib.TpLog($"TaskManager early exit: Exit condition met on entry Info:[{Info}] ID [{Id}], Cont result {result.ToString()}, NFrames: [{FrameCount}]");
                        ActionOnComplete(ContinuationResult.ConditionSatisfiedOnEntry);
                    }
                }
                try
                {
                    ActionOnComplete(ContinuationResult.Exec);
                }
                catch (NullReferenceException e)
                {
                    TpLib.TpLogError($"Conditional Delayed callback ID [{Id}] for [{Info}] had a null-ref exception: {e}");
                }
            }

            private async Task<ContinuationResult> TestCondition()
            {
                ContinuationResult r;
                do
                {
                    //note interval is forced to be 10 or greater.
                    for (var i = 0; i < TaskPollingInterval; i++)
                        await Task.Yield();
                    FrameCount += TaskPollingInterval;
                    r = Continuation();
                }
                while ( r == ContinuationResult.Continue);

                return r;

            }

            private ContinuationResult Continuation()
            {
                if (Kill)
                {
                    if (Silent)
                        return ContinuationResult.Exit;
                    if (TpLibEditor.Informational)
                        TpLib.TpLog($"TaskManager Early exit: Conditional task ID: [{Id}] was killed. Info:[{Info}]");
                    return ContinuationResult.Exit;
                }

                var exitOnNull = TestParentForNull && Parent == null;
                var exitOnTimeout = FrameCount > TaskTimeoutFramesTime; 
                // ReSharper disable once InvertIf
                if (exitOnNull || exitOnTimeout )
                {
                    if (!Silent)
                    {
                        if (TpLibEditor.Informational)
                            TpLib.TpLog($"TaskManager Early exit: caller became null? [{exitOnNull}]  or timeout [{exitOnTimeout}]. Info:[{Info}] ID [{Id}] ");
                    }
                    return exitOnNull ? ContinuationResult.Exit : ContinuationResult.Timeout; //we're done, parent is null or timeout or task was killed.
                }

                try
                {
                    return Condition(FrameCount)
                               ? ContinuationResult.Exec
                               : ContinuationResult.Continue;
                }
                catch (Exception e)
                {
                    TpLib.TpLogError($"Conditional Delayed callback ID [{Id}] for [{Info}] had an exception: {e}. Exiting...");
                    return ContinuationResult.Exit;
                }
                
            }
        }
        #endregion
        

    }
}
