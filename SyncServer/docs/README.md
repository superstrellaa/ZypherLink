# 📝 ZeroPing Message Protocol (v1)

**Project:** ZypherLink | SyncServer (Node.js, WebSocket, Unity backend)  
**Author:** superstrellaa

---

## 📦 Server-to-Client Messages

| Type         | Description                                 |
|--------------|---------------------------------------------|
| `init`       | Sent on connection. Contains `uuid`, `roomId` (if assigned). |
| `joinedRoom` | Notifies the client that it has joined a room. Contains `roomId`. |
| `playerMoved`| Broadcast when a player moves. Contains `uuid`, `x`, `y`, `z`. |
| `ping`       | Server liveness check. Client must reply with `pong`. |
| `pong`       | Response to `ping`.                         |
| `error`      | Error message. Contains `error` and optional `details`. |

---

## 📦 Client-to-Server Messages

| Type   | Description                                 |
|--------|---------------------------------------------|
| `pong` | Response to server `ping`.                  |
| `ping` | (Optional) Client can ping server.          |
| `move` | Request to move. Contains `x`, `y`, `z`.    |

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

---

## 🏗️ Architecture & Connection Flow

1. The user opens the game but is not connected to the server.
2. When clicking "Find Match", the client connects to the WebSocket server.
3. The server generates a UUID and automatically adds the player to the matchmaking queue.
4. When enough players are present, a room is created and all are notified with `joinedRoom`.
5. The client can send game messages (e.g., `move`) only after joining a room.
6. If the client disconnects, it is removed from the room or matchmaking queue.

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
     handler: (uuid, socket, message, roomId, { logger, playerManager, roomManager }) => {
       // logic here
     },
   };
   ```
2. Add the corresponding Joi schema in `/handlers/messages/schemas.js`.
3. Document the new message in this file.

---

## 🧩 Project Structure (Summary)

- `/core/server.js`           → Initializes the WebSocket server
- `/handlers/connectionHandler.js` → Connection/disconnection logic
- `/handlers/messageHandler.js`    → Validation, rate-limiting, dynamic dispatch
- `/handlers/messages/`            → Handlers for each message type
- `/managers/`                     → Player, room, matchmaking logic
- `/models/`                       → Room and Player models
- `/config/`                       → Centralized configuration
- `/utils/`                        → Anti-cheat, replay logger, uuid, etc.
- `/logs/`                         → Server logs (auto-generated)
- `/replays/`                      → Replay logs (auto-generated)

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
