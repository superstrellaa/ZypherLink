using UnityEngine;

public class BootstrapLoader : MonoBehaviour
{
    [SerializeField] private GameObject guiPrefab;

    private void Awake()
    {
        LogManager.Log("Bootstrap system initializing...", LogType.Bootstrap);

        if (FindObjectOfType<GUIManager>() == null)
        {
            LogManager.Log("GUI Manager not found. Instantiating new GUI prefab.", LogType.Bootstrap);
            Instantiate(guiPrefab);
        }

        LogManager.Log("Bootstrap completed. Ready for scene transition.", LogType.Bootstrap);
        SceneTransitionManager.Instance.TransitionTo("sc_Lobby", withFade: false);
    }
}
