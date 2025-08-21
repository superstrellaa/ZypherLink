const express = require("express");
const cors = require("cors");
require("dotenv").config();
const logger = require("./db/logger");

const authRoutes = require("./routes/auth");

const app = express();
const PORT = process.env.PORT || 3000;

app.use(cors());
app.use(express.json());

app.use((req, res, next) => {
  logger.info(`${req.method} ${req.url} from ${req.ip}`);
  next();
});

app.use("/auth", authRoutes);

app.listen(PORT, () => {
  logger.info(`SyncAPI running on http://localhost:${PORT}`);
});
