const queue = require("./queue");
const roomMap = require("./roomMap");
const processor = require("./processor");

module.exports = {
  // Queue
  matchmakingQueue: queue.queueState.matchmakingQueue,
  addPlayerToQueue: queue.addPlayerToQueue,
  findPlayerInQueue: queue.findPlayerInQueue,
  removePlayerFromQueue: queue.removePlayerFromQueue,
  // Room Map
  rooms: roomMap.roomState.rooms,
  playerToRoom: roomMap.roomState.playerToRoom,
  getRoomByPlayer: roomMap.getRoomByPlayer,
  isPlayerInRoom: roomMap.isPlayerInRoom,
  assignPlayerToRoom: roomMap.assignPlayerToRoom,
  removePlayerFromRoomMap: roomMap.removePlayerFromRoomMap,
  // Processor
  processQueue: processor.processQueue,
};
