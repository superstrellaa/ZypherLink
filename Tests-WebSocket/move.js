const WebSocket = require("ws");

// Conexión al servidor WebSocket
const socket = new WebSocket("ws://localhost:3000");

socket.on("open", () => {
  console.log("✅ Connected to server");

  // Simular movimiento cada 2 segundos
  setInterval(() => {
    const position = {
      type: "move",
      x: Math.floor(Math.random() * 100),
      y: Math.floor(Math.random() * 100),
      z: Math.floor(Math.random() * 100),
    };
    socket.send(JSON.stringify(position));
    console.log("📤 Sent move:", position);
  }, 2000);
});

socket.on("message", (data) => {
  const msg = JSON.parse(data);

  // 🔍 Mostrar todos los mensajes recibidos
  console.log("📨 Message from server:", msg);

  if (msg.type === "init") {
    console.log("🆔 Your UUID is:", msg.uuid);
  } else if (msg.type === "playerMoved") {
    console.log(
      `📦 Another player moved: UUID=${msg.uuid}, x=${msg.x}, y=${msg.y}, z=${msg.z}`
    );
  } else if (msg.type === "pong") {
    console.log("🏓 Received pong from server");
  }
});

socket.on("close", () => {
  console.log("🔴 Disconnected from server");
});

socket.on("error", (err) => {
  console.error("❌ WebSocket error:", err.message);
});
