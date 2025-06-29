const WebSocket = require("ws");
const logger = require("../config/logger");
const handleConnection = require("../handlers/connectionHandler");
const roomManager = require("../managers/roomManager");

function createServer(port = process.env.PORT || 3000) {
  const server = new WebSocket.Server({ port });

  server.on("connection", (socket) => handleConnection(socket));

  roomManager.startRoomTicking();

  logger.info("WebSocket server started", {
    context: "server",
    port,
  });
}

module.exports = { createServer };
