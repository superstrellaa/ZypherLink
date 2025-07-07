const ConfigManager = require("../managers/configManager");
const { SERVER_PING_INTERVAL_MS, MAX_MISSED_PONGS } = ConfigManager.server;
const logger = require("../utils/logger");

function startHeartbeat(socket, uuid) {
  let missedPongs = 0;

  const pingInterval = setInterval(() => {
    if (missedPongs >= MAX_MISSED_PONGS) {
      logger.warn("Too many missed pongs, terminating socket", {
        player: uuid,
      });
      socket.terminate();
      clearInterval(pingInterval);
      return;
    }

    try {
      socket.send(JSON.stringify({ type: "ping" }));
      missedPongs++;
    } catch (e) {
      logger.error("Failed to send ping", { player: uuid, error: e.message });
    }
  }, SERVER_PING_INTERVAL_MS);

  return {
    onPong: () => {
      missedPongs = 0;
    },
    stop: () => clearInterval(pingInterval),
  };
}

module.exports = { startHeartbeat };
