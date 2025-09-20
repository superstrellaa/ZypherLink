function startRoomCleanup(
  matchmaking,
  logger,
  replayLogger,
  TICK_RATE_MS,
  INACTIVITY_TIMEOUT_MS
) {
  setInterval(() => {
    for (const [roomId, room] of matchmaking.rooms.entries()) {
      if (roomId === "unlimited-room") {
        continue;
      }
      if (
        room.isEmpty() ||
        Date.now() - room.lastActivity > INACTIVITY_TIMEOUT_MS
      ) {
        for (const uuid of room.players) {
          const playerManager = require("../../managers/playerManager");
          const socket = playerManager.getSocket(uuid);
          if (socket && socket.readyState === socket.OPEN) {
            socket.send(JSON.stringify({ type: "roomInactive", roomId }));
          }
        }
        logger.info("Room inactive or empty, deleting", {
          context: "roomManager",
          roomId,
        });
        replayLogger.closeReplay(roomId);
        matchmaking.rooms.delete(roomId);
      }
    }
  }, TICK_RATE_MS);
}

module.exports = { startRoomCleanup };
