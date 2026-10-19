# Changelog

## [Unreleased]

## [0.23.0] - 2026-10-20

### Ranked

- New RANKED mode in the netplay menu using Glicko-2 algorithm
- A ranked match is a best of 3 series that starts right away
- Wider Set players are only matched with other Wider Set players
- The series result moves your rating
- Your rating shows as ? until it has settled after a few series
- Leaving before the first game is a dodge: after two warnings, ranked gets locked for a while
- Leaving once a game has started forfeits the series
- New LeaderBoard in the ranked menu with the top 100 and your own rank

### Fixed

- After a netplay error you should now properly leave the lobby instead of staying in it
- [InputDisplayer] Inputs should now show at the right place in the instant replay with the Wider Set

## [0.22.2] - 2026-10-18

### Fixed

- A match should no longer start slightly desynced on one side on specific maps

## [0.22.1] - 2026-10-18

### Fixed

- [State] Fixed a desync when an archer gets stomped right after being hit by an arrow
- A spectator who never synchronize for whatever reason no longer stops a match from starting

## [0.22.0] - 2026-10-17

### Seamless server updates

- Server updates no longer end matches in progress
- Players in match will get a notice telling them how long they have left
- A match still running when that time is up gets disconnected

### New

- Every past changelog can now be read from the new CHANGELOG button in the netplay menu

### Fixed

- The instant replay should no longer freeze after a disconnect
- [State] A round should no longer end early after a rollback when an archer dies ducked under a low ceiling
- Custom archers without a hat should now spawn hatless for every player, even those without the mod

## [0.21.1] - 2026-10-15

### Fixed

- [State] Fixed a desync warning when brambles disappear, their sound now should plays after a rollback
- [Replay] Long replays no longer slow down when watching or seeking near the last rounds
- [State] A bolt arrow destroyed by a bolt catch shock should no longer come back after a rollback
- Co-op and other local modes no longer try to play online after visiting the online menus
- Gamepads no longer instantly skip the instant replay in local matches
- Failing to leave a lobby now brings you back to the lobby browser
- [Replay] Recording a match on a procedural tower keeps the seed you picked
- [Replay] Watching a replay should no longer count in your stats, Trials medals or Steam achievements
- [Replay] A replay that fails to start should no longer leave the game stuck in replay mode or unable to record
- [Replay] A replay whose tower is not installed now shows TOWER IS MISSING
- [Replay] Matches on Workshop towers are no longer recorded since they can't be replayed yet

## [0.21.0] - 2026-10-15

### Added

- Online play now requires the Dark World DLC

### Changed

- Improvement on sent network packets which should help a bit for unstable connection (pls, use an ethernet cable)
- Excluding a tower from the random pick is disabled online

### Fixed

- Fixed a desync warning tied with archer render
- Players who haven't unlocked Sunken City, Moonstone, TowerForge or Ascension should no longer end up on a different tower than the others
- [State] A swinging lantern that falls during a rollback no longer stays frozen in the air for one player
- [State] Fixed a potential desync after catching an arrow with a full quiver once that arrow is picked up
- [State] Fixed a potential desync when an archer dies right after shooting
- [State] Unlock events (Purple Archer, Moonstone moon) should no longer start during an online match

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
