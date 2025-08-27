const jwt = require("jsonwebtoken");
const prisma = require("../db/client");
const logger = require("../utils/logger");

const JWT_SECRET = process.env.JWT_SECRET;

async function login(req, res) {
  try {
    const ip = req.ip;
    const token = jwt.sign({ ip }, JWT_SECRET, { expiresIn: "15m" });

    const expiresAt = new Date(Date.now() + 15 * 60 * 1000);

    logger.info(`User logged in`, { ip: ip, token: token });

    await prisma.token.create({
      data: {
        ip,
        token,
        expiresAt,
      },
    });

    res.json({ token });
  } catch (err) {
    logger.error("Login error", err);
    res.status(500).json({ error: "Internal Server Error" });
  }
}

module.exports = { login };
