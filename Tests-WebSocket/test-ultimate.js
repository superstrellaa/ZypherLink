const WebSocket = require("ws");
const fetch = require("node-fetch");

const API_URL = "http://213.21.245.123:25607/auth/login";
const WS_URL = "ws://213.21.245.123:25631";

const NUM_BOTS = 10000;
const MOVE_INTERVAL = 50; // ms entre cada "move"

async function login() {
  const res = await fetch(API_URL, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
  });
  const data = await res.json();
  if (!data.token) throw new Error("Login failed: " + JSON.stringify(data));
  return data.token;
}

class Bot {
  constructor(id, token) {
    this.id = id;
    this.token = token;
    this.ws = new WebSocket(WS_URL);

    // Posición inicial
    this.x = 0;
    this.y = -0.05;
    this.z = 0;
    this.speed = 5.9 + Math.random() * 0.2;
    this.stopping = false;

    // destino inicial
    this.target = this.getRandomTargetFromCurrent();

    this.ws.on("open", () => {
      this.send({ type: "auth", token });
      setTimeout(() => this.send({ type: "joinQueue" }), 500);
    });

    this.ws.on("message", (msg) => this.handleMessage(msg));
  }

  send(obj) {
    if (this.ws.readyState === WebSocket.OPEN) {
      this.ws.send(JSON.stringify(obj));
    }
  }

  handleMessage(msg) {
    const data = JSON.parse(msg);

    if (data.type === "ping") {
      this.send({ type: "pong" });
      return;
    }

    if (data.type === "startGame") {
      this.startMoving();
    }
  }

  startMoving() {
    setInterval(() => this.moveStep(), MOVE_INTERVAL);
  }

  moveStep() {
    if (this.stopping) {
      // Mandar tick parado antes de cambiar de destino
      this.send({
        type: "move",
        x: this.x,
        y: this.y,
        z: this.z,
        rotationY: this.rotationY || 0,
        vx: 0,
        vy: 0,
        vz: 0,
      });

      this.stopping = false;
      this.target = this.getRandomTargetFromCurrent();
      return;
    }

    const dx = this.target.x - this.x;
    const dz = this.target.z - this.z;
    const dist = Math.sqrt(dx * dx + dz * dz);

    if (dist < 0.2) {
      // Llegó al destino → parar un tick
      this.stopping = true;
      return;
    }

    const dirX = dx / dist;
    const dirZ = dz / dist;

    const step = this.speed * (MOVE_INTERVAL / 1000);

    this.x += dirX * step;
    this.z += dirZ * step;

    this.rotationY = Math.atan2(dirX, dirZ) * (180 / Math.PI);

    this.send({
      type: "move",
      x: this.x,
      y: this.y,
      z: this.z,
      rotationY: this.rotationY,
      vx: dirX * this.speed,
      vy: 0,
      vz: dirZ * this.speed,
    });
  }

  // Nuevo destino desde la posición actual
  getRandomTargetFromCurrent() {
    return {
      x: this.x + (Math.random() * 6 - 3), // máximo +/-3 unidades desde la posición actual
      z: this.z + (Math.random() * 6 - 3),
    };
  }
}

(async () => {
  const token = await login();
  console.log("Got token:", token);

  for (let i = 0; i < NUM_BOTS; i++) {
    new Bot(i, token);
    await new Promise((r) => setTimeout(r, 200));
  }
})();
