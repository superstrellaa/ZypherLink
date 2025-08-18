const logger = require("../utils/logger");
const playerManager = require("../managers/playerManager");
const handleMessage = require("./messageHandler");

function registerSocketEvents(socket, uuid, onPong, stopHeartbeat) {
  socket.on("message", (data) => {
    let parsed = null;
    try {
      if (Buffer.isBuffer(data)) data = data.toString("utf8");
      parsed = typeof data === "string" ? JSON.parse(data) : data;
    } catch (e) {
      logger.error("Error parsing message", {
        player: uuid,
        context: "connectionHandler",
        error: e.message,
        rawMessage: data,
      });
      return;
    }

    if (parsed && parsed.type === "pong") {
      onPong();
      return;
    }

    if (!parsed || typeof parsed !== "object" || !parsed.type) return;

    handleMessage(uuid, socket, JSON.stringify(parsed));
  });

  socket.on("close", () => {
    playerManager.removePlayer(uuid);
    const roomManager = require("../managers/roomManager");
    roomManager.removePlayerFromRoom(uuid);
    if (typeof roomManager.getRoomByPlayer === "function") {
      const room = roomManager.getRoomByPlayer(uuid);
      if (room && room.players && room.players.has(uuid)) {
        room.players.delete(uuid);
      }
    }
    if (typeof stopHeartbeat === "function") stopHeartbeat();
    logger.info("Player disconnected", { player: uuid, context: "connection" });
  });

  socket.on("error", (err) => {
    logger.error("Socket error", {
      player: uuid,
      context: "connectionHandler",
      error: err.message,
    });
  });
}

module.exports = { registerSocketEvents };
