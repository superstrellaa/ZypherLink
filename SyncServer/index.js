require("dotenv").config();
const { createServer } = require("./core/server");
const logger = require("./utils/logger");

// Start WebSocket server
createServer();

logger.info("WebSocket server initialized", {
  context: "startup",
});
