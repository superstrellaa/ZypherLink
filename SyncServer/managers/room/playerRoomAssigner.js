const matchmaking = require("../matchmaking");
const ConfigManager = require("../configManager");
const { sendStartGame } = require("./gameStarter");
const UNLIMITED_ROOM_ID = "unlimited-room";

function cleanupUnlimitedRoomIfNeeded() {
  if (!ConfigManager.game.roomUnlimited) {
    const unlimitedRoom = matchmaking.rooms.get(UNLIMITED_ROOM_ID);
    if (unlimitedRoom) {
      for (const uuid of unlimitedRoom.players) {
        matchmaking.removePlayerFromRoomMap(uuid);
      }
      matchmaking.rooms.delete(UNLIMITED_ROOM_ID);
    }
  }
}

cleanupUnlimitedRoomIfNeeded();

function addPlayerToRoom(uuid) {
  const playerManager = require("../../managers/playerManager");
  if (ConfigManager.game.roomUnlimited) {
    let room = matchmaking.rooms.get(UNLIMITED_ROOM_ID);
    if (!room) {
      const Room = require("../../models/Room");
      room = new Room(UNLIMITED_ROOM_ID);
      matchmaking.rooms.set(UNLIMITED_ROOM_ID, room);
    }
    room.addPlayer(uuid);
    matchmaking.assignPlayerToRoom(uuid, UNLIMITED_ROOM_ID);
    const player = playerManager.getPlayer(uuid);
    if (player) player.roomId = UNLIMITED_ROOM_ID;
    for (const otherUUID of room.players) {
      const otherPlayer = playerManager.getPlayer(otherUUID);
      if (
        otherPlayer &&
        otherPlayer.socket &&
        otherPlayer.socket.readyState === otherPlayer.socket.OPEN
      ) {
        otherPlayer.socket.send(
          JSON.stringify({
            type: "playerJoin",
            uuid,
          })
        );
      }
    }
    sendUnlimitedRoomInit(uuid, room);
    return UNLIMITED_ROOM_ID;
  } else {
    const added = matchmaking.addPlayerToQueue(uuid);
    if (added) {
      matchmaking.processQueue();
      const roomId = matchmaking.playerToRoom.get(uuid);
      if (roomId) {
        const room = matchmaking.rooms.get(roomId);
        if (
          room &&
          room.players.size === ConfigManager.game.MAX_PLAYERS_PER_ROOM
        ) {
          sendStartGame(roomId);
        }
      }
      return roomId;
    }
    return null;
  }
}

function sendUnlimitedRoomInit(uuid, room) {
  const playerManager = require("../../managers/playerManager");
  const player = playerManager.getPlayer(uuid);
  if (
    !player ||
    !player.socket ||
    player.socket.readyState !== player.socket.OPEN
  )
    return;

  const players = Array.from(room.players);
  player.socket.send(
    JSON.stringify({
      type: "startGame",
      roomId: room.roomId,
      players,
      map: "MainMap",
    })
  );

  const positions = {};
  for (const uuid of players) {
    const p = playerManager.getPlayer(uuid);
    if (p) {
      positions[uuid] = p.position;
    }
  }
  player.socket.send(
    JSON.stringify({
      type: "startPositions",
      positions,
    })
  );
}

module.exports = { addPlayerToRoom };
