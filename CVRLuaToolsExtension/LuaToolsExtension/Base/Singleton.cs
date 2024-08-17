using UnityEngine;

namespace NAK.CVRLuaToolsExtension;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new();
    private static bool _isShuttingDown;

    public static T Instance
    {
        get
        {
            if (_isShuttingDown)
            {
                Debug.LogWarning($"[Singleton] Instance of {typeof(T)} already destroyed. Returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance != null) return _instance;
                _instance = (T)FindObjectOfType(typeof(T));
                if (_instance != null) return _instance;
                GameObject singletonObject = new($"{typeof(T).Name} (Singleton)");
                _instance = singletonObject.AddComponent<T>();
                DontDestroyOnLoad(singletonObject);
                return _instance;
            }
        }
    }

    private void OnApplicationQuit()
    {
        _isShuttingDown = true;
    }

    private void OnDestroy()
    {
        if (_instance == this) 
            _isShuttingDown = true;
    }
}