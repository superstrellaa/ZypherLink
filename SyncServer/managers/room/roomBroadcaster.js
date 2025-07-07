const matchmaking = require("../matchmaking");
const logger = require("../../utils/logger");
const replayLogger = require("../../utils/replayLogger");

function broadcastToRoom(senderUUID, message) {
  const roomId = matchmaking.playerToRoom.get(senderUUID);
  if (!roomId) {
    logger.warn("Player tried to broadcast without room", {
      player: senderUUID,
    });
    return;
  }
  const room = matchmaking.rooms.get(roomId);
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

module.exports = { broadcastToRoom };
