class Player {
  constructor(uuid, socket) {
    this.uuid = uuid;
    this.socket = socket;
    this.roomId = null;
    this.lastActive = Date.now();
    this.position = { x: 0, y: 0, z: 0 };
    this.rotationY = 0;
    this.velocity = { vx: 0, vy: 0, vz: 0 };
  }

  updateState({ x, y, z, rotationY, vx, vy, vz }) {
    this.position = { x, y, z };
    this.rotationY = rotationY;
    this.velocity = { vx, vy, vz };
    this.lastActive = Date.now();
  }
}

module.exports = Player;
