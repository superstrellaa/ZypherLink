require("dotenv").config();
const express = require("express");
const cors = require("cors");
const logger = require("./utils/logger");
const prisma = require("./db/client");

const authRoutes = require("./routes/auth");
const adminRoutes = require("./routes/admin");

const app = express();
const PORT = process.env.PORT || 3000;

app.use(cors());
app.use(express.json());

app.use((req, res, next) => {
  logger.info(`New incoming request`, {
    method: req.method,
    url: req.url,
    ip: req.ip,
  });
  next();
});

app.use("/auth", authRoutes);
app.use("/admin", adminRoutes);

app.listen(PORT, () => {
  logger.info(`SyncAPI running`, { port: PORT });

  setInterval(async () => {
    try {
      const deleted = await prisma.token.deleteMany({
        where: {
          expiresAt: {
            lt: new Date(),
          },
        },
      });
      if (deleted.count > 0) {
        logger.info(`Deleted expired tokens`, {
          count: deleted.count,
        });
      }
    } catch (err) {
      logger.error("Error cleaning expired tokens", err);
    }
  }, 60 * 1000);
});
