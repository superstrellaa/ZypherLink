using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapManager : Singleton<MapManager>
{
    [SerializeField] private GameObject remotePlayerPrefab;
    private bool sceneReady;
    private bool hasSetupDone;

    private Dictionary<string, GameObject> remotePlayers = new Dictionary<string, GameObject>();

    private class RemotePlayerState
    {
        public Vector3 targetPos;
        public Quaternion targetRot;
        public Vector3 velocity;
        public float lastUpdateTime;
    }

    private Dictionary<string, RemotePlayerState> playerStates = new();

    private void OnEnable()
    {
        SceneTransitionManager.OnSceneLoaded += HandleSceneLoaded;
        if (NetworkManager.IsAlive)
            NetworkManager.Instance.OnSpawnsReady += HandleSpawnsReady;
    }

    private void OnDisable()
    {
        SceneTransitionManager.OnSceneLoaded -= HandleSceneLoaded;
        if (NetworkManager.IsAlive)
            NetworkManager.Instance.OnSpawnsReady -= HandleSpawnsReady;
    }

    private void HandleSceneLoaded(string sceneName)
    {
        sceneReady = sceneName.StartsWith("sc_MainMap");
        if (sceneReady)
        {
            LogManager.LogDebugOnly($"MapManager detected scene load: {sceneName}", LogType.SceneManager);
            TrySetup();
        }
    }

    private void HandleSpawnsReady()
    {
        LogManager.Log("Spawns ready signal received.", LogType.Gameplay);
        TrySetup();
    }

    private bool AreSpawnsValid()
    {
        var players = NetworkManager.Instance.Players;
        if (players == null || players.Count == 0) return false;
        foreach (var p in players)
        {
            if (p.spawnPosition.sqrMagnitude > 0.0001f) return true;
        }
        return false;
    }

    private void TrySetup()
    {
        if (hasSetupDone) return;
        if (!sceneReady) return;
        if (!NetworkManager.Instance.MatchReady) return;
        if (!AreSpawnsValid())
        {
            LogManager.Log("Spawns not ready yet. Dumping current data...", LogType.Warning);
            DumpSpawnData();
            return;
        }
        Setup();
        hasSetupDone = true;
    }

    public void Setup()
    {
        var activeScene = SceneManager.GetActiveScene();

        foreach (var p in NetworkManager.Instance.Players)
        {
            if (p.uuid == PlayerManager.Instance.UUID)
            {
                var playerGO = PlayerManager.Instance.gameObject.transform.Find("Player")?.gameObject;
                if (playerGO != null)
                {
                    Teleport(playerGO, p.spawnPosition, p.spawnRotation);
                }
            }
            else
            {
                if (remotePlayerPrefab == null)
                {
                    LogManager.Log("remotePlayerPrefab is null.", LogType.Error);
                    continue;
                }

                var other = Instantiate(remotePlayerPrefab, p.spawnPosition, p.spawnRotation);
                other.name = $"RemotePlayer_{p.uuid}";
                SceneManager.MoveGameObjectToScene(other, activeScene);

                if (!remotePlayers.ContainsKey(p.uuid))
                    remotePlayers.Add(p.uuid, other);

                if (!playerStates.ContainsKey(p.uuid))
                    playerStates[p.uuid] = new RemotePlayerState
                    {
                        targetPos = p.spawnPosition,
                        targetRot = p.spawnRotation
                    };
            }
        }

        LogManager.LogDebugOnly("Map setup complete.", LogType.Gameplay);
    }

    public void UpdateRemotePlayer(string uuid, Vector3 pos, float rotationY, Vector3 velocity)
    {
        if (!remotePlayers.TryGetValue(uuid, out var playerGO)) return;

        if (!playerStates.TryGetValue(uuid, out var state))
        {
            state = new RemotePlayerState();
            playerStates[uuid] = state;
        }

        state.targetPos = pos;
        state.targetRot = Quaternion.Euler(0, rotationY, 0);
        state.velocity = velocity;
        state.lastUpdateTime = Time.time;
    }

    private void Update()
    {
        foreach (var kvp in remotePlayers)
        {
            var uuid = kvp.Key;
            var playerGO = kvp.Value;

            if (!playerStates.TryGetValue(uuid, out var state)) continue;

            float delta = (float)(Time.time - state.lastUpdateTime);
            Vector3 predictedPos = state.targetPos + state.velocity * delta;

            playerGO.transform.position = Vector3.Lerp(playerGO.transform.position, predictedPos, Time.deltaTime * 15f);
            playerGO.transform.rotation = Quaternion.Slerp(playerGO.transform.rotation, state.targetRot, Time.deltaTime * 10f);
        }
    }

    private void Teleport(GameObject go, Vector3 pos, Quaternion rot)
    {
        if (go.TryGetComponent<CharacterController>(out var cc))
        {
            cc.enabled = false;
            go.transform.SetPositionAndRotation(pos, rot);
            cc.enabled = true;
        }
        else if (go.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.position = pos;
            rb.rotation = rot;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        else
        {
            go.transform.SetPositionAndRotation(pos, rot);
        }
    }

    public void RemoveRemotePlayer(string uuid)
    {
        if (remotePlayers.TryGetValue(uuid, out var playerGO))
        {
            if (playerGO != null)
                GameObject.Destroy(playerGO);

            remotePlayers.Remove(uuid);
        }

        if (playerStates.ContainsKey(uuid))
            playerStates.Remove(uuid);

        LogManager.Log($"Remote player {uuid} removed.", LogType.Gameplay);
    }

    private void DumpSpawnData()
    {
        foreach (var p in NetworkManager.Instance.Players)
        {
            bool isZero = p.spawnPosition.sqrMagnitude < 0.0001f;
            LogManager.Log(
                $"Spawn -> uuid:{p.uuid} pos:{p.spawnPosition} rot:{p.spawnRotation.eulerAngles} isZero:{isZero}",
                LogType.Gameplay
            );
        }
    }

    public void ResetMapState()
    {
        hasSetupDone = false;
        sceneReady = false;

        foreach (var go in remotePlayers.Values)
        {
            if (go != null) GameObject.Destroy(go);
        }
        remotePlayers.Clear();
        playerStates.Clear();
    }
}
