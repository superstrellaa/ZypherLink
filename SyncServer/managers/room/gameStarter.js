const matchmaking = require("../matchmaking");
const logger = require("../../utils/logger");
const mapManager = require("../mapManager");
const { shuffleArray } = require("../../utils/shuffle");

function sendStartGame(roomId) {
  const room = matchmaking.rooms.get(roomId);
  if (!room) return;

  const players = Array.from(room.players.keys());

  const validMap = mapManager
    .getAllMaps()
    .find((m) => m.spawns.length >= players.length);

  if (!validMap) {
    logger.error("No valid map found with enough spawns", {
      context: "roomManager",
      playerCount: players.length,
    });
    return;
  }

  const shuffledSpawns = shuffleArray(validMap.spawns);
  const spawnAssignments = {};
  players.forEach((uuid, index) => {
    spawnAssignments[uuid] = shuffledSpawns[index];
  });

  const positions = {};
  players.forEach((uuid) => {
    positions[uuid] = spawnAssignments[uuid];
  });

  for (const [playerUUID, playerSocket] of room.players.entries()) {
    playerSocket.send(
      JSON.stringify({
        type: "startGame",
        roomId,
        players,
        map: validMap.name,
      })
    );
    playerSocket.send(
      JSON.stringify({
        type: "startPositions",
        positions,
      })
    );
  }

  room.metadata = {
    map: validMap.name,
    spawnAssignments,
  };

  logger.info("Game started", {
    context: "roomManager",
    roomId,
    map: validMap.name,
    players,
    spawnAssignments,
  });
}

module.exports = { sendStartGame };
