const winston = require("winston");
const DailyRotateFile = require("winston-daily-rotate-file");
const path = require("path");

const jsonFormat = winston.format.combine(
  winston.format.timestamp({ format: "YYYY-MM-DD HH:mm:ss" }),
  winston.format.printf(({ timestamp, level, message, ...meta }) => {
    return JSON.stringify({
      timestamp,
      level,
      message,
      ...meta,
    });
  })
);

const consoleTransport = new winston.transports.Console({
  level: "debug",
  format: jsonFormat,
});

const fileTransport = new DailyRotateFile({
  dirname: path.join(__dirname, "..", "logs"),
  filename: "server-%DATE%.log",
  datePattern: "YYYY-MM-DD",
  zippedArchive: false,
  maxFiles: "14d",
  level: "info",
  format: jsonFormat,
});

const logger = winston.createLogger({
  level: "info",
  transports: [consoleTransport, fileTransport],
});

module.exports = logger;
