const Room = require("../../models/Room");

const roomState = {
  rooms: new Map(),
  playerToRoom: new Map(),
};

function getRoomByPlayer(uuid) {
  const { playerToRoom, rooms } = roomState;
  const roomId = playerToRoom.get(uuid);
  if (!roomId) return null;
  return rooms.get(roomId) || null;
}

function isPlayerInRoom(uuid) {
  const { playerToRoom, rooms } = roomState;
  const roomId = playerToRoom.get(uuid);
  if (!roomId) return false;
  const room = rooms.get(roomId);
  return !!(room && room.players.has(uuid));
}

function assignPlayerToRoom(uuid, roomId) {
  roomState.playerToRoom.set(uuid, roomId);
}

function removePlayerFromRoomMap(uuid) {
  roomState.playerToRoom.delete(uuid);
}

module.exports = {
  roomState,
  getRoomByPlayer,
  isPlayerInRoom,
  assignPlayerToRoom,
  removePlayerFromRoomMap,
};
