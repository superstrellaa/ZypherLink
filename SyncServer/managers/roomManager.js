const { addPlayerToRoom } = require("./room/playerRoomAssigner");
const { broadcastToRoom } = require("./room/roomBroadcaster");
const { sendStartGame } = require("./room/gameStarter");
const { startRoomCleanup } = require("./room/roomCleaner");
const matchmaking = require("./matchmaking");

module.exports = {
  addPlayerToRoom,
  removePlayerFromRoom: matchmaking.removePlayerFromQueue,
  broadcastToRoom,
  getRoomByPlayer: matchmaking.getRoomByPlayer,
  startRoomCleanup,
  sendStartGame,
};
