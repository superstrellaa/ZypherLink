using System.Collections;

public class CoroutineRunner : Singleton<CoroutineRunner>
{
    public void Run(IEnumerator coroutine)
    {
        StartCoroutine(coroutine);
    }
}
