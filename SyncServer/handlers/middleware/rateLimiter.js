module.exports = function rateLimiter(context) {
  const {
    uuid,
    socket,
    logger,
    rateLimitMap,
    RATE_LIMIT_WINDOW_MS,
    RATE_LIMIT_MAX_MSGS,
  } = context;
  const now = Date.now();
  const rl = rateLimitMap.get(uuid) || { count: 0, last: now };
  if (now - rl.last > RATE_LIMIT_WINDOW_MS) {
    rl.count = 0;
    rl.last = now;
  }
  rl.count++;
  rateLimitMap.set(uuid, rl);
  if (rl.count > RATE_LIMIT_MAX_MSGS) {
    logger.warn("Rate limit exceeded", { player: uuid });
    socket.send(
      JSON.stringify({ type: "error", error: "Rate limit exceeded" })
    );
    return false;
  }
  return true;
};
