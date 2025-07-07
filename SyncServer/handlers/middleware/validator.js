module.exports = function validator(context) {
  const { uuid, socket, rawMessage, logger, schemas } = context;
  let message;
  if (typeof rawMessage === "undefined" || rawMessage === null) {
    logger.warn("Received undefined/null message", { player: uuid });
    return false;
  }
  let str = rawMessage;
  if (Buffer.isBuffer(str)) str = str.toString("utf8");
  try {
    message = JSON.parse(str);
  } catch (err) {
    logger.warn("Invalid JSON received", {
      player: uuid,
      context: "validator",
      rawMessage: str,
    });
    socket.send(
      JSON.stringify({ type: "error", error: "Invalid JSON format" })
    );
    return false;
  }
  if (!message.type || typeof message.type !== "string") {
    logger.warn("Missing or invalid message type", {
      player: uuid,
      context: "validator",
      message,
    });
    socket.send(
      JSON.stringify({
        type: "error",
        error: "Missing or invalid message type",
      })
    );
    return false;
  }
  const schema = schemas[message.type];
  if (!schema) {
    logger.warn("Unknown message type", {
      player: uuid,
      context: "validator",
      messageType: message.type,
    });
    socket.send(
      JSON.stringify({
        type: "error",
        error: `Unknown message type: ${message.type}`,
      })
    );
    return false;
  }
  const { error } = schema.validate(message);
  if (error) {
    logger.warn("Invalid message structure", {
      player: uuid,
      context: "validator",
      messageType: message.type,
      message,
      joi: error.details,
    });
    socket.send(
      JSON.stringify({
        type: "error",
        error: `Invalid structure for type: ${message.type}`,
        details:
          error.details && error.details.length
            ? error.details[0].message
            : undefined,
      })
    );
    return false;
  }
  context.message = message; // Attach parsed/validated message to context
  return true;
};
