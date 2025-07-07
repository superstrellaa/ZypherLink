using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : Singleton<SceneTransitionManager>
{
    [Header("Bridge Scene Transition")]
    [SerializeField] private string transitionSceneName = "sc_TransitionBridge";

    private string targetScene;
    private string currentScene;

    protected override void Awake()
    {
        base.Awake();
        currentScene = SceneManager.GetActiveScene().name;
    }

    public void TransitionTo(string newScene, bool withFade = true)
    {
        if (string.IsNullOrEmpty(newScene))
        {
            LogManager.Log("Target scene name is null or empty. Transition aborted.", LogType.SceneManager);
            return;
        }

        LogManager.Log($"Initiating transition to scene: '{newScene}' (With fade: {withFade})", LogType.SceneManager);

        targetScene = newScene;
        if (withFade)
        {
            FadeManager.Instance.FadeOut(() =>
            {
                StartCoroutine(PerformTransition(true));
            });
        }
        else
        {
            StartCoroutine(PerformTransition(false));
        }
    }

    private IEnumerator PerformTransition(bool withFade)
    {
        LogManager.LogDebugOnly($"Loading bridge scene: '{transitionSceneName}'", LogType.SceneManager);
        AsyncOperation loadBridgeOp = SceneManager.LoadSceneAsync(transitionSceneName, LoadSceneMode.Additive);
        while (!loadBridgeOp.isDone) yield return null;

        SceneManager.SetActiveScene(SceneManager.GetSceneByName(transitionSceneName));
        LogManager.LogDebugOnly("Bridge scene loaded and activated.", LogType.SceneManager);

        yield return new WaitForSeconds(0.3f);

        LogManager.LogDebugOnly($"Loading target scene: '{targetScene}'", LogType.SceneManager);
        AsyncOperation loadOp = SceneManager.LoadSceneAsync(targetScene, LoadSceneMode.Additive);

        LogManager.LogDebugOnly($"Unloading previous scene: '{currentScene}'", LogType.SceneManager);
        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(currentScene);

        while (!loadOp.isDone || (unloadOp != null && !unloadOp.isDone))
            yield return null;

        Scene scene = SceneManager.GetSceneByName(targetScene);
        SceneManager.SetActiveScene(scene);
        currentScene = targetScene;
        LogManager.Log($"Target scene '{targetScene}' loaded and activated.", LogType.SceneManager);

        yield return new WaitForSeconds(0.3f);

        if (withFade)
        {
            LogManager.LogDebugOnly("Starting fade in after scene load.", LogType.SceneManager);
            FadeManager.Instance.FadeIn();
        }
        else if (FadeManager.Instance.IsFadePanelVisible())
        {
            LogManager.LogDebugOnly("Fade panel still visible. Forcing fade in.", LogType.SceneManager);
            FadeManager.Instance.FadeIn();
        }

        LogManager.LogDebugOnly($"Unloading bridge scene: '{transitionSceneName}'", LogType.SceneManager);
        yield return SceneManager.UnloadSceneAsync(transitionSceneName);

        LogManager.Log("Scene transition complete.", LogType.SceneManager);
    }
}
