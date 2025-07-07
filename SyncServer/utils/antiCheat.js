const ConfigManger = require("../managers/configManager");
const { MAX_TELEPORT_DISTANCE } = ConfigManger.game;
const playerLastPosition = new Map();

function isMoveSuspicious(uuid, newPos) {
  const last = playerLastPosition.get(uuid);
  if (!last) {
    playerLastPosition.set(uuid, { ...newPos });
    return false;
  }
  const dx = newPos.x - last.x;
  const dy = newPos.y - last.y;
  const dz = newPos.z - last.z;
  const dist = Math.sqrt(dx * dx + dy * dy + dz * dz);
  if (dist > MAX_TELEPORT_DISTANCE) {
    return true;
  }
  playerLastPosition.set(uuid, { ...newPos });
  return false;
}

function resetPlayer(uuid) {
  playerLastPosition.delete(uuid);
}

module.exports = {
  isMoveSuspicious,
  resetPlayer,
};
