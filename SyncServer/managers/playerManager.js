const Player = require("../models/Player");

const players = new Map();

function addPlayer(uuid, socket) {
  if (players.has(uuid)) return players.get(uuid);
  const player = new Player(uuid, socket);
  players.set(uuid, player);
  return player;
}

function getPlayer(uuid) {
  return players.get(uuid) || null;
}

function removePlayer(uuid) {
  players.delete(uuid);
}

function updatePlayerState(uuid, state) {
  const player = players.get(uuid);
  if (!player) return;
  player.position = {
    x: state.x,
    y: state.y,
    z: state.z,
  };
  player.rotationY = state.rotationY;
  player.velocity = {
    vx: state.vx,
    vy: state.vy,
    vz: state.vz,
  };
  player.lastActive = Date.now();
}

function getSocket(uuid) {
  const player = players.get(uuid);
  return player ? player.socket : null;
}

function getAllPlayers() {
  return Array.from(players.values());
}

module.exports = {
  addPlayer,
  getPlayer,
  removePlayer,
  updatePlayerState,
  getSocket,
  getAllPlayers,
  players,
};
