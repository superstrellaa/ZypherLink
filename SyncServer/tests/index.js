const WebSocket = require("ws");
const assert = require("assert");

const WS_URL = process.env.WS_URL || "ws://localhost:3000";
const TEST_TIMEOUT = 10000;

function delay(ms) {
  return new Promise((resolve) => setTimeout(resolve, ms));
}

async function testConnectionAndInit() {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(WS_URL);
    let timeout = setTimeout(() => reject(new Error("Timeout")), TEST_TIMEOUT);
    ws.on("open", () => {});
    ws.on("message", (data) => {
      const msg = JSON.parse(data);
      if (msg.type === "init") {
        assert.ok(msg.uuid, "UUID should be present");
        ws.close();
        clearTimeout(timeout);
        resolve();
      }
    });
    ws.on("error", reject);
  });
}

async function testPingPong() {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(WS_URL);
    let timeout = setTimeout(() => reject(new Error("Timeout")), TEST_TIMEOUT);
    let gotInit = false;
    ws.on("message", (data) => {
      const msg = JSON.parse(data);
      if (msg.type === "init") {
        gotInit = true;
        ws.send(JSON.stringify({ type: "ping" }));
      } else if (msg.type === "pong") {
        ws.close();
        clearTimeout(timeout);
        resolve();
      }
    });
    ws.on("error", reject);
    ws.on("close", () => {
      if (!gotInit) reject(new Error("Did not receive init"));
    });
  });
}

async function testMoveAndBroadcast() {
  return new Promise((resolve, reject) => {
    const ws1 = new WebSocket(WS_URL);
    const ws2 = new WebSocket(WS_URL);
    let uuid1 = null;
    let uuid2 = null;
    let ready = 0;
    let timeout = setTimeout(() => reject(new Error("Timeout")), TEST_TIMEOUT);

    ws1.on("message", (data) => {
      const msg = JSON.parse(data);
      if (msg.type === "init") {
        uuid1 = msg.uuid;
        ready++;
        if (ready === 2) sendMove();
      } else if (msg.type === "playerMoved") {
        assert.strictEqual(msg.x, 1);
        assert.strictEqual(msg.y, 2);
        assert.strictEqual(msg.z, 3);
        assert.strictEqual(msg.rotationY, 45);
        assert.strictEqual(msg.vx, 0.1);
        assert.strictEqual(msg.vy, 0.2);
        assert.strictEqual(msg.vz, 0.3);
        ws1.close();
        ws2.close();
        clearTimeout(timeout);
        resolve();
      }
    });
    ws2.on("message", (data) => {
      const msg = JSON.parse(data);
      if (msg.type === "init") {
        uuid2 = msg.uuid;
        ready++;
        if (ready === 2) sendMove();
      }
    });
    function sendMove() {
      ws2.send(
        JSON.stringify({
          type: "move",
          x: 1,
          y: 2,
          z: 3,
          rotationY: 45,
          vx: 0.1,
          vy: 0.2,
          vz: 0.3,
        })
      );
    }
    ws1.on("error", reject);
    ws2.on("error", reject);
  });
}

async function runAllTests() {
  console.log("[Test] Player Connection and Init...");
  await testConnectionAndInit();
  console.log("✔ Player Connection and Init passed");

  console.log("[Test] Ping/Pong...");
  await testPingPong();
  console.log("✔ Ping/Pong passed");

  console.log("[Test] Move and Broadcast...");
  await testMoveAndBroadcast();
  console.log("✔ Move and Broadcast passed");
}

if (require.main === module) {
  runAllTests()
    .then(() => {
      console.log("All tests passed!");
      process.exit(0);
    })
    .catch((err) => {
      console.error("Test failed:", err);
      process.exit(1);
    });
}
