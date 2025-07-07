function findPlayerInQueue(queue, uuid) {
  return queue.findIndex((p) => p.uuid === uuid);
}

function isPlayerInRoom(playerToRoom, rooms, uuid) {
  const roomId = playerToRoom.get(uuid);
  if (!roomId) return false;
  const room = rooms.get(roomId);
  return !!(room && room.players.has(uuid));
}

module.exports = {
  findPlayerInQueue,
  isPlayerInRoom,
};
