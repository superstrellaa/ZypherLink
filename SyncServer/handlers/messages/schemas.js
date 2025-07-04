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
  }),
};
