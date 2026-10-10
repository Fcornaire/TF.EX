# Changelog

## [Unreleased]

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
