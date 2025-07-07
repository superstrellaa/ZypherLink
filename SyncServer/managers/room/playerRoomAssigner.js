const matchmaking = require("../matchmaking");
const ConfigManager = require("../configManager");
const { sendStartGame } = require("./gameStarter");

function addPlayerToRoom(uuid, socket) {
  const added = matchmaking.addPlayerToQueue(uuid, socket);
  if (added) {
    matchmaking.processQueue();
    const roomId = matchmaking.playerToRoom.get(uuid);
    if (roomId) {
      const room = matchmaking.rooms.get(roomId);
      if (
        room &&
        room.players.size === ConfigManager.game.MAX_PLAYERS_PER_ROOM
      ) {
        sendStartGame(roomId);
      }
    }
    return roomId;
  }
  return null;
}

module.exports = { addPlayerToRoom };
