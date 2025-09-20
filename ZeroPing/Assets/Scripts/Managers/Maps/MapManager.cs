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

    private Dictionary<string, RemotePlayerState> playerStates = new Dictionary<string, RemotePlayerState>();

    private HashSet<string> pendingInstantiate = new HashSet<string>();

    private const float k_SpawnThreshold = 0.0001f;

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
            TryInstantiatePending();
            TrySetup();
        }
    }

    private void HandleSpawnsReady()
    {
        LogManager.Log("Spawns ready signal received.", LogType.Gameplay);
        TryInstantiatePending();
        TrySetup();
    }

    private bool AreSpawnsValid()
    {
        var players = NetworkManager.Instance.Players;
        if (players == null || players.Count == 0) return false;
        foreach (var p in players)
        {
            if (p.spawnPosition.sqrMagnitude > k_SpawnThreshold) return true;
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
                if (remotePlayers.TryGetValue(p.uuid, out var existing) && existing != null)
                {
                    SceneManager.MoveGameObjectToScene(existing, activeScene);
                    Teleport(existing, p.spawnPosition, p.spawnRotation);
                }
                else
                {
                    if (p.spawnPosition.sqrMagnitude > k_SpawnThreshold)
                    {
                        InstantiateRemotePlayer(p.uuid, p.spawnPosition, p.spawnRotation, activeScene);
                        pendingInstantiate.Remove(p.uuid);
                    }
                    else
                    {
                        pendingInstantiate.Add(p.uuid);
                        if (!playerStates.ContainsKey(p.uuid))
                            playerStates[p.uuid] = new RemotePlayerState { targetPos = Vector3.zero, targetRot = Quaternion.identity, lastUpdateTime = Time.time };
                    }
                }
            }
        }

        LogManager.LogDebugOnly("Map setup complete.", LogType.Gameplay);
    }

    private void InstantiateRemotePlayer(string uuid, Vector3 pos, Quaternion rot, Scene targetScene)
    {
        if (remotePlayers.ContainsKey(uuid) && remotePlayers[uuid] != null)
        {
            LogManager.LogDebugOnly($"InstantiateRemotePlayer skipped: already exists {uuid}.", LogType.Gameplay);
            return;
        }

        if (remotePlayerPrefab == null)
        {
            LogManager.Log("remotePlayerPrefab is null.", LogType.Error);
            return;
        }

        var other = Instantiate(remotePlayerPrefab, pos, rot);
        other.name = $"RemotePlayer_{uuid}";
        SceneManager.MoveGameObjectToScene(other, targetScene);

        remotePlayers[uuid] = other;

        if (!playerStates.ContainsKey(uuid))
            playerStates[uuid] = new RemotePlayerState { targetPos = pos, targetRot = rot, lastUpdateTime = Time.time };

        LogManager.LogDebugOnly($"Remote player {uuid} instantiated in scene {targetScene.name}.", LogType.Gameplay);
    }

    public void UpdateRemotePlayer(string uuid, Vector3 pos, float rotationY, Vector3 velocity)
    {
        if (!playerStates.TryGetValue(uuid, out var state))
        {
            state = new RemotePlayerState();
            playerStates[uuid] = state;
        }

        state.targetPos = pos;
        state.targetRot = Quaternion.Euler(0, rotationY, 0);
        state.velocity = velocity;
        state.lastUpdateTime = Time.time;

        if (!remotePlayers.ContainsKey(uuid))
        {
            pendingInstantiate.Add(uuid);
        }
    }

    public void AddRemotePlayer(string uuid)
    {
        if (remotePlayers.ContainsKey(uuid) || pendingInstantiate.Contains(uuid)) return;

        pendingInstantiate.Add(uuid);
        playerStates[uuid] = new RemotePlayerState
        {
            targetPos = Vector3.zero,
            targetRot = Quaternion.identity,
            lastUpdateTime = Time.time
        };

        if (sceneReady)
        {
            var p = NetworkManager.Instance.Players.Find(x => x.uuid == uuid);
            if (p != null && p.spawnPosition.sqrMagnitude > k_SpawnThreshold)
            {
                InstantiateRemotePlayer(uuid, p.spawnPosition, p.spawnRotation, SceneManager.GetActiveScene());
                pendingInstantiate.Remove(uuid);
            }
        }

        LogManager.LogDebugOnly($"Remote player {uuid} queued (pendingInstantiate).", LogType.Gameplay);
    }

    private void TryInstantiatePending()
    {
        if (!sceneReady || pendingInstantiate.Count == 0) return;
        var activeScene = SceneManager.GetActiveScene();

        foreach (var uuid in new List<string>(pendingInstantiate))
        {
            if (remotePlayers.TryGetValue(uuid, out var existing) && existing != null)
            {
                pendingInstantiate.Remove(uuid);
                continue;
            }

            var p = NetworkManager.Instance.Players.Find(x => x.uuid == uuid);
            if (p != null && p.spawnPosition.sqrMagnitude > k_SpawnThreshold)
            {
                InstantiateRemotePlayer(uuid, p.spawnPosition, p.spawnRotation, activeScene);
                pendingInstantiate.Remove(uuid);
                continue;
            }

            if (playerStates.TryGetValue(uuid, out var state) && state.targetPos.sqrMagnitude > k_SpawnThreshold)
            {
                InstantiateRemotePlayer(uuid, state.targetPos, state.targetRot, activeScene);
                pendingInstantiate.Remove(uuid);
                continue;
            }
        }
    }

    private void Update()
    {
        foreach (var kvp in new List<KeyValuePair<string, GameObject>>(remotePlayers))
        {
            var uuid = kvp.Key;
            var playerGO = kvp.Value;

            if (playerGO == null) continue;

            if (!playerStates.TryGetValue(uuid, out var state)) continue;

            float delta = Time.time - state.lastUpdateTime;
            Vector3 predictedPos = state.targetPos + state.velocity * delta;

            playerGO.transform.position = Vector3.Lerp(playerGO.transform.position, predictedPos, Time.deltaTime * 15f);
            playerGO.transform.rotation = Quaternion.Slerp(playerGO.transform.rotation, state.targetRot, Time.deltaTime * 10f);
        }

        TryInstantiatePending();
    }

    private void Teleport(GameObject go, Vector3 pos, Quaternion rot)
    {
        if (go == null) return;

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

        pendingInstantiate.Remove(uuid);

        LogManager.Log($"Remote player {uuid} removed.", LogType.Gameplay);
    }

    private void DumpSpawnData()
    {
        foreach (var p in NetworkManager.Instance.Players)
        {
            bool isZero = p.spawnPosition.sqrMagnitude < k_SpawnThreshold;
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
        pendingInstantiate.Clear();
    }
}
