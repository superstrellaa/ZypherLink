using UnityEngine;

public class PlayerManager : Singleton<PlayerManager>
{
    public string UUID { get; private set; }

    public PlayerState CurrentState { get; private set; } = PlayerState.Loading;

    public void SetUUID(string uuid)
    {
        UUID = uuid;
        LogManager.LogDebugOnly($"Player UUID set to: {uuid}", LogType.Network);
    }

    public void SetState(PlayerState state)
    {
        CurrentState = state;
        LogManager.Log($"Player state changed to: {state}", LogType.Gameplay);
    }
}
