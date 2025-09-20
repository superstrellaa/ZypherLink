module.exports = {
  type: "ping",
  validate: () => true,
  handler: (uuid, socket, message, roomId, { logger }) => {
    // If the client receives a ping from the server, it should respond with pong
    // If the client sends a ping, it should respond with pong
    socket.send(JSON.stringify({ type: "pong" }));
    /* logger.info("Pong sent", { player: uuid, context: "ping" }); */
  },
};
