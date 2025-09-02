const ConfigManager = require("../managers/configManager");
const { MAX_TELEPORT_DISTANCE, MAX_VELOCITY, MAX_ROTATION_DELTA } =
  ConfigManager.game;

const playerLastState = new Map();

function isMoveSuspicious(uuid, newState) {
  const last = playerLastState.get(uuid);

  if (!last) {
    playerLastState.set(uuid, { ...newState });
    return false;
  }

  const dx = newState.x - last.x;
  const dy = newState.y - last.y;
  const dz = newState.z - last.z;
  const dist = Math.sqrt(dx * dx + dy * dy + dz * dz);
  if (dist > MAX_TELEPORT_DISTANCE) return true;

  const vxAbs = Math.abs(newState.vx);
  const vyAbs = Math.abs(newState.vy);
  const vzAbs = Math.abs(newState.vz);
  if (vxAbs > MAX_VELOCITY || vyAbs > MAX_VELOCITY || vzAbs > MAX_VELOCITY)
    return true;

  let rotDiff = Math.abs(newState.rotationY - last.rotationY) % 360;
  if (rotDiff > 180) rotDiff = 360 - rotDiff;
  if (rotDiff > MAX_ROTATION_DELTA) return true;

  playerLastState.set(uuid, { ...newState });
  return false;
}

function resetPlayer(uuid) {
  playerLastState.delete(uuid);
}

module.exports = {
  isMoveSuspicious,
  resetPlayer,
};
