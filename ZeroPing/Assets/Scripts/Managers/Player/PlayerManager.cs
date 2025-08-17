using UnityEngine;

public class PlayerManager : Singleton<PlayerManager>
{
    public string UUID { get; private set; }

    public void SetUUID(string uuid)
    {
        UUID = uuid;
        LogManager.LogDebugOnly($"Player UUID set to: {uuid}", LogType.Network);
    }
}
