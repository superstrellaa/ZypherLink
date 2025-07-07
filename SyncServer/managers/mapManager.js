const ConfigManager = require("./configManager");

function getAllMaps() {
  return ConfigManager.maps;
}

function getRandomMap() {
  const maps = ConfigManager.maps;
  return maps[Math.floor(Math.random() * maps.length)];
}

module.exports = {
  getAllMaps,
  getRandomMap,
};
