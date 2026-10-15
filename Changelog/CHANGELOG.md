# Changelog

## [Unreleased]

## [0.21.0] - 2026-10-15

### Added

- Online play now requires the Dark World DLC

### Changed

- Improvement on sent network packets which should help a bit for unstable connection (pls, use an ethernet cable)

## [0.20.1] - 2026-10-14

### Fixed

- Fixed a 0 frame delay input that could start a match without archers
- The instant replay no longer freezes the game and shows a broken replay in some conditions

## [0.20.0] - 2026-10-12

### Added

- Players who can't connect directly to each other can now still play together: only the pairs that can't connect go through the server (Relayed), everyone else stays connected directly
- Connections between players are checked before a match starts to route properly depending on their connection
- An icon next to each ping shows how you reach that player: a plug for a direct connection or a server for a connection through the TF.EX server
- Spectators who can't reach the host directly watch through the TF.EX server (2 per lobby because thats CPU intensive, still uncapped when direct connection work )
- Temporarily, a match that desyncs, ends early or crashes will send appropriate metrics (logs + replays) to help fix the issue. This will be removed at some point (unless you want to pay me for storage ?)

### Changed

- Late spectators catch up much faster
- The in game pings fade out while an archer or an flying arrow passes behind them

### Fixed

- Spectating a match after a player left it (3 or 4 players) no longer gets stuck before the match start
- The instant replay now replays the screen shift of the space orb and the scrolling variants
- [State] Linux players should no longer get the tower's levels in a different order than their opponent
- [State] Levels should now be seeded the same way on Linux and Windows, removing one potential cause of desync between the 2 OS (Need testing pls)

## [0.19.2] - 2026-10-10

### Changed

- Connecting to an opponent now waits 30 seconds before giving up instead of 20
- Warnings about a player with modified game files or on a different OS (Windows / Linux) are now repeated at match start (please let me breathe with desynch :/ )

### Fixed

- Spectating a quickplay match no longer desyncs
- Spectators no longer freeze for a few seconds at the end of a round before the instant replay

## [0.19.1] - 2026-10-10

### Changed

- At the end of a netplay match, players who don't choose in time now go to archer select

### Fixed

- Auto update no longer leaves the previous TF.EX zip in the Mods folder, which made the game crash with two EX versions loaded
- Fixed a crash when a chest opens after the opponent disconnected mid round with instant replay

## [0.19.0] - 2026-10-09

### New

- Say hello to a new in game changelog
- Ping color in lobby will now also take into account lag spikes and packet loss for a better estimation of connection quality
- In a series, every player uses the host's input delay for a fair experience

### Fixed

- Quitting a netplay match now returns to the netplay menu
- Quitting a replay returns to the replay browser
