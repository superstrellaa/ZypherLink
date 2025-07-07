# 📝 ZeroPing Message Protocol (v1)

**Project:** ZypherLink | SyncServer (Node.js, WebSocket, Unity backend)  
**Author:** superstrellaa

---

## 📦 Server-to-Client Messages

| Type             | Description                                                                                                                 |
| ---------------- | --------------------------------------------------------------------------------------------------------------------------- |
| `init`           | Sent on connection. Contains `uuid`, `roomId` (if assigned).                                                                |
| `startGame`      | Sent to all players when a room is full and the match starts. Contains `roomId` and a list of all player UUIDs (`players`). |
| `startPositions` | Sent to all players at match start. Contains the initial positions of **all** players, mapped by UUID.                      |
| `playerMoved`    | Broadcast when a player moves. Contains `uuid`, `x`, `y`, `z`.                                                              |
| `ping`           | Server liveness check. Client must reply with `pong`.                                                                       |
| `pong`           | Response to `ping`.                                                                                                         |
| `error`          | Error message. Contains `error` and optional `details`.                                                                     |

> **Note:**  
> The `startPositions` message ensures all clients know the initial spawn position of every player (including themselves) at the start of the match.  
> Example payload:
>
> ```json
> {
>   "type": "startPositions",
>   "positions": {
>     "uuid1": { "x": 0, "y": 0, "z": 0 },
>     "uuid2": { "x": 10, "y": 0, "z": 5 }
>   }
> }
> ```

---

## 📦 Client-to-Server Messages

| Type   | Description                              |
| ------ | ---------------------------------------- |
| `pong` | Response to server `ping`.               |
| `ping` | (Optional) Client can ping server.       |
| `move` | Request to move. Contains `x`, `y`, `z`. |

---

## 🔒 Validation, Rules & Security

- All messages must include a `type` field (string).
- Messages are validated using Joi schemas ([see schemas.js](../handlers/messages/schemas.js)).
- Invalid or unknown messages are ignored or responded to with an error.
- **Server authority:** Only valid, in-room actions are processed. Suspicious moves are blocked by anti-cheat.
- **Rate limiting:** Each player can send up to `RATE_LIMIT_MAX_MSGS` per `RATE_LIMIT_WINDOW_MS` (see [`config/rateLimit.js`](../config/rateLimit.js)).
- **Optimized serialization:** Messages are sent as compact JSON.
- **Room assignment:** Game actions are only allowed if the player is in a room.
- **Matchmaking cleanup:** Players who disconnect before being matched are removed from the queue.
- **Replay logging:** All room events are logged for replay/debugging.
- **Player sync on match start:** When a room is full, the server sends a `startGame` message to all players with the `roomId` and the full list of player UUIDs. This ensures all clients are synchronized before gameplay begins.

---

## 🫀 Heartbeat & Connection Liveness

- The server periodically sends a `ping` message to each connected client.
- The client **must** reply with a `pong` message as soon as possible.
- If the server does not receive a `pong` after several attempts (`MAX_MISSED_PONGS`), it will automatically disconnect the client.
- This mechanism ensures that inactive or disconnected clients are cleaned up quickly and reliably.

---

## 🧹 Automatic Room Cleanup

- When all players leave or disconnect from a room, the server automatically deletes the empty room.
- This cleanup is performed periodically and ensures efficient resource usage.
- No manual intervention is needed; rooms are only kept alive while they have active players.

---

## 🏗️ Architecture & Connection Flow

1. The user opens the game but is not connected to the server.
2. When clicking "Find Match", the client connects to the WebSocket server.
3. The server generates a UUID and automatically adds the player to the matchmaking queue.
4. When enough players are present, a room is created and all are notified with `startGame`.
5. The client can send game messages (e.g., `move`) only after receiving `startGame`.
6. If the client disconnects, it is removed from the room or matchmaking queue.
7. If a room becomes empty, it is deleted automatically.

**Advantages:**

- No idle/lobby connections.
- The server only manages active players in matchmaking or in-game.
- Easy to scale and decouple (future: separate lobby server if needed).

---

## 🛠️ Adding New Messages

1. Create a file in `/handlers/messages/` following this pattern:
   ```js
   module.exports = {
     type: "newType",
     handler: (
       uuid,
       socket,
       message,
       roomId,
       { logger, playerManager, roomManager }
     ) => {
       // logic here
     },
   };
   ```
2. Add the corresponding Joi schema in `/handlers/messages/schemas.js`.

---

## 🧩 Project Structure (Summary)

- `/core/server.js` → Initializes the WebSocket server
- `/handlers/connectionHandler.js` → Connection/disconnection logic
- `/handlers/messageHandler.js` → Validation, rate-limiting, dynamic dispatch
- `/handlers/messages/` → Handlers for each message type
- `/managers/` → Player, room, matchmaking logic
- `/models/` → Room and Player models
- `/config/` → Centralized configuration
- `/utils/` → Anti-cheat, replay logger, uuid, etc.
- `/logs/` → Server logs (auto-generated)
- `/replays/` → Replay logs (auto-generated)

---

## 🪵 Logging & Replay

- All important events (connections, disconnections, room creation, queue actions, errors, etc.) are logged using a professional logger.
- Room events are also saved as replay logs for debugging and analysis.
- Logs are stored in `/logs/` and `/replays/` directories.

---

## 📚 Example Message: Move

**Client → Server:**

```json
{
  "type": "move",
  "x": 1.23,
  "y": 0.0,
  "z": -4.56
}
```

**Server → All in Room:**

```json
{
  "type": "playerMoved",
  "uuid": "player-uuid",
  "x": 1.23,
  "y": 0.0,
  "z": -4.56
}
```

---

## 📄 See Also

- [SyncServer README](../../README.md)
- [Test Panel](../../Tests-WebSocket/exampleClient.js)
- [Configuration](../config/)

---

<p align="center"><sub>ZeroPing Protocol — Fast, Secure, and Simple Multiplayer for Unity & Node.js</sub></p>
