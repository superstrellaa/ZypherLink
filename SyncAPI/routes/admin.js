const express = require("express");
const router = express.Router();
const { postAdminMessage } = require("../controllers/adminController");

router.post("/message", postAdminMessage);

module.exports = router;
