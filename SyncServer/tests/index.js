const WebSocket = require("ws");
const assert = require("assert");
const { spawn } = require("child_process");
const net = require("net");

const WS_URL = process.env.WS_URL || "ws://localhost:3000";
const TEST_TIMEOUT = 10000;
const SERVER_PORT = parseInt(process.env.PORT, 10) || 3000;

function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function waitForPort(port, timeout = 10000) {
  const start = Date.now();
  while (Date.now() - start < timeout) {
    if (await isPortOpen(port)) return true;
    await delay(200);
  }
  throw new Error(`Timeout waiting for port ${port}`);
}

function isPortOpen(port) {
  return new Promise((resolve) => {
    const socket = net.createConnection(port, "127.0.0.1");
    socket.once("connect", () => {
      socket.destroy();
      resolve(true);
    });
    socket.once("error", () => {
      resolve(false);
    });
  });
}

async function testPlayerConnection() {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(WS_URL);
    let receivedInit = false;
    ws.on("message", (data) => {
      const msg = JSON.parse(data);
      if (msg.type === "init") {
        receivedInit = true;
        assert(msg.uuid, "Should receive uuid");
        ws.close();
      }
    });
    ws.on("close", () => {
      if (receivedInit) resolve();
      else reject(new Error("Did not receive init"));
    });
    ws.on("error", reject);
    setTimeout(() => reject(new Error("Timeout")), TEST_TIMEOUT);
  });
}

async function testMatchmakingAndRoom() {
  return new Promise((resolve, reject) => {
    const ws1 = new WebSocket(WS_URL);
    const ws2 = new WebSocket(WS_URL);
    let gotStartGame = 0;
    let roomId = null;
    let players = null;
    function cleanup() {
      ws1.close();
      ws2.close();
    }
    function handleStartGame(msg) {
      if (msg.type === "startGame") {
        gotStartGame++;
        roomId = msg.roomId;
        players = msg.players;
        if (gotStartGame === 2) {
          assert(roomId, "RoomId should be present");
          assert(
            Array.isArray(players) && players.length === 2,
            "Should have 2 players"
          );
          cleanup();
        }
      }
    }
    ws1.on("message", (data) => {
      const msg = JSON.parse(data);
      handleStartGame(msg);
    });
    ws2.on("message", (data) => {
      const msg = JSON.parse(data);
      handleStartGame(msg);
    });
    ws1.on("close", () => {
      if (gotStartGame === 2) resolve();
      else reject(new Error("Did not get startGame for ws1"));
    });
    ws2.on("close", () => {
      if (gotStartGame === 2) resolve();
      else reject(new Error("Did not get startGame for ws2"));
    });
    ws1.on("error", reject);
    ws2.on("error", reject);
    setTimeout(() => {
      cleanup();
      reject(new Error("Timeout in matchmaking test"));
    }, TEST_TIMEOUT);
  });
}

async function testPlayerMoveBroadcast() {
  return new Promise((resolve, reject) => {
    const ws1 = new WebSocket(WS_URL);
    const ws2 = new WebSocket(WS_URL);
    let ws1uuid = null,
      ws2uuid = null;
    let ws1ready = false,
      ws2ready = false;
    let gotStartGame = 0;
    let gotStartPositions = 0;
    let moveReceived = false;
    function cleanup() {
      ws1.close();
      ws2.close();
    }
    function tryMove() {
      if (
        ws1ready &&
        ws2ready &&
        gotStartGame === 2 &&
        gotStartPositions === 2
      ) {
        // ws1 moves, ws2 should receive playerMoved
        ws2.on("message", (data) => {
          const msg = JSON.parse(data);
          if (msg.type === "playerMoved" && msg.uuid === ws1uuid) {
            moveReceived = true;
            cleanup();
          }
        });
        ws1.send(JSON.stringify({ type: "move", x: 1, y: 2, z: 3 }));
      }
    }
    ws1.on("message", (data) => {
      const msg = JSON.parse(data);
      if (msg.type === "init") {
        ws1uuid = msg.uuid;
        ws1ready = true;
      }
      if (msg.type === "startGame") gotStartGame++;
      if (msg.type === "startPositions") gotStartPositions++;
      tryMove();
    });
    ws2.on("message", (data) => {
      const msg = JSON.parse(data);
      if (msg.type === "init") {
        ws2uuid = msg.uuid;
        ws2ready = true;
      }
      if (msg.type === "startGame") gotStartGame++;
      if (msg.type === "startPositions") gotStartPositions++;
      tryMove();
    });
    ws1.on("close", () => {
      if (moveReceived) resolve();
      else reject(new Error("Move not broadcasted"));
    });
    ws2.on("close", () => {
      if (moveReceived) resolve();
      else reject(new Error("Move not broadcasted (ws2)"));
    });
    ws1.on("error", reject);
    ws2.on("error", reject);
    setTimeout(() => {
      cleanup();
      reject(new Error("Timeout in move broadcast test"));
    }, TEST_TIMEOUT);
  });
}

async function testPingPong() {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(WS_URL);
    let gotPing = false,
      gotPong = false;
    ws.on("message", (data) => {
      const msg = JSON.parse(data);
      if (msg.type === "ping") {
        gotPing = true;
        ws.send(JSON.stringify({ type: "pong" }));
      }
      if (msg.type === "pong") {
        gotPong = true;
        ws.close();
      }
    });
    ws.on("close", () => {
      if (gotPing || gotPong) resolve();
      else reject(new Error("Did not receive ping/pong"));
    });
    ws.on("error", reject);
    setTimeout(() => {
      ws.close();
      reject(new Error("Timeout in ping-pong test"));
    }, TEST_TIMEOUT);
  });
}

async function runAllTests() {
  console.log("Running: Player Connection Test...");
  await testPlayerConnection();
  console.log("✔ Player Connection Test passed");

  console.log("Running: Matchmaking & Room Test...");
  await testMatchmakingAndRoom();
  console.log("✔ Matchmaking & Room Test passed");

  console.log("Running: Player Move Broadcast Test...");
  await testPlayerMoveBroadcast();
  console.log("✔ Player Move Broadcast Test passed");

  console.log("Running: Ping-Pong Test...");
  await testPingPong();
  console.log("✔ Ping-Pong Test passed");

  console.log("All tests passed!");
}

async function main() {
  console.log("Starting backend server...");
  const backend = spawn(
    process.platform === "win32" ? "npm.cmd" : "npm",
    ["start"],
    {
      cwd: __dirname + "/..",
      stdio: "inherit",
      env: { ...process.env, PORT: SERVER_PORT },
    }
  );

  try {
    await waitForPort(SERVER_PORT, 15000);
    console.log("Backend is up. Running tests...");
    await runAllTests();
    console.log("All integration tests completed successfully.");
    backend.kill();
    process.exit(0);
  } catch (err) {
    console.error("Test failed:", err.message);
    backend.kill();
    process.exit(1);
  }
}

if (require.main === module) {
  main();
}
