const logger = require("../../utils/logger");

const queueState = {
  matchmakingQueue: [],
};

function addPlayerToQueue(uuid, socket) {
  const { matchmakingQueue } = queueState;
  if (findPlayerInQueue(uuid) !== -1) {
    return false;
  }
  matchmakingQueue.push({ uuid, socket });
  logger.info("Player added to matchmaking queue", {
    player: uuid,
    context: "matchmaking",
  });
  return true;
}

function findPlayerInQueue(uuid) {
  const { matchmakingQueue } = queueState;
  return matchmakingQueue.findIndex((p) => p.uuid === uuid);
}

function removePlayerFromQueue(uuid) {
  const { matchmakingQueue } = queueState;
  const idx = findPlayerInQueue(uuid);
  if (idx !== -1) {
    matchmakingQueue.splice(idx, 1);
    logger.info("Player removed from matchmaking queue", {
      player: uuid,
      context: "matchmaking",
    });
    return true;
  }
  return false;
}

module.exports = {
  queueState,
  addPlayerToQueue,
  findPlayerInQueue,
  removePlayerFromQueue,
};
