const Joi = require("joi");

module.exports = {
  ping: Joi.object({
    type: Joi.string().valid("ping").required(),
  }),
  move: Joi.object({
    type: Joi.string().valid("move").required(),
    x: Joi.number().required(),
    y: Joi.number().required(),
    z: Joi.number().required(),
    rotationY: Joi.number().required(),
    vx: Joi.number().required(),
    vy: Joi.number().required(),
    vz: Joi.number().required(),
  }),
  joinQueue: Joi.object({
    type: Joi.string().valid("joinQueue").required(),
  }),
  leaveQueue: Joi.object({
    type: Joi.string().valid("leaveQueue").required(),
  }),
  auth: Joi.object({
    type: Joi.string().valid("auth").required(),
    token: Joi.string().required(),
  }),
};
