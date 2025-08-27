const roomManager = require("../../managers/roomManager");
const Logger = require("../../utils/logger");

module.exports = {
  type: "leaveQueue",
  handler: (uuid, socket, message, roomId, { playerManager }) => {
    const removed = roomManager.removePlayerFromRoom(uuid);
    Logger.info("Player left matchmaking queue", {
      player: uuid,
      context: "leaveQueue",
      removed,
    });
    // socket.send(JSON.stringify({ type: "queueLeft", success: !!removed }));
  },
};
