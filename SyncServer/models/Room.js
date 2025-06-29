class Room {
  constructor(roomId) {
    this.roomId = roomId;
    this.players = new Map();
    this.lastActivity = Date.now();
    this.tickInterval = null;
  }

  addPlayer(uuid, socket) {
    this.players.set(uuid, socket);
    this.lastActivity = Date.now();
  }

  removePlayer(uuid) {
    this.players.delete(uuid);
    this.lastActivity = Date.now();
  }

  isEmpty() {
    return this.players.size === 0;
  }
}

module.exports = Room;
