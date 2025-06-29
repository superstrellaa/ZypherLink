const fs = require("fs");
const path = require("path");

const REPLAY_DIR = path.join(__dirname, "..", "replays");
if (!fs.existsSync(REPLAY_DIR)) {
  fs.mkdirSync(REPLAY_DIR, { recursive: true });
}

const replayStreams = new Map();

function getReplayStream(roomId) {
  if (!replayStreams.has(roomId)) {
    const filePath = path.join(REPLAY_DIR, `replay_${roomId}.log`);
    const stream = fs.createWriteStream(filePath, { flags: "a" });
    replayStreams.set(roomId, stream);
  }
  return replayStreams.get(roomId);
}

function logEvent(roomId, event) {
  const stream = getReplayStream(roomId);
  const entry =
    JSON.stringify({
      timestamp: Date.now(),
      ...event,
    }) + "\n";
  stream.write(entry);
}

function closeReplay(roomId) {
  const stream = replayStreams.get(roomId);
  if (stream) {
    stream.end();
    replayStreams.delete(roomId);
  }
}

module.exports = {
  logEvent,
  closeReplay,
};
