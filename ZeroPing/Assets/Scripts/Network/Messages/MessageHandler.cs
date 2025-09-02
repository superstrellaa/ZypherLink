using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MessageHandler
{
    private static float lastSendTime = 0f;

    public static void Handle(string json)
    {
        try
        {
            JObject obj = JObject.Parse(json);
            string type = obj.Value<string>("type");

            switch (type)
            {
                case "init":
                    HandleInit(obj);
                    break;

                case "startGame":
                    HandleStartGame(obj);
                    break;

                case "startPositions":
                    HandleStartPositions(obj);
                    break;

                case "playerMoved":
                    HandlePlayerMoved(obj);
                    break;

                case "playerDisconnected":
                    HandlePlayerDisconnected(obj);
                    break;

                case "roomInactive":
                    LogManager.LogDebugOnly("Room is inactive.", LogType.Network);
                    CoroutineRunner.Instance.Run(HandleRoomInactiveEnum(obj.Value<string>("roomId")));
                    break;

                case "roomDeleted":
                    HandleRoomDeleted(obj);
                    break;

                case "roomLefted":
                    ESCManager.Instance.ActionLeaveOk();
                    LogManager.LogDebugOnly("Left room successfully.", LogType.Network);
                    break;

                case "authSuccess":
                    LogManager.Log("Authentication successful.", LogType.Network);
                    break;

                case "adminMessage":
                    HandleAdminMessage(obj);
                    break;

                case "ping":
                    NetworkManager.Instance.Send("{\"type\":\"pong\"}");
                    break;

                case "pong":
                    HandlePong();
                    break;

                case "error":
                    string error = obj.Value<string>("error");
                    LogManager.Log($"Server error: {error}", LogType.Error);
                    break;

                default:
                    LogManager.Log($"Unknown message type: {type}", LogType.Warning);
                    break;
            }
        }
        catch (Exception ex)
        {
            LogManager.Log($"Failed to parse message: {ex.Message}", LogType.Error);
        }
    }

    private static void HandlePong()
    {
        double rtt = (Time.timeAsDouble - NetworkManager.Instance.LastPingSent) * 1000.0;
        NetworkManager.Instance.AddRttSample(rtt);

        double avg = NetworkManager.Instance.GetAverageRtt();
        double jitter = NetworkManager.Instance.GetJitter();

        LogManager.LogDebugOnly($"Ping: {rtt:F1} ms | Avg: {avg:F1} ms | Jitter: {jitter:F1} ms", LogType.Network);
    }

    private static void HandleInit(JObject msg)
    {
        string uuid = msg.Value<string>("uuid");

        PlayerManager.Instance.SetUUID(uuid);
        LogManager.Log($"Received init. UUID: {uuid}", LogType.Network);
    }

    private static void HandleStartGame(JObject msg)
    {
        string roomId = msg.Value<string>("roomId");
        string mapName = msg.Value<string>("map") ?? "Unknown Map";
        var playersToken = msg["players"];

        var playersList = new List<PlayerInfo>();

        if (playersToken is JArray playersArray)
        {
            foreach (var p in playersArray)
            {
                if (p is JObject playerObj)
                {
                    string uuid = playerObj.Value<string>("uuid");
                    Vector3 spawnPos = Vector3.zero;
                    Quaternion spawnRot = Quaternion.identity;

                    playersList.Add(new PlayerInfo
                    {
                        uuid = uuid,
                        spawnPosition = spawnPos,
                        spawnRotation = spawnRot
                    });
                }
                else if (p is JValue playerVal)
                {
                    string uuid = playerVal.Value<string>();
                    playersList.Add(new PlayerInfo
                    {
                        uuid = uuid,
                        spawnPosition = Vector3.zero,
                        spawnRotation = Quaternion.identity
                    });
                }
                else
                {
                    Debug.LogWarning("Unexpected player format: " + p);
                }
            }
        }
        else
        {
            Debug.LogWarning("Players token is not an array: " + playersToken);
        }

        NetworkManager.Instance.SetMatchData(roomId, playersList, mapName);
        LogManager.LogDebugOnly($"Match started in Room: {roomId} with players: {playersList.Count}", LogType.Gameplay);

        if (LobbyManager.Instance != null)
            CoroutineRunner.Instance.Run(LobbyManager.Instance.MatchFoundScreenEnum());
    }

    private static void HandleStartPositions(JObject msg)
    {
        var positionsToken = msg["positions"] as JObject;
        if (positionsToken == null) return;

        foreach (var kvp in positionsToken)
        {
            string uuid = kvp.Key;
            var posObj = kvp.Value as JObject;
            if (posObj == null) continue;

            float x = posObj.Value<float>("x");
            float y = posObj.Value<float>("y");
            float z = posObj.Value<float>("z");

            var playerInfo = NetworkManager.Instance.Players.Find(pl => pl.uuid == uuid);
            if (playerInfo != null)
            {
                playerInfo.spawnPosition = new Vector3(x, y, z);
                playerInfo.spawnRotation = Quaternion.identity;
            }
            LogManager.LogDebugOnly($"Updated {uuid} spawn -> {playerInfo.spawnPosition}", LogType.Gameplay);
        }

        NetworkManager.Instance.MarkSpawnsReady();
        LogManager.LogDebugOnly($"Start positions updated for {positionsToken.Count} players.", LogType.Gameplay);
    }

    public static void Move(Vector3 position, float rotationY, Vector3 velocity)
    {
        if (!NetworkManager.IsAlive || !NetworkManager.Instance.IsConnected) return;

        if (Time.time - lastSendTime < 0.05f)
            return;

        lastSendTime = Time.time;

        JObject moveMsg = new JObject
        {
            ["type"] = "move",
            ["x"] = position.x,
            ["y"] = position.y,
            ["z"] = position.z,
            ["rotationY"] = rotationY,
            ["vx"] = velocity.x,
            ["vy"] = velocity.y,
            ["vz"] = velocity.z
        };

        NetworkManager.Instance.Send(moveMsg.ToString());
    }

    private static void HandlePlayerMoved(JObject msg)
    {
        string uuid = msg.Value<string>("uuid");
        float x = msg.Value<float>("x");
        float y = msg.Value<float>("y");
        float z = msg.Value<float>("z");
        float rotationY = msg.Value<float>("rotationY");
        float vx = msg.Value<float>("vx");
        float vy = msg.Value<float>("vy");
        float vz = msg.Value<float>("vz");

        Vector3 position = new Vector3(x, y, z);
        Vector3 velocity = new Vector3(vx, vy, vz);

        LogManager.LogDebugOnly($"Player {uuid} moved to {position} rotY({rotationY}) vel({velocity})", LogType.Gameplay);

        if (uuid != PlayerManager.Instance.UUID)
        {
            MapManager.Instance.UpdateRemotePlayer(uuid, position, rotationY, velocity);
        }
    }

    private static void HandlePlayerDisconnected(JObject msg)
    {
        string uuid = msg.Value<string>("uuid");
        LogManager.Log($"Player {uuid} disconnected.", LogType.Gameplay);

        if (MapManager.IsAlive)
            MapManager.Instance.RemoveRemotePlayer(uuid);
    }

    private static void HandleRoomDeleted(JObject msg)
    {
        string roomId = msg.Value<string>("roomId");
        LogManager.Log($"Room {roomId} has been deleted.", LogType.Network);
        CoroutineRunner.Instance.Run(HandleRoomDeletedEnum(roomId));
    }

    private static IEnumerator HandleRoomDeletedEnum(string roomId)
    {
        PlayerManager.Instance.SetState(PlayerState.Lobby);
        ESCManager.Instance.CloseESCMenu();
        yield return new WaitForSeconds(1f);

        if (MapManager.IsAlive)
            MapManager.Instance.ResetMapState();

        SceneTransitionManager.Instance.TransitionTo("sc_Lobby", withFade: false);
        GUIManager.Instance.SetGUIOpen(true);
        PlayerManager.Instance.gameObject.transform.Find("Player").gameObject.transform.position = new Vector3(0, 0, 0);
        GUIManager.Instance.ShowPanel(GUIManager.Instance.lobbyMatchFound, false);
        yield return new WaitForSeconds(1f);
        PopupManager.Instance.Open(PopupType.RoomDeleted);
    }

    private static IEnumerator HandleRoomInactiveEnum(string roomId)
    {
        PlayerManager.Instance.SetState(PlayerState.Lobby);
        ESCManager.Instance.CloseESCMenu();
        yield return new WaitForSeconds(1f);

        if (MapManager.IsAlive)
            MapManager.Instance.ResetMapState();

        SceneTransitionManager.Instance.TransitionTo("sc_Lobby", withFade: false);
        GUIManager.Instance.SetGUIOpen(true);
        PlayerManager.Instance.gameObject.transform.Find("Player").gameObject.transform.position = new Vector3(0, 0, 0);
        GUIManager.Instance.ShowPanel(GUIManager.Instance.lobbyMatchFound, false);
        yield return new WaitForSeconds(1f);
        PopupManager.Instance.Open(PopupType.RoomInactive);
    }

    private static void HandleAdminMessage(JObject msg)
    {
        string message = msg.Value<string>("content");
        PopupManager.Instance.Open("Admin Message", message);
        LogManager.Log($"Admin message: {message}", LogType.Network);
    }
}
