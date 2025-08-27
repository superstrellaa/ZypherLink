const WebSocket = require("ws");
const logger = require("../utils/logger");
const handleConnection = require("../handlers/connectionHandler");
const { startRoomCleanup } = require("../managers/room/roomCleaner");
const matchmaking = require("../managers/matchmaking");
const replayLogger = require("../utils/replayLogger");
const { TICK_RATE_MS, INACTIVITY_TIMEOUT_MS } =
  require("../managers/configManager").game;

const playerManager = require("../managers/playerManager");
const { PrismaClient } = require("../generated/prisma");
const prisma = new PrismaClient();

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

  setInterval(async () => {
    try {
      const adminMessages = await prisma.adminMessage.findMany({
        where: { sent: false },
      });
      if (adminMessages.length > 0) {
        const players = playerManager.getAllPlayers();
        for (const msg of adminMessages) {
          for (const player of players) {
            if (player.socket && player.socket.readyState === 1) {
              player.socket.send(
                JSON.stringify({ type: "adminMessage", content: msg.message })
              );
            }
          }
          await prisma.adminMessage.update({
            where: { id: msg.id },
            data: { sent: true, sentAt: new Date() },
          });
          logger.info("AdminMessage sended to all", {
            context: "adminMessage",
            id: msg.id,
            message: msg.message,
          });
        }
      }
    } catch (err) {
      logger.error("Error trying to send AdminMessages", {
        context: "adminMessage",
        error: err.message,
      });
    }
  }, 60000);
}

module.exports = { createServer };
