const { MAX_PLAYERS_PER_ROOM, MAX_ROOMS } = require("../../config/game");
const Room = require("../../models/Room");
const { generateUUID } = require("../../utils/uuid");
const logger = require("../../config/logger");

const matchmakingQueue = [];
const rooms = new Map();
const playerToRoom = new Map();

function addPlayerToQueue(uuid, socket) {
  matchmakingQueue.push({ uuid, socket });
  logger.info("Player added to matchmaking queue", {
    context: "matchmaking",
    player: uuid,
  });
  if (
    matchmakingQueue.length >= MAX_PLAYERS_PER_ROOM &&
    rooms.size < MAX_ROOMS
  ) {
    const playersForRoom = matchmakingQueue.splice(0, MAX_PLAYERS_PER_ROOM);
    const roomId = generateUUID();
    const room = new Room(roomId);
    rooms.set(roomId, room);
    for (const p of playersForRoom) {
      room.addPlayer(p.uuid, p.socket);
      playerToRoom.set(p.uuid, roomId);
      logger.info("Player added to new room", {
        context: "matchmaking",
        player: p.uuid,
        roomId,
      });
      p.socket.send(JSON.stringify({ type: "joinedRoom", roomId }));
    }
    return roomId;
  } else if (
    matchmakingQueue.length >= MAX_PLAYERS_PER_ROOM &&
    rooms.size >= MAX_ROOMS
  ) {
    logger.warn("Room creation blocked: MAX_ROOMS limit reached", {
      context: "matchmaking",
      maxRooms: MAX_ROOMS,
      queueLength: matchmakingQueue.length,
    });
  }
  return null;
}

function removePlayerFromRoom(uuid) {
  const idx = matchmakingQueue.findIndex((p) => p.uuid === uuid);
  if (idx !== -1) {
    matchmakingQueue.splice(idx, 1);
    logger.info("Player removed from matchmaking queue", {
      context: "matchmaking",
      player: uuid,
    });
    return;
  }
  const roomId = playerToRoom.get(uuid);
  if (!roomId) return;
  const room = rooms.get(roomId);
  if (!room) return;
  room.removePlayer(uuid);
  playerToRoom.delete(uuid);
  logger.info("Player removed from room", {
    context: "matchmaking",
    player: uuid,
    roomId,
  });
  if (room.isEmpty()) {
    logger.info("Room deleted (empty)", { context: "matchmaking", roomId });
    rooms.delete(roomId);
  }
}

function getRoomByPlayer(uuid) {
  const roomId = playerToRoom.get(uuid);
  if (!roomId) return null;
  return rooms.get(roomId);
}

module.exports = {
  addPlayerToQueue,
  removePlayerFromRoom,
  getRoomByPlayer,
  rooms,
  playerToRoom,
};
