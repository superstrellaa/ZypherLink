const logger = require("../config/logger");
const playerManager = require("../managers/playerManager");
const roomManager = require("../managers/roomManager");
const fs = require("fs");
const path = require("path");
const Joi = require("joi");
const schemas = require("./messages/schemas");
const {
  RATE_LIMIT_WINDOW_MS,
  RATE_LIMIT_MAX_MSGS,
} = require("../config/rateLimit");
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

function sendValidationError(socket, uuid, type, details, messageObj) {
  logger.warn("Invalid message structure", {
    player: uuid,
    context: "messageHandler",
    messageType: type,
    message: messageObj,
    joi: details,
  });
  socket.send(
    JSON.stringify({
      type: "error",
      error: `Invalid structure for type: ${type}`,
      details: details && details.length ? details[0].message : undefined,
    })
  );
}

function handleMessage(uuid, socket, rawMessage) {
  // Rate limit per player
  const now = Date.now();
  const rl = rateLimitMap.get(uuid) || { count: 0, last: now };
  if (now - rl.last > RATE_LIMIT_WINDOW_MS) {
    rl.count = 0;
    rl.last = now;
  }
  rl.count++;
  rateLimitMap.set(uuid, rl);
  if (rl.count > RATE_LIMIT_MAX_MSGS) {
    logger.warn("Rate limit exceeded", { player: uuid });
    socket.send(
      JSON.stringify({ type: "error", error: "Rate limit exceeded" })
    );
    return;
  }

  let message;
  try {
    message = JSON.parse(rawMessage);
  } catch (err) {
    logger.warn("Invalid JSON received", {
      player: uuid,
      context: "messageHandler",
      rawMessage,
      roomId: roomManager.getRoomByPlayer(uuid)
        ? roomManager.getRoomByPlayer(uuid).roomId
        : null,
    });
    socket.send(
      JSON.stringify({ type: "error", error: "Invalid JSON format" })
    );
    return;
  }

  if (!message.type || typeof message.type !== "string") {
    logger.warn("Missing or invalid message type", {
      player: uuid,
      context: "messageHandler",
      message,
    });
    socket.send(
      JSON.stringify({
        type: "error",
        error: "Missing or invalid message type",
      })
    );
    return;
  }

  // Joi validation
  const schema = schemas[message.type];
  if (!schema) {
    logger.warn("Unknown message type", {
      player: uuid,
      context: "messageHandler",
      messageType: message.type,
    });
    socket.send(
      JSON.stringify({
        type: "error",
        error: `Unknown message type: ${message.type}`,
      })
    );
    return;
  }
  const { error } = schema.validate(message);
  if (error) {
    sendValidationError(socket, uuid, message.type, error.details, message);
    return;
  }

  // Server authority check (example for move)
  if (message.type === "move") {
    const room = roomManager.getRoomByPlayer(uuid);
    if (!room || !room.players.has(uuid)) {
      logger.warn("Move rejected: player not in room", { player: uuid });
      socket.send(JSON.stringify({ type: "error", error: "Not in room" }));
      return;
    }
  }

  const room = roomManager.getRoomByPlayer(uuid);
  const roomId = room ? room.roomId : null;
  const handler = messageHandlers[message.type];
  if (handler) {
    try {
      handler(uuid, socket, message, roomId, {
        logger,
        playerManager,
        roomManager,
      });
    } catch (err) {
      logger.error("Error in message handler", {
        player: uuid,
        context: "messageHandler",
        messageType: message.type,
        error: err.message,
        roomId,
      });
      socket.send(
        JSON.stringify({
          type: "error",
          error: `Internal server error in handler for type: ${message.type}`,
        })
      );
    }
  } else {
    logger.warn("Unknown message type", {
      player: uuid,
      context: "messageHandler",
      messageType: message.type,
      roomId,
    });
    socket.send(
      JSON.stringify({
        type: "error",
        error: `Unknown message type: ${message.type}`,
      })
    );
  }
}

module.exports = handleMessage;
