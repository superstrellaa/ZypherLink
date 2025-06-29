const roomManager = require("./roomManager");

function addPlayer(uuid, socket) {
  return roomManager.addPlayerToRoom(uuid, socket);
}

function removePlayer(uuid) {
  roomManager.removePlayerFromRoom(uuid);
}

function broadcast(senderUUID, message) {
  roomManager.broadcastToRoom(senderUUID, message);
}

module.exports = {
  addPlayer,
  removePlayer,
  broadcast,
};
