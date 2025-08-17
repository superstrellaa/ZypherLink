using System;
using Newtonsoft.Json.Linq;

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

                case "ping":
                    NetworkManager.Instance.Send("{\"type\":\"pong\"}");
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
        string roomId = msg.Value<string>("roomId");

        PlayerManager.Instance.SetUUID(uuid);
        LogManager.Log($"Received init. UUID: {uuid}, Room ID: {roomId}", LogType.Network);
    }

    private static void HandleStartGame(JObject msg)
    {
        var players = msg["players"];
        string roomId = msg.Value<string>("roomId");

        LogManager.Log($"Match started in Room: {roomId} with players: {players}", LogType.Gameplay);
        // Puedes guardar jugadores o instanciarlos aquí
    }

    private static void HandleStartPositions(JObject msg)
    {
        var positions = msg["positions"];
        LogManager.Log($"Start positions received: {positions}", LogType.Gameplay);
        // Aquí iría la lógica para posicionar jugadores
    }

    private static void HandlePlayerMoved(JObject msg)
    {
        string uuid = msg.Value<string>("uuid");
        float x = msg.Value<float>("x");
        float y = msg.Value<float>("y");
        float z = msg.Value<float>("z");

        LogManager.LogDebugOnly($"Player {uuid} moved to ({x}, {y}, {z})", LogType.Gameplay);
        // Mueve al jugador con ese UUID
    }
}
