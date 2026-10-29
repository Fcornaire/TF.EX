# Self-hosting

TF.EX uses the official matchmaking server by default, but you can run your own for a LAN, a local tournament or just to play on your own machine

The server is [tf-ex-matchmaking-server](https://github.com/Fcornaire/tf-ex-matchmaking-server)

## 1. Start the server

No configuration is needed, pick one:

- **Prebuilt**: download the zip matching your OS from the server [releases](https://github.com/Fcornaire/tf-ex-matchmaking-server/releases), extract it and run the binary inside
  - Windows: `tf-ex-matchmaking-server-<version>-windows-x64.zip`
  - Linux: `tf-ex-matchmaking-server-<version>-linux-x64-ubuntu24.04.zip`, run `./tf-ex-matchmaking-server`
  - Anything else: no prebuilt binary, build from source or use Docker
- **From source** (needs [Rust](https://rustup.rs)): clone the server repo and run `cargo run --release`
- **Docker**: `docker build -t tf-ex-matchmaking-server .` then `docker run -p 3000:3000 tf-ex-matchmaking-server`

It listens on port `3000` (TCP). Every setting is an optional environment variable, listed in the server [README](https://github.com/Fcornaire/tf-ex-matchmaking-server#configuration)

## 2. Let the other machines reach it (LAN)

1. Find the local IP of the machine running the server
2. Allow incoming connections on TCP port `3000`

## 3. Point TF.EX to it

In `OPTIONS` > TF.EX > `SERVER`:

- Open the field and type `ws://<server IP>:3000`, for example `ws://192.168.1.20:3000`

Then enter `NETPLAY` as usual

## Good to know

- **Same version for everyone**: the auto update and the latest version check are skipped on a non official server, so make sure every player runs the same TF.EX version (a mismatch can desync)
- **Over the internet**: see [Running behind a reverse proxy](https://github.com/Fcornaire/tf-ex-matchmaking-server#running-behind-a-reverse-proxy-tls)
