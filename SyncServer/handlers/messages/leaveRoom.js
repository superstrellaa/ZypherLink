const roomManager = require("../../managers/roomManager");
const Logger = require("../../utils/logger");

module.exports = {
  type: "leaveRoom",
  handler: (uuid, socket, message, roomId, { playerManager, roomManager }) => {
    const room = roomManager.getRoomByPlayer(uuid);
    if (room && room.players && room.players.has(uuid)) {
      for (const otherUUID of room.players) {
        if (otherUUID !== uuid) {
          const otherSocket = playerManager.getSocket(otherUUID);
          if (otherSocket && otherSocket.readyState === otherSocket.OPEN) {
            otherSocket.send(
              JSON.stringify({ type: "playerDisconnected", uuid })
            );
          }
        }
      }

      room.players.delete(uuid);
      if (typeof roomManager.removePlayerFromRoom === "function") {
        roomManager.removePlayerFromRoom(uuid);
      }
      if (playerManager.getPlayer) {
        const player = playerManager.getPlayer(uuid);
        if (player) {
          player.position = { x: 0, y: 0, z: 0 };
          player.rotationY = 0;
          player.velocity = { vx: 0, vy: 0, vz: 0 };
        }
      }
      Logger.info("Player left room", {
        player: uuid,
        context: "leaveRoom",
        roomId: room.roomId,
      });
      socket.send(JSON.stringify({ type: "roomLefted", roomId: room.roomId }));

      if (room.roomId !== "unlimited-room" && room.players.size === 1) {
        const lastUUID = Array.from(room.players)[0];
        const lastSocket = playerManager.getSocket(lastUUID);
        if (lastSocket && lastSocket.readyState === lastSocket.OPEN) {
          lastSocket.send(
            JSON.stringify({ type: "roomDeleted", roomId: room.roomId })
          );
          Logger.info("Room Deleted message sent to player", {
            context: "roomManager",
            to: lastUUID,
            roomId: room.roomId,
          });
          room.players.delete(lastUUID);
          if (typeof roomManager.removePlayerFromRoom === "function") {
            roomManager.removePlayerFromRoom(lastUUID);
          }
        }
      }
    } else {
      socket.send(JSON.stringify({ type: "error", error: "Not in any room" }));
    }
  },
};
