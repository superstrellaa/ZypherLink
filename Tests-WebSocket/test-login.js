const fetch = require("node-fetch");

async function testLogin() {
  const res = await fetch(
    `http://31.57.96.123:25607/auth/login`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({}),
    }
  );

  const data = await res.json();
  console.log("Token recibido:", data.token);
}

testLogin();
