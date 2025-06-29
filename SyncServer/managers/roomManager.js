const matchmakingManager = require("./matchmaking/matchmakingManager");
const logger = require("../config/logger");
const { TICK_RATE_MS, INACTIVITY_TIMEOUT_MS } = require("../config/game");
const replayLogger = require("../utils/replayLogger");

// Room tick logic for all rooms
function startRoomTicking() {
  setInterval(() => {
    for (const [roomId, room] of matchmakingManager.rooms.entries()) {
      // Custom per-room logic here (game state, etc)
      // Check inactivity
      if (
        room.isEmpty() ||
        Date.now() - room.lastActivity > INACTIVITY_TIMEOUT_MS
      ) {
        logger.info("Room inactive or empty, deleting", {
          context: "roomManager",
          roomId,
        });
        // Cerrar replay al terminar la sala
        replayLogger.closeReplay(roomId);
        matchmakingManager.rooms.delete(roomId);
      }
    }
  }, TICK_RATE_MS);
}

function broadcastToRoom(senderUUID, message) {
  const roomId = matchmakingManager.playerToRoom.get(senderUUID);
  if (!roomId) {
    logger.warn("Player tried to broadcast without room", {
      player: senderUUID,
    });
    return;
  }
  const room = matchmakingManager.rooms.get(roomId);
  if (!room) {
    logger.warn("Room not found for broadcast", { player: senderUUID, roomId });
    return;
  }
  const data = JSON.stringify(message);
  for (const [uuid, socket] of room.players.entries()) {
    if (uuid !== senderUUID && socket.readyState === socket.OPEN) {
      socket.send(data);
    }
  }
  replayLogger.logEvent(roomId, {
    event: "broadcast",
    sender: senderUUID,
    message,
  });
  logger.info("Broadcast to room", {
    context: "roomManager",
    sender: senderUUID,
    roomId,
    messageType: message.type,
  });
}

module.exports = {
  addPlayerToRoom: matchmakingManager.addPlayerToQueue,
  removePlayerFromRoom: matchmakingManager.removePlayerFromRoom,
  broadcastToRoom,
  getRoomByPlayer: matchmakingManager.getRoomByPlayer,
  startRoomTicking,
};
