const fetch = require("node-fetch");

async function testLogin() {
  const res = await fetch(
    `http://localhost:${process.env.PORT || 3001}/auth/login`,
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
