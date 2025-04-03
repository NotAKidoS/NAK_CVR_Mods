using JetBrains.Annotations;
using UnityEngine;

namespace NAK.BetterContentLoading.Util;

[PublicAPI]
public static class ThreadingHelper
{
    private static readonly SynchronizationContext _mainThreadContext;

    static ThreadingHelper()
    {
        _mainThreadContext = SynchronizationContext.Current;
    }
    
    public static bool IsMainThread => SynchronizationContext.Current == _mainThreadContext;

    /// <summary>
    /// Runs an action on the main thread. Optionally waits for its completion.
    /// </summary>
    public static void RunOnMainThread(Action action, bool waitForCompletion = true, Action<Exception> onError = null)
    {
        if (SynchronizationContext.Current == _mainThreadContext)
        {
            // Already on the main thread
            TryExecute(action, onError);
        }
        else
        {
            if (waitForCompletion)
            {
                ManualResetEvent done = new(false);
                _mainThreadContext.Post(_ =>
                {
                    TryExecute(action, onError);
                    done.Set();
                }, null);
                done.WaitOne(50000); // Block until action is completed
            }
            else
            {
                // Fire and forget (don't wait for the action to complete)
                _mainThreadContext.Post(_ => TryExecute(action, onError), null);
            }
        }
    }
    
    /// <summary>
    /// Runs an action asynchronously on the main thread and returns a Task.
    /// </summary>
    public static Task RunOnMainThreadAsync(Action action, Action<Exception> onError = null)
    {
        if (SynchronizationContext.Current == _mainThreadContext)
        {
            // Already on the main thread
            TryExecute(action, onError);
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource<bool>();
        _mainThreadContext.Post(_ =>
        {
            try
            {
                TryExecute(action, onError);
                tcs.SetResult(true);
            }
            catch (Exception ex)
            {
                tcs.SetException(ex);
            }
        }, null);
        return tcs.Task;
    }

    /// <summary>
    /// Runs a task on a background thread with cancellation support.
    /// </summary>
    public static async Task RunOffMainThreadAsync(Action action, CancellationToken cancellationToken, Action<Exception> onError = null)
    {
        try
        {
            await Task.Run(() =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                TryExecute(action, onError);
            }, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning("Task was canceled.");
        }
    }

    /// <summary>
    /// Helper method for error handling.
    /// </summary>
    private static void TryExecute(Action action, Action<Exception> onError)
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
        }
    }
}
