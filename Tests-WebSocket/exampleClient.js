// --- ZeroPing Advanced Test Panel ---
const readline = require("readline");
const WebSocket = require("ws");

const WS_URL = process.env.WS_URL || "ws://localhost:3000";
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout,
  prompt: "[TestPanel] > ",
});

let clients = [];
let totalSent = 0;
let totalReceived = 0;
let lastSent = 0,
  lastReceived = 0;
let ppsInterval = null;
let moving = false;
let running = false;

function printPanelStatus() {
  process.stdout.write(
    "\r" +
      (running ? "🟢" : "🔴") +
      " TestPanel " +
      (running ? "RUNNING" : "STOPPED") +
      "      "
  );
}

function help() {
  console.log(`\nAvailable commands:`);
  console.log(
    `  start <n> <mode>         - Launch <n> bots in mode: realistic | flood | teleport`
  );
  console.log(
    `  add <mode>               - Add a single bot in mode: realistic | flood | teleport`
  );
  console.log(`  remove <id>              - Remove a bot by id`);
  console.log(`  move <id> <x> <y> <z>    - Move a bot manually`);
  console.log(`  moveall <x> <y> <z>      - Move all bots to position`);
  console.log(`  startmoving              - Start movement for all bots`);
  console.log(`  stopmoving               - Stop movement for all bots`);
  console.log(`  status                   - Show current statistics`);
  console.log(
    `  list                     - List all bots and their known players`
  );
  console.log(`  clear                    - Clear the console`);
  console.log(`  help                     - Show this help`);
  console.log(`  exit                     - Exit the panel\n`);
}

function randomWalk(pos) {
  return {
    x: pos.x + (Math.random() - 0.5) * 2,
    y: pos.y + (Math.random() - 0.5) * 2,
    z: pos.z + (Math.random() - 0.5) * 2,
  };
}
function teleportMove() {
  return {
    x: Math.floor(Math.random() * 200),
    y: Math.floor(Math.random() * 200),
    z: Math.floor(Math.random() * 200),
  };
}

function createBot(mode) {
  let pos = { x: 0, y: 0, z: 0 };
  const socket = new WebSocket(WS_URL);
  let uuid = null;
  let roomId = null;
  let sent = 0;
  let received = 0;
  let knownPlayers = [];
  let botIndex = clients.length;
  let timer = null;

  function updateBot() {
    clients[botIndex] = {
      socket,
      sent,
      received,
      pos,
      uuid,
      roomId,
      knownPlayers,
      timer,
      mode,
    };
  }

  socket.on("open", () => {});
  socket.on("message", (data) => {
    let msg;
    try {
      msg = JSON.parse(data);
    } catch {
      return;
    }
    received++;
    totalReceived++;
    if (msg.type === "init") {
      uuid = msg.uuid;
      roomId = msg.roomId;
      updateBot();
    } else if (msg.type === "startGame") {
      roomId = msg.roomId;
      knownPlayers = (msg.players || []).filter((id) => id !== uuid);
      updateBot();
      console.log(
        `\n[Bot ${botIndex}] ▶️ Game started in room ${roomId}. Players: [${msg.players.join(
          ", "
        )}]`
      );
    } else if (msg.type === "startPositions") {
      if (msg.positions && typeof msg.positions === "object") {
        console.log(`\n[Bot ${botIndex}] 🗺️ Start positions:`);
        Object.entries(msg.positions).forEach(([id, pos]) => {
          const isMe = id === uuid ? "(me)" : "";
          console.log(
            `  Player ${id} ${isMe} starts at (${pos.x}, ${pos.y}, ${pos.z})`
          );
          if (id === uuid)
            pos &&
              pos.x !== undefined &&
              pos.y !== undefined &&
              pos.z !== undefined &&
              botIndex in clients &&
              (clients[botIndex].pos = { x: pos.x, y: pos.y, z: pos.z });
        });
      }
    } else if (msg.type === "playerMoved") {
      if (msg.uuid !== uuid) {
        console.log(
          `\n[Bot ${botIndex}] saw player ${msg.uuid} move to (${msg.x},${msg.y},${msg.z})`
        );
      }
    } else if (msg.type === "ping") {
      socket.send(JSON.stringify({ type: "pong" }));
      sent++;
      totalSent++;
      updateBot();
    } else if (msg.type === "error") {
      console.log(`\n[Bot ${botIndex}] ❌ Error:`, msg.error);
    }
  });
  socket.on("close", () => {
    console.log(`\n[Bot ${botIndex}] disconnected.`);
  });
  socket.on("error", () => {
    console.log(`\n[Bot ${botIndex}] connection error.`);
  });

  clients.push({
    socket,
    sent,
    received,
    pos,
    uuid,
    roomId,
    knownPlayers,
    timer,
    mode,
  });
  printPanelStatus();
}

function startBots(n, mode) {
  stopBots();
  for (let i = 0; i < n; i++) createBot(mode);
  running = true;
  printPanelStatus();
  if (ppsInterval) clearInterval(ppsInterval);
  ppsInterval = setInterval(() => {
    if (!moving) return;
    const sentNow = totalSent - lastSent;
    const recvNow = totalReceived - lastReceived;
    lastSent = totalSent;
    lastReceived = totalReceived;
    process.stdout.write(
      `\r📦 PPS: sent=${sentNow}, received=${recvNow}, totalSent=${totalSent}, totalReceived=${totalReceived}      `
    );
  }, 1000);
  console.log(`\n🟢 Launched ${n} bots in mode ${mode}`);
  console.log(`Use 'startmoving' to begin movement for all bots.`);
}

