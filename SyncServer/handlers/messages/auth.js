const jwt = require("jsonwebtoken");
const { PrismaClient } = require("../../generated/prisma");
const prisma = new PrismaClient();
const Logger = require("../../utils/logger");

const JWT_SECRET = process.env.JWT_SECRET || "supersecret";

module.exports = {
  type: "auth",
  handler: async (
    uuid,
    socket,
    message,
    roomId,
    { playerManager, setAuthed, disconnect }
  ) => {
    const { token } = message;
    if (!token) {
      socket.send(JSON.stringify({ type: "error", error: "Missing token" }));
      disconnect();
      return;
    }
    let payload;
    try {
      payload = jwt.verify(token, JWT_SECRET);
    } catch (err) {
      Logger.warn("Invalid JWT", { player: uuid, error: err.message });
      socket.send(JSON.stringify({ type: "error", error: "Invalid token" }));
      disconnect();
      return;
    }
    if (payload.exp && Date.now() >= payload.exp * 1000) {
      socket.send(JSON.stringify({ type: "error", error: "Token expired" }));
      disconnect();
      return;
    }
    const tokenEntry = await prisma.token.findUnique({ where: { token } });
    if (!tokenEntry) {
      socket.send(
        JSON.stringify({ type: "error", error: "Token not found in DB" })
      );
      disconnect();
      return;
    }
    setAuthed();
    socket.send(JSON.stringify({ type: "authSuccess" }));
    Logger.info("Player authenticated", { player: uuid, context: "auth" });
  },
};
