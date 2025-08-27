const Logger = require("../utils/logger");
const { generateUUID } = require("../utils/uuid");
const playerManager = require("../managers/playerManager");
const { startHeartbeat } = require("./heartbeat");
const { registerSocketEvents } = require("./socketEvents");

function handleConnection(socket) {
  const uuid = generateUUID();
  playerManager.addPlayer(uuid, socket);

  Logger.info("Player connected", {
    player: uuid,
    context: "connection",
  });

  socket.send(JSON.stringify({ type: "init", uuid }));

  let authed = false;
  let heartbeat = null;
  let authTimeout = setTimeout(() => {
    if (!authed) {
      Logger.warn("Auth timeout, disconnecting", { player: uuid });
      socket.send(JSON.stringify({ type: "error", error: "Auth timeout" }));
      socket.terminate();
    }
  }, 60000);

  function setAuthed() {
    if (!authed) {
      authed = true;
      clearTimeout(authTimeout);
      heartbeat = startHeartbeat(socket, uuid);
      registerSocketEvents(socket, uuid, heartbeat.onPong, heartbeat.stop);
    }
  }

  function disconnect() {
    clearTimeout(authTimeout);
    socket.terminate();
  }

  socket.on("message", async (data) => {
    let parsed = null;
    try {
      if (Buffer.isBuffer(data)) {
        data = data.toString("utf8");
      }
      parsed = typeof data === "string" ? JSON.parse(data) : data;
    } catch (e) {
      socket.send(JSON.stringify({ type: "error", error: "Invalid JSON" }));
      return;
    }
    if (!authed) {
      if (parsed && parsed.type === "auth") {
        const authHandler = require("./messages/auth");
        await authHandler.handler(uuid, socket, parsed, null, {
          playerManager,
          setAuthed,
          disconnect,
        });
      } else {
        socket.send(
          JSON.stringify({
            type: "error",
            error: "Not authenticated. Send 'auth' message first.",
          })
        );
      }
      return;
    }
  });
}

module.exports = handleConnection;
