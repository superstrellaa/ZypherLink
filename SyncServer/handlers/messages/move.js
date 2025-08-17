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
    if (
      antiCheat.isMoveSuspicious(uuid, {
        x: message.x,
        y: message.y,
        z: message.z,
        rotationY: message.rotationY,
        vx: message.vx,
        vy: message.vy,
        vz: message.vz,
      })
    ) {
      logger.warn("[AntiCheat] Suspicious move detected, move cancelled", {
        player: uuid,
        context: "move",
        ...message,
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

    const player = playerManager.getPlayer(uuid);
    if (player) {
      player.updateState({
        x: message.x,
        y: message.y,
        z: message.z,
        rotationY: message.rotationY,
        vx: message.vx,
        vy: message.vy,
        vz: message.vz,
      });
    }

    logger.info("Player moved", {
      player: uuid,
      context: "move",
      ...message,
      roomId,
    });

    roomManager.broadcastToRoom(uuid, {
      type: "playerMoved",
      uuid,
      x: message.x,
      y: message.y,
      z: message.z,
      rotationY: message.rotationY,
      vx: message.vx,
      vy: message.vy,
      vz: message.vz,
    });
  },
};
