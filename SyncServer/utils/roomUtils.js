function getRoomByPlayer(playerToRoom, rooms, uuid) {
  const roomId = playerToRoom.get(uuid);
  if (!roomId) return null;
  return rooms.get(roomId) || null;
}

function deleteRoomIfEmpty(rooms, roomId, onDelete) {
  const room = rooms.get(roomId);
  if (room && room.isEmpty()) {
    rooms.delete(roomId);
    if (typeof onDelete === "function") onDelete(roomId);
    return true;
  }
  return false;
}

module.exports = {
  getRoomByPlayer,
  deleteRoomIfEmpty,
};
