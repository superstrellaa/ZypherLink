const matchmaking = require("../matchmaking");
const logger = require("../../utils/logger");
const mapManager = require("../mapManager");
const { shuffleArray } = require("../../utils/shuffle");

function sendStartGame(roomId) {
  const room = matchmaking.rooms.get(roomId);
  if (!room) return;

  const players = Array.from(room.players);

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

  const playerManager = require("../../managers/playerManager");
  for (const playerUUID of room.players) {
    const player = playerManager.getPlayer(playerUUID);
    if (
      player &&
      player.socket &&
      player.socket.readyState === player.socket.OPEN
    ) {
      player.socket.send(
        JSON.stringify({
          type: "startPositions",
          positions,
        })
      );
      player.socket.send(
        JSON.stringify({
          type: "startGame",
          roomId,
          players,
          map: validMap.name,
        })
      );
    }
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
