module.exports = function roomAccessChecker(context) {
  const {
    uuid,
    socket,
    logger,
    playerManager,
    roomManager,
    message,
    ALLOWED_WHEN_NOT_IN_ROOM,
  } = context;
  let inRoom = false;
  if (typeof roomManager.getRoomByPlayer === "function") {
    const room = roomManager.getRoomByPlayer(uuid);
    inRoom = !!(
      room &&
      room.players &&
      room.players.has &&
      room.players.has(uuid)
    );
  }
  if (!inRoom && !ALLOWED_WHEN_NOT_IN_ROOM.includes(message.type)) {
    logger.warn("Action rejected: player not in room", {
      player: uuid,
      type: message.type,
    });
    socket.send(
      JSON.stringify({
        type: "error",
        error: "Not in room. Join a room to interact.",
      })
    );
    return false;
  }
  return true;
};
