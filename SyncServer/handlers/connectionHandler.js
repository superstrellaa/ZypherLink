const logger = require("../config/logger");
const { generateUUID } = require("../utils/uuid");
const playerManager = require("../managers/playerManager");
const handleMessage = require("./messageHandler");

const {
  SERVER_PING_INTERVAL_MS,
  SERVER_PING_TIMEOUT_MS,
} = require("../config/server");

function handleConnection(socket) {
  const uuid = generateUUID();
  const roomId = playerManager.addPlayer(uuid, socket);

  logger.info("Player connected", {
    player: uuid,
    context: "connectionHandler",
    roomId,
  });

  socket.send(JSON.stringify({ type: "init", uuid, roomId }));

  let pongReceived = true;
  let pingInterval = setInterval(() => {
    if (!pongReceived) {
      logger.warn("No pong from client, terminating connection", {
        player: uuid,
        context: "server-ping",
        roomId,
      });
      socket.terminate();
      clearInterval(pingInterval);
      return;
    }
    pongReceived = false;
    try {
      socket.send(JSON.stringify({ type: "ping" }));
    } catch (e) {
      logger.error("Failed to send server ping", {
        player: uuid,
        error: e.message,
      });
    }
  }, SERVER_PING_INTERVAL_MS);

  socket.on("message", (data) => {
    try {
      const msg = JSON.parse(data);
      if (msg && msg.type === "pong") {
        pongReceived = true;
        logger.debug &&
          logger.debug("Pong received from client", { player: uuid });
        return;
      }
    } catch {}
    handleMessage(uuid, socket, data);
  });

  socket.on("close", () => {
    playerManager.removePlayer(uuid);
    clearInterval(pingInterval);
    logger.info("Player disconnected", {
      player: uuid,
      context: "connectionHandler",
      roomId,
    });
  });

  socket.on("error", (err) => {
    logger.error("Socket error", {
      player: uuid,
      context: "connectionHandler",
      error: err.message,
      roomId,
    });
  });
}

module.exports = handleConnection;
