class Room {
  constructor(roomId) {
    this.roomId = roomId;
    this.players = new Set();
    this.lastActivity = Date.now();
    this.tickInterval = null;
  }

  addPlayer(uuid) {
    this.players.add(uuid);
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
