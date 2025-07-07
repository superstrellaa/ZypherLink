const Logger = require("../utils/logger");
const { generateUUID } = require("../utils/uuid");
const playerManager = require("../managers/playerManager");
const { startHeartbeat } = require("./heartbeat");
const { registerSocketEvents } = require("./socketEvents");

function handleConnection(socket) {
  const uuid = generateUUID();
  const roomId = playerManager.addPlayer(uuid, socket);

  Logger.info("Player connected", { player: uuid, context: "connection" });

  socket.send(JSON.stringify({ type: "init", uuid, roomId }));

  const heartbeat = startHeartbeat(socket, uuid);

  registerSocketEvents(socket, uuid, heartbeat.onPong, heartbeat.stop);
}

module.exports = handleConnection;
