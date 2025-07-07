using UnityEngine;

public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static bool IsAlive => !_shuttingDown && _instance != null;

    private static T _instance;
    private static bool _shuttingDown = false;
    private static object _lock = new object();

    public static T Instance
    {
        get
        {
            if (_shuttingDown)
            {
                Debug.LogWarning($"[Singleton] Instance '{typeof(T)}' already destroyed. Returning null.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    _instance = (T)FindObjectOfType(typeof(T));
                    if (_instance == null)
                    {
                        Debug.LogError($"[Singleton] Instance of {typeof(T)} not found in scene. Please add one to the bootstrap scene.");
                    }
                    else
                    {
                        DontDestroyOnLoad(_instance.gameObject);
                    }
                }

                return _instance;
            }
        }
    }

    protected virtual void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this as T;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnApplicationQuit()
    {
        _shuttingDown = true;
    }

    protected virtual void OnDestroy()
    {
        _shuttingDown = true;
    }
}
