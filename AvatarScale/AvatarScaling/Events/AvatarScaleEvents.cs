using NAK.AvatarScaleMod.Components;

namespace NAK.AvatarScaleMod.AvatarScaling;

public static class AvatarScaleEvents
{
    #region Local Avatar Scaling Events
    
    /// <summary>
    /// Invoked when the local avatar's height changes for any reason.
    /// </summary>
    public static readonly AvatarScaleEvent<LocalScaler> OnLocalAvatarHeightChanged = new();
    
    /// <summary>
    /// Invoked when the local avatar's animated height changes.
    /// </summary>
    public static readonly AvatarScaleEvent<LocalScaler> OnLocalAvatarAnimatedHeightChanged = new();
    
    /// <summary>
    /// Invoked when the local avatar's target height changes.
    /// </summary>
    public static readonly AvatarScaleEvent<LocalScaler> OnLocalAvatarTargetHeightChanged = new();

    /// <summary>
    /// Invoked when the local avatar's height is reset.
    /// </summary>
    public static readonly AvatarScaleEvent<LocalScaler> OnLocalAvatarHeightReset = new();
    
    #endregion
    
    #region Avatar Scaling Events
    
    /// <summary>
    /// Invoked when a remote avatar's height changes.
    /// </summary>
    public static readonly AvatarScaleEvent<string, NetworkScaler> OnRemoteAvatarHeightChanged = new();
    
    /// <summary>
    /// Invoked when a remote avatar's height is reset.
    /// </summary>
    public static readonly AvatarScaleEvent<string, NetworkScaler> OnRemoteAvatarHeightReset = new();

    #endregion

    #region Event Classes

    public class AvatarScaleEvent<T>
    {
        private Action<T> _listener = arg => { };

        public void AddListener(Action<T> listener) => _listener += listener;
        public void RemoveListener(Action<T> listener) => _listener -= listener;

        public void Invoke(T arg)
        {
            var invokeList = _listener.GetInvocationList();
            foreach (Delegate method in invokeList)
            {
                if (method is not Action<T> action)
                    continue;

                try
                {
                    action(arg);
                }
                catch (Exception e)
                {
                    AvatarScaleMod.Logger.Error($"Unable to invoke listener, an exception was thrown and not handled: {e}.");
                }
            }
        }
    }

    public class AvatarScaleEvent<T1, T2>
    {
        private Action<T1, T2> _listener = (arg1, arg2) => { };

        public void AddListener(Action<T1, T2> listener) => _listener += listener;
        public void RemoveListener(Action<T1, T2> listener) => _listener -= listener;

        public void Invoke(T1 arg1, T2 arg2)
        {
            var invokeList = _listener.GetInvocationList();
            foreach (Delegate method in invokeList)
            {
                if (method is not Action<T1, T2> action)
                    continue;

                try
                {
                    action(arg1, arg2);
                }
                catch (Exception e)
                {
                    AvatarScaleMod.Logger.Error($"Unable to invoke listener, an exception was thrown and not handled: {e}.");
                }
            }
        }
    }

    #endregion
}