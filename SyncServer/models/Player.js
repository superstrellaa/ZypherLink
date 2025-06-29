class Player {
  constructor(uuid, socket) {
    this.uuid = uuid;
    this.socket = socket;
    this.roomId = null;
    this.lastActive = Date.now();
  }
}

module.exports = Player;
