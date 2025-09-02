const express = require("express");
const router = express.Router();
const { gameVersion } = require("../config");

router.get("/version", (req, res) => {
  res.json({ version: gameVersion });
});

module.exports = router;
