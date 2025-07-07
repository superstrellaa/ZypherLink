const path = require("path");
const fs = require("fs");
const logger = require("../utils/logger");
const playerManager = require("../managers/playerManager");
const roomManager = require("../managers/roomManager");
const ConfigManager = require("../managers/configManager");
const schemas = require("./messages/schemas");
const ALLOWED_WHEN_NOT_IN_ROOM = ["ping", "help", "joinQueue"];
const { RATE_LIMIT_WINDOW_MS, RATE_LIMIT_MAX_MSGS } = ConfigManager.rateLimit;
const rateLimitMap = new Map();

// Dynamic message handler loader
const handlersDir = path.join(__dirname, "messages");
const messageHandlers = {};
fs.readdirSync(handlersDir).forEach((file) => {
  if (file.endsWith(".js") && file !== "schemas.js") {
    const mod = require(path.join(handlersDir, file));
    if (mod.type && typeof mod.handler === "function") {
      messageHandlers[mod.type] = mod.handler;
    }
  }
});

// Middlewares
const rateLimiter = require("./middleware/rateLimiter");
const validator = require("./middleware/validator");
const roomAccessChecker = require("./middleware/roomAccessChecker");
const dispatcher = require("./middleware/dispatcher");

function handleMessage(uuid, socket, rawMessage) {
  const context = {
    uuid,
    socket,
    rawMessage,
    logger,
    playerManager,
    roomManager,
    messageHandlers,
    schemas,
    ALLOWED_WHEN_NOT_IN_ROOM,
    rateLimitMap,
    RATE_LIMIT_WINDOW_MS,
    RATE_LIMIT_MAX_MSGS,
  };
  if (!rateLimiter(context)) return;
  if (!validator(context)) return;
  if (!roomAccessChecker(context)) return;
  dispatcher(context);
}

module.exports = handleMessage;
