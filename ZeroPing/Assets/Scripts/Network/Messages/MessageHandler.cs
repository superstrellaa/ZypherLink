using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MessageHandler
{
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

                case "roomDeleted":
                    HandleRoomDeleted(obj);
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
                    LogManager.LogDebugOnly("Pong received from server.", LogType.Network);
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
        if (positionsToken == null)
        {
            Debug.LogWarning("Positions token is not a JObject: " + msg["positions"]);
            return;
        }

        if (NetworkManager.Instance.Players == null)
        {
            Debug.LogWarning("NetworkManager players list is null.");
            return;
        }

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
        }

        LogManager.LogDebugOnly($"Start positions updated for {positionsToken.Count} players.", LogType.Gameplay);
    }

    private static void HandlePlayerMoved(JObject msg)
    {
        string uuid = msg.Value<string>("uuid");
        float x = msg.Value<float>("x");
        float y = msg.Value<float>("y");
        float z = msg.Value<float>("z");

        LogManager.LogDebugOnly($"Player {uuid} moved to ({x}, {y}, {z})", LogType.Gameplay);
    }
    
    private static void HandlePlayerDisconnected(JObject msg)
    {
        string uuid = msg.Value<string>("uuid");
        LogManager.Log($"Player {uuid} disconnected.", LogType.Gameplay);
    }

    private static void HandleRoomDeleted(JObject msg)
    {
        string roomId = msg.Value<string>("roomId");
        LogManager.Log($"Room {roomId} has been deleted.", LogType.Network);
        CoroutineRunner.Instance.Run(HandleRoomDeletedEnum(roomId));
    }

    private static IEnumerator HandleRoomDeletedEnum(string roomId)
    {
        yield return new WaitForSeconds(1f);
        SceneTransitionManager.Instance.TransitionTo("sc_Lobby", withFade: false);
        PlayerManager.Instance.SetState(PlayerState.Lobby);
        GUIManager.Instance.SetGUIOpen(true);
        GUIManager.Instance.ShowPanel(GUIManager.Instance.lobbyMatchFound, false);
        yield return new WaitForSeconds(1f);
        PopupManager.Instance.Open(PopupType.RoomDeleted);
    }

    private static void HandleAdminMessage(JObject msg)
    {
        string message = msg.Value<string>("content");
        PopupManager.Instance.Open("Admin Message", message);
        LogManager.Log($"Admin message: {message}", LogType.Network);
    }
}
