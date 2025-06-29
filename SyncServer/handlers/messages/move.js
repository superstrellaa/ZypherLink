const antiCheat = require("../../utils/antiCheat");

module.exports = {
  type: "move",
  handler: (
    uuid,
    socket,
    message,
    roomId,
    { logger, playerManager, roomManager }
  ) => {
    // Anti-cheat: check for suspicious movement
    if (
      antiCheat.isMoveSuspicious(uuid, {
        x: message.x,
        y: message.y,
        z: message.z,
      })
    ) {
      logger.warn("[AntiCheat] Suspicious move detected, move cancelled", {
        player: uuid,
        context: "move",
        x: message.x,
        y: message.y,
        z: message.z,
        roomId,
      });
      socket.send(
        JSON.stringify({
          type: "error",
          error: "Suspicious movement detected. Move cancelled.",
        })
      );
      return;
    }
    logger.info("Player moved", {
      player: uuid,
      context: "move",
      x: message.x,
      y: message.y,
      z: message.z,
      roomId,
    });
    playerManager.broadcast(uuid, {
      type: "playerMoved",
      uuid,
      x: message.x,
      y: message.y,
      z: message.z,
    });
  },
};
