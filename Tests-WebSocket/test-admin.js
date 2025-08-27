const fetch = require("node-fetch");

const API_URL = "http://localhost:3001/admin/message";
const ADMIN_BEARER_TOKEN =
  "zypherlink_admin_token_ultra_secret_admin_asjdfklasjdfklñjaskdlñfjaskdlñfjadsklñjfsjfkplasjfdñlaksdfkj3490827834";

async function sendAdminMessage(message) {
  try {
    const res = await fetch(API_URL, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${ADMIN_BEARER_TOKEN}`,
      },
      body: JSON.stringify({ message }),
    });
    const data = await res.json();
    if (res.ok) {
      console.log("Mensaje enviado:", data);
    } else {
      console.error("Error:", res.status, data);
    }
  } catch (err) {
    console.error("Error:", err.message);
  }
}

sendAdminMessage("Hola desde el script de test!");
