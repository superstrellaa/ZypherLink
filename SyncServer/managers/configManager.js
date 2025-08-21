const path = require("path");
const Joi = require("joi");

const schemas = {
  game: Joi.object({
    MAX_PLAYERS_PER_ROOM: Joi.number().integer().min(2).required(),
    TICK_RATE_MS: Joi.number().integer().min(1).required(),
    INACTIVITY_TIMEOUT_MS: Joi.number().integer().min(1000).required(),
    MAX_TELEPORT_DISTANCE: Joi.number().integer().min(1).required(),
    MAX_ROOMS: Joi.number().integer().min(1).required(),
  }),
  rateLimit: Joi.object({
    RATE_LIMIT_WINDOW_MS: Joi.number().integer().min(1).required(),
    RATE_LIMIT_MAX_MSGS: Joi.number().integer().min(1).required(),
  }),
  server: Joi.object({
    SERVER_PING_INTERVAL_MS: Joi.number().integer().min(1000).required(),
    SERVER_PING_TIMEOUT_MS: Joi.number().integer().min(1000).required(),
    MAX_MISSED_PONGS: Joi.number().integer().min(1).max(10).default(3),
  }),
  maps: Joi.array().items(
    Joi.object({
      name: Joi.string().required(),
      spawns: Joi.array()
        .items(
          Joi.object({
            x: Joi.number().required(),
            y: Joi.number().required(),
            z: Joi.number().required(),
          })
        )
        .min(1)
        .required(),
    })
  ),
};

const configCache = {};

function loadConfig(name) {
  if (configCache[name]) return configCache[name];
  let config;
  try {
    config = require(path.join("../config", name + ".js"));
  } catch (e) {
    throw new Error(`[ConfigManager] Config file not found: ${name}`);
  }
  if (schemas[name]) {
    const { error } = schemas[name].validate(config);
    if (error) {
      throw new Error(
        `[ConfigManager] Invalid config for ${name}: ${error.message}`
      );
    }
  }
  configCache[name] = config;
  return config;
}

module.exports = {
  get game() {
    return loadConfig("game");
  },
  get rateLimit() {
    return loadConfig("rateLimit");
  },
  get server() {
    return loadConfig("server");
  },
  get maps() {
    return loadConfig("maps");
  },
};