function addBot(mode) {
  createBot(mode);
  running = true;
  printPanelStatus();
  console.log(`\n🟢 Added 1 bot in mode ${mode}`);
  console.log(`Use 'startmoving' to begin movement for all bots.`);
}

function removeBot(id) {
  const bot = clients[id];
  if (!bot) return console.log("Bot not found");
  if (bot.timer) clearInterval(bot.timer);
  if (bot.socket) bot.socket.close();
  clients.splice(id, 1);
  console.log(`\n🗑️ Bot ${id} removed.`);
}

function stopBots() {
  for (const bot of clients) {
    if (bot.timer) clearInterval(bot.timer);
    if (bot.socket) bot.socket.close();
  }
  clients = [];
  if (ppsInterval) clearInterval(ppsInterval);
  moving = false;
  running = false;
  printPanelStatus();
  console.log("\n🛑 All bots stopped.");
}

function startMoving() {
  if (moving) return console.log("Bots are already moving.");
  moving = true;
  clients.forEach((bot, i) => {
    if (bot.timer) clearInterval(bot.timer);
    let interval = 200;
    if (bot.mode === "flood") interval = 50;
    bot.timer = setInterval(() => {
      if (!moving) return;
      if (bot.socket.readyState === 1) {
        let move;
        if (bot.mode === "realistic") {
          bot.pos = randomWalk(bot.pos);
          move = { type: "move", ...bot.pos };
        } else if (bot.mode === "teleport") {
          move = { type: "move", ...teleportMove() };
        } else if (bot.mode === "flood") {
          move = {
            type: "move",
            x: Math.random() * 100,
            y: Math.random() * 100,
            z: Math.random() * 100,
          };
        } else {
          bot.pos = randomWalk(bot.pos);
          move = { type: "move", ...bot.pos };
        }
        bot.socket.send(JSON.stringify(move));
        bot.sent++;
        totalSent++;
      }
    }, interval);
  });
  printPanelStatus();
  console.log("\n▶️ Movement started for all bots.");
}

function stopMoving() {
  if (!moving) return console.log("Bots are not moving.");
  moving = false;
  clients.forEach((bot) => {
    if (bot.timer) clearInterval(bot.timer);
    bot.timer = null;
  });
  printPanelStatus();
  console.log("\n⏸️ Movement stopped for all bots.");
}

function status() {
  printPanelStatus();
  console.log(`\nActive bots: ${clients.length}`);
  console.log(`Total sent: ${totalSent}, Total received: ${totalReceived}`);
  clients.forEach((c, i) => {
    console.log(
      `  Bot ${i}: sent=${c.sent}, received=${
        c.received
      }, pos=(${c.pos.x.toFixed(2)},${c.pos.y.toFixed(2)},${c.pos.z.toFixed(
        2
      )}), uuid=${c.uuid || "?"}, roomId=${c.roomId || "?"}, knownPlayers=[${
        c.knownPlayers ? c.knownPlayers.join(", ") : ""
      }]`
    );
  });
}

function moveBot(id, x, y, z) {
  const bot = clients[id];
  if (!bot) return console.log("Bot not found");
  bot.pos = { x: Number(x), y: Number(y), z: Number(z) };
  bot.socket.send(JSON.stringify({ type: "move", ...bot.pos }));
  bot.sent++;
  totalSent++;
  console.log(`Bot ${id} moved to (${x},${y},${z})`);
}

function moveAll(x, y, z) {
  clients.forEach((bot, i) => {
    bot.pos = { x: Number(x), y: Number(y), z: Number(z) };
    bot.socket.send(JSON.stringify({ type: "move", ...bot.pos }));
    bot.sent++;
    totalSent++;
    console.log(`Bot ${i} moved to (${x},${y},${z})`);
  });
}

function listBots() {
  clients.forEach((bot, i) => {
    console.log(
      `Bot ${i}: uuid=${bot.uuid || "?"}, roomId=${
        bot.roomId || "?"
      }, knownPlayers=[${bot.knownPlayers ? bot.knownPlayers.join(", ") : ""}]`
    );
  });
}

console.clear();
console.log("Welcome to the ZeroPing Advanced Test Panel!");
printPanelStatus();
help();
rl.prompt();

rl.on("line", (line) => {
  const [cmd, ...args] = line.trim().split(/\s+/);
  switch (cmd) {
    case "start":
      if (args.length < 2)
        return console.log("Usage: start <n> <realistic|flood|teleport>");
      startBots(Number(args[0]), args[1]);
      break;
    case "add":
      if (args.length < 1)
        return console.log("Usage: add <realistic|flood|teleport>");
      addBot(args[0]);
      break;
    case "remove":
      if (args.length < 1) return console.log("Usage: remove <id>");
      removeBot(Number(args[0]));
      break;
    case "move":
      if (args.length < 4) return console.log("Usage: move <id> <x> <y> <z>");
      moveBot(Number(args[0]), args[1], args[2], args[3]);
      break;
    case "moveall":
      if (args.length < 3) return console.log("Usage: moveall <x> <y> <z>");
      moveAll(args[0], args[1], args[2]);
      break;
    case "startmoving":
      startMoving();
      break;
    case "stopmoving":
      stopMoving();
      break;
    case "status":
      status();
      break;
    case "list":
      listBots();
      break;
    case "clear":
      console.clear();
      printPanelStatus();
      break;
    case "help":
      help();
      break;
    case "exit":
      stopBots();
      process.exit(0);
      break;
    default:
      console.log("Unknown command. Type 'help' to see options.");
  }
  rl.prompt();
});
