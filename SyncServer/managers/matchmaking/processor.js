const { queueState } = require("./queue");
const { roomState, assignPlayerToRoom } = require("./roomMap");
const logger = require("../../utils/logger");
const Room = require("../../models/Room");
const { generateUUID } = require("../../utils/uuid");
const { deleteRoomIfEmpty } = require("../../utils/roomUtils");
const ConfigManager = require("../../managers/configManager");
const { MAX_PLAYERS_PER_ROOM, MAX_ROOMS } = ConfigManager.game;

function processQueue() {
  const { matchmakingQueue } = queueState;
  const { rooms } = roomState;
  while (
    matchmakingQueue.length >= MAX_PLAYERS_PER_ROOM &&
    rooms.size < MAX_ROOMS
  ) {
    const playersForRoom = matchmakingQueue.splice(0, MAX_PLAYERS_PER_ROOM);
    const roomId = generateUUID();
    const room = new Room(roomId);
    rooms.set(roomId, room);
    logger.info("New room created", {
      roomId,
      context: "matchmaking",
      players: playersForRoom.map((p) => p.uuid),
    });
    for (const p of playersForRoom) {
      room.addPlayer(p.uuid, p.socket);
      assignPlayerToRoom(p.uuid, roomId);
      logger.info("Player added to new room", {
        context: "matchmaking",
        player: p.uuid,
        roomId,
      });
      p.socket.send(JSON.stringify({ type: "joinedRoom", roomId }));
    }
  }
  if (
    matchmakingQueue.length >= MAX_PLAYERS_PER_ROOM &&
    rooms.size >= MAX_ROOMS
  ) {
    logger.warn("Room creation blocked: MAX_ROOMS limit reached", {
      context: "matchmaking",
      maxRooms: MAX_ROOMS,
      queueLength: matchmakingQueue.length,
    });
  }
}

module.exports = { processQueue };
