const { queueState } = require("./queue");
const { roomState, assignPlayerToRoom } = require("./roomMap");
const logger = require("../../utils/logger");
const Room = require("../../models/Room");
const { generateUUID } = require("../../utils/uuid");
const ConfigManager = require("../../managers/configManager");
const { MAX_PLAYERS_PER_ROOM, MAX_ROOMS } = ConfigManager.game;

function processQueue() {
  const { matchmakingQueue } = queueState;
  const { rooms } = roomState;
  while (
    matchmakingQueue.length >= MAX_PLAYERS_PER_ROOM &&
    rooms.size < MAX_ROOMS
  ) {
    const uuidsForRoom = matchmakingQueue.splice(0, MAX_PLAYERS_PER_ROOM);
    const roomId = generateUUID();
    const room = new Room(roomId);
    rooms.set(roomId, room);
    logger.info("New room created", {
      roomId,
      context: "matchmaking",
      players: uuidsForRoom,
    });
    for (const uuid of uuidsForRoom) {
      room.addPlayer(uuid);
      assignPlayerToRoom(uuid, roomId);
      const player = playerManager.getPlayer(uuid);
      if (player) player.roomId = roomId;
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

const playerManager = require("../playerManager");
module.exports = { processQueue };
