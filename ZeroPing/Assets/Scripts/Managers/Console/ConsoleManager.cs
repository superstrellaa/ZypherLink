using UnityEngine;

public class ConsoleManager : Singleton<ConsoleManager>
{
    [Header("Console Button")]
    [SerializeField] private GameObject consoleButton;

    [Header("Console")]
    [SerializeField] private GameObject console;

    private void Start()
    {
        if (Debug.isDebugBuild || Application.isEditor)
        {
            consoleButton.SetActive(true);
            consoleButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
            {
                console.SetActive(!console.activeSelf);
            });
        } 
        else
        {
            consoleButton.SetActive(false);
        }
    }
}
