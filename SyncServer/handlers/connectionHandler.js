const Logger = require("../utils/logger");
const { generateUUID } = require("../utils/uuid");
const playerManager = require("../managers/playerManager");
const { startHeartbeat } = require("./heartbeat");
const { registerSocketEvents } = require("./socketEvents");

function handleConnection(socket) {
  const uuid = generateUUID();
  playerManager.addPlayer(uuid, socket);

  Logger.info("Player connected", {
    player: uuid,
    context: "connection",
  });

  socket.send(JSON.stringify({ type: "init", uuid }));

  const heartbeat = startHeartbeat(socket, uuid);

  registerSocketEvents(socket, uuid, heartbeat.onPong, heartbeat.stop);
}

module.exports = handleConnection;
