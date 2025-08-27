const prisma = require("../db/client");
const logger = require("../utils/logger");

const ADMIN_BEARER_TOKEN =
  process.env.ADMIN_BEARER_TOKEN || "super_secret_admin_token";

async function postAdminMessage(req, res) {
  const authHeader = req.headers["authorization"];
  if (!authHeader || !authHeader.startsWith("Bearer ")) {
    logger.warn("Admin message: missing or invalid bearer token");
    return res.status(401).json({ error: "Unauthorized" });
  }

  const token = authHeader.split(" ")[1];
  if (token !== ADMIN_BEARER_TOKEN) {
    logger.warn("Admin message: invalid bearer token");
    return res.status(403).json({ error: "Forbidden" });
  }

  const { message } = req.body;
  if (!message || typeof message !== "string" || message.length === 0) {
    logger.warn("Admin message: missing or invalid message body");
    return res.status(400).json({ error: "Message is required" });
  }

  try {
    const adminMessage = await prisma.adminMessage.create({
      data: {
        message,
        sent: false,
      },
    });
    logger.info("Admin message created", { id: adminMessage.id, message });
    res
      .status(201)
      .json({ id: adminMessage.id, message: adminMessage.message });
  } catch (err) {
    logger.error("Error creating admin message", err);
    res.status(500).json({ error: "Internal Server Error" });
  }
}

module.exports = { postAdminMessage };
