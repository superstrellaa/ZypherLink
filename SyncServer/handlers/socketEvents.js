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
    const roomManager = require("../managers/roomManager");
    let room = null;
    if (typeof roomManager.getRoomByPlayer === "function") {
      room = roomManager.getRoomByPlayer(uuid);
    }
    if (room && room.players) {
      for (const otherUUID of room.players) {
        if (otherUUID !== uuid) {
          const otherSocket = playerManager.getSocket(otherUUID);
          if (otherSocket && otherSocket.readyState === otherSocket.OPEN) {
            otherSocket.send(
              JSON.stringify({ type: "playerDisconnected", uuid })
            );
            logger.info("PlayerDisconnected message sent", {
              context: "roomManager",
              to: otherUUID,
              disconnected: uuid,
              roomId: room.roomId,
            });
          }
        }
      }
      room.players.delete(uuid);
      if (room.players.size === 1) {
        const lastUUID = Array.from(room.players)[0];
        const lastSocket = playerManager.getSocket(lastUUID);
        if (lastSocket && lastSocket.readyState === lastSocket.OPEN) {
          lastSocket.send(
            JSON.stringify({ type: "roomDeleted", roomId: room.roomId })
          );
          logger.info("Room Deleted message sent to player", {
            context: "roomManager",
            to: lastUUID,
            roomId: room.roomId,
          });
        }
      }
    }
    roomManager.removePlayerFromRoom(uuid);
    playerManager.removePlayer(uuid);
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
