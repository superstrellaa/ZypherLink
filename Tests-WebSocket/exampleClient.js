// --- ZeroPing Interactive Test Panel ---
const readline = require("readline");
const WebSocket = require("ws");

const WS_URL = process.env.WS_URL || "ws://localhost:3000";
const rl = readline.createInterface({
  input: process.stdin,
  output: process.stdout,
  prompt: "[TestPanel] > ",
});

let clients = [];
let moveTimers = [];
let totalSent = 0;
let totalReceived = 0;
let lastSent = 0,
  lastReceived = 0;
let ppsInterval = null;

function help() {
  console.log(`\nAvailable commands:`);
  console.log(
    `  start <n> <mode>    - Launch <n> bots in mode: realistic | flood | teleport`
  );
  console.log(`  stop                - Stop all bots and clear the test`);
  console.log(`  status              - Show current statistics`);
  console.log(`  move <id> <x> <y> <z> - Move a bot manually`);
  console.log(`  help                - Show this help`);
  console.log(`  exit                - Exit the panel\n`);
}

function randomWalk(pos) {
  // Realistic movement: small random step
  return {
    x: pos.x + (Math.random() - 0.5) * 2,
    y: pos.y + (Math.random() - 0.5) * 2,
    z: pos.z + (Math.random() - 0.5) * 2,
  };
}

function teleportMove() {
  // Abrupt movement (anti-cheat will catch it)
  return {
    x: Math.floor(Math.random() * 200),
    y: Math.floor(Math.random() * 200),
    z: Math.floor(Math.random() * 200),
  };
}

function startBots(n, mode) {
  stopBots();
  clients = [];
  moveTimers = [];
  totalSent = 0;
  totalReceived = 0;
  for (let i = 0; i < n; i++) {
    let pos = { x: 0, y: 0, z: 0 };
    const socket = new WebSocket(WS_URL);
    let uuid = null;
    let roomId = null;
    let sent = 0;
    let received = 0;
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
      } else if (msg.type === "ping") {
        socket.send(JSON.stringify({ type: "pong" }));
        sent++;
        totalSent++;
      } else if (msg.type === "error") {
        console.log(`[Bot ${i}] ❌ Error:`, msg.error);
      }
    });
    socket.on("close", () => {});
    socket.on("error", () => {});

    let interval = 200;
    if (mode === "flood") interval = 50;
    let timer = setInterval(() => {
      if (socket.readyState === 1) {
        let move;
        if (mode === "realistic") {
          pos = randomWalk(pos);
          move = { type: "move", ...pos };
        } else if (mode === "teleport") {
          move = { type: "move", ...teleportMove() };
        } else if (mode === "flood") {
          move = {
            type: "move",
            x: Math.random() * 100,
            y: Math.random() * 100,
            z: Math.random() * 100,
          };
        } else {
          pos = randomWalk(pos);
          move = { type: "move", ...pos };
        }
        socket.send(JSON.stringify(move));
        sent++;
        totalSent++;
      }
    }, interval);
    moveTimers.push(timer);
    clients.push({ socket, sent, received, pos });
  }
  if (ppsInterval) clearInterval(ppsInterval);
  ppsInterval = setInterval(() => {
    const sentNow = totalSent - lastSent;
    const recvNow = totalReceived - lastReceived;
    lastSent = totalSent;
    lastReceived = totalReceived;
    console.log(
      `📦 PPS: sent=${sentNow}, received=${recvNow}, totalSent=${totalSent}, totalReceived=${totalReceived}`
    );
  }, 1000);
  console.log(`✅ Launched ${n} bots in mode ${mode}`);
}

function stopBots() {
  for (const timer of moveTimers) clearInterval(timer);
  for (const c of clients) if (c.socket) c.socket.close();
  moveTimers = [];
  clients = [];
  if (ppsInterval) clearInterval(ppsInterval);
  console.log("🛑 All bots stopped.");
}

function status() {
  console.log(`\nActive bots: ${clients.length}`);
  console.log(`Total sent: ${totalSent}, Total received: ${totalReceived}`);
  clients.forEach((c, i) => {
    console.log(
      `  Bot ${i}: sent=${c.sent}, received=${
        c.received
      }, pos=(${c.pos.x.toFixed(2)},${c.pos.y.toFixed(2)},${c.pos.z.toFixed(
        2
      )})`
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

console.log("Welcome to the ZeroPing Test Panel!");
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
    case "stop":
      stopBots();
      break;
    case "status":
      status();
      break;
    case "move":
      if (args.length < 4) return console.log("Usage: move <id> <x> <y> <z>");
      moveBot(Number(args[0]), args[1], args[2], args[3]);
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
