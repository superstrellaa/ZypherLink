const WebSocket = require("ws");
const logger = require("../utils/logger");
const handleConnection = require("../handlers/connectionHandler");
const { startRoomCleanup } = require("../managers/room/roomCleaner");
const matchmaking = require("../managers/matchmaking");
const replayLogger = require("../utils/replayLogger");
const { TICK_RATE_MS, INACTIVITY_TIMEOUT_MS } =
  require("../managers/configManager").game;

function createServer(port = process.env.PORT || 3000) {
  const server = new WebSocket.Server({ port });

  server.on("connection", (socket) => handleConnection(socket));

  startRoomCleanup(
    matchmaking,
    logger,
    replayLogger,
    TICK_RATE_MS,
    INACTIVITY_TIMEOUT_MS
  );

  logger.info("WebSocket server started and listening", {
    context: "server",
    port,
  });
}

module.exports = { createServer };
