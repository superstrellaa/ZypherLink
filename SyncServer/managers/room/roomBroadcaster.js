const matchmaking = require("../matchmaking");
const logger = require("../../utils/logger");
const replayLogger = require("../../utils/replayLogger");
const playerManager = require("../../managers/playerManager");

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
  for (const uuid of room.players) {
    if (uuid !== senderUUID) {
      const player = playerManager.getPlayer(uuid);
      if (
        player &&
        player.socket &&
        player.socket.readyState === player.socket.OPEN
      ) {
        player.socket.send(data);
      }
    }
  }
  replayLogger.logEvent(roomId, {
    event: "broadcast",
    sender: senderUUID,
    message,
  });
  /* logger.info("Broadcast to room", {
    context: "roomManager",
    sender: senderUUID,
    roomId,
    messageType: message.type,
  }); */
}

module.exports = { broadcastToRoom };
