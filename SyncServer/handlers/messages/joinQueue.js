const roomManager = require("../../managers/roomManager");
const Logger = require("../../utils/logger");

module.exports = {
  type: "joinQueue",
  handler: (uuid, socket, message, roomId, { playerManager }) => {
    const assignedRoomId = roomManager.addPlayerToRoom(uuid);
    Logger.info("Player joined matchmaking queue", {
      player: uuid,
      context: "joinQueue",
      assignedRoomId,
    });
    if (assignedRoomId) {
      socket.send(
        JSON.stringify({ type: "roomAssigned", roomId: assignedRoomId })
      );
    }
  },
};
