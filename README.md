# <img src="./docs/static/zypherlink-cover.png" alt="ZypherLink Cover" style="width:100%;max-width:900px;" />

<p align="center">
  <img src="https://img.shields.io/github/issues/superstrellaa/ZypherLink" alt="Issues">
  <img src="https://img.shields.io/github/license/superstrellaa/ZypherLink" alt="License">
  <img src="https://img.shields.io/github/last-commit/superstrellaa/ZypherLink" alt="Last Commit">
  <img src="https://img.shields.io/github/languages/top/superstrellaa/ZypherLink" alt="Top Language">
</p>

# ZypherLink (WIP)

**ZypherLink** is a modern, open-source multiplayer framework for Unity, designed for low-latency, scalable, and secure real-time games. It consists of:

- **SyncServer**: A robust Node.js WebSocket backend for authoritative multiplayer logic, replay logging, anti-cheat, and more.
- **SyncAPI**: A robust API backend maded in ExpressJS for JWT-Auth, versions check and admin messages.
- **ZeroPing**: A Unity C# client example for rapid prototyping and integration with SyncServer using **NativeWebSocket**

> ZypherLink still in progress and is continues testing, the actual source-code can include bugs or lack of optimization

---

## Features

- ⚡ Ultra-low latency WebSocket communication
- 🛡️ Server authority, anti-cheat, and replay logging
- 🧩 Modular, extensible backend (handlers, managers, config)
- 📈 Interactive test panel and stress tools
- 📝 Clear message protocol documentation
- 🐳 Docker-ready for easy deployment

---

## Project Structure

```
ZypherLink/
├── SyncAPI/         # Node.js backend API in ExpressJS
├── SyncServer/      # Node.js backend (WebSocket server)
│   ├── config/      # Config files (game, rateLimit, server)
│   ├── replays/     # Replay logs (auto-generated)
│   ├── logs/        # Server logs (auto-generated)
│   ├── ...
├── Tests-WebSocket/ # Node.js test clients and panels
│   └── exampleClient.js # Node.js Script for testing (deprecated and unused)
├── ZeroPing/        # Unity client example (see folder for details)
├── README.md
└── ...
```

---

## Quick Start | Linux | SyncAPI

```bash
# 1. Clone the repo
$ git clone https://github.com/superstrellaa/ZypherLink.git
$ cd ZypherLink/SyncAPI

# 2. Copy and edit environment variables
$ cp .env.example .env
$ nano .env

# 3. Build and run with Docker (recommended)
$ docker-compose up --build

# Or run locally
$ npm install
$ npm start

```

---

## Quick Start | Linux | SyncServer

```bash
# 1. Clone the repo
$ git clone https://github.com/superstrellaa/ZypherLink.git
$ cd ZypherLink/SyncServer

# 2. Copy and edit environment variables
$ cp .env.example .env
$ nano .env

# 3. Build and run with Docker (recommended)
$ docker-compose up --build

# Or run locally (Node.js 18+ required)
$ npm install
$ npm start
```

---

## Quick Start | Unity | ZeroPing

1. Clone the repository with git or downloading .zip
2. Download Unity 6 from [Unity Hub](https://unity.com/es/download)
3. Go to Add > Add project from disk
4. Select ZeroPing folder and open it
5. Once opened, start SyncServer and SyncAPI projects
6. Now you can press Play and see how functions

---

## Contributing

Pull requests and issues are welcome! Please open an issue for bugs, ideas, or questions.

---

## License

MIT © superstrellaa

---

<p align="center">
  <sub>Made with ❤️ for the Unity multiplayer community.</sub>
</p>
