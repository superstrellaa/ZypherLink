function startRoomCleanup(
  matchmaking,
  logger,
  replayLogger,
  TICK_RATE_MS,
  INACTIVITY_TIMEOUT_MS
) {
  setInterval(() => {
    for (const [roomId, room] of matchmaking.rooms.entries()) {
      if (
        room.isEmpty() ||
        Date.now() - room.lastActivity > INACTIVITY_TIMEOUT_MS
      ) {
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
