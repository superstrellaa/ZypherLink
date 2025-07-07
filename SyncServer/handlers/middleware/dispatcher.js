module.exports = function dispatcher(context) {
  const {
    uuid,
    socket,
    logger,
    playerManager,
    roomManager,
    messageHandlers,
    message,
  } = context;
  const room = roomManager.getRoomByPlayer(uuid);
  const roomId = room ? room.roomId : null;
  const handler = messageHandlers[message.type];
  if (typeof handler === "function") {
    try {
      handler(uuid, socket, message, roomId, {
        logger,
        playerManager,
        roomManager,
      });
    } catch (err) {
      logger.error("Error in message handler", {
        player: uuid,
        context: "dispatcher",
        messageType: message.type,
        error: err.message,
        roomId,
      });
      socket.send(
        JSON.stringify({
          type: "error",
          error: `Internal server error in handler for type: ${message.type}`,
        })
      );
      return false;
    }
    return true;
  } else {
    logger.warn("Unknown message type (dispatcher)", {
      player: uuid,
      context: "dispatcher",
      messageType: message.type,
      roomId,
    });
    socket.send(
      JSON.stringify({
        type: "error",
        error: `Unknown message type: ${message.type}`,
      })
    );
    return false;
  }
};
