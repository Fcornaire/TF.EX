# TF.Replay

Records TowerFall matches and plays them back — frame by frame, seekable, exportable as a GIF.

A replay is not a video: it stores the **inputs** of every frame plus periodic state snapshots, and the
game re-simulates from them

# Features

## Replay browser

Browse all replay saved. A replay is exported at the end of Versus and Trials (No quests) , configurable in the mod options.
A replay is exported by his month

<p align="center">
  <img src="../../images/replays.gif" alt="animated" />
</p>

## Playback

Launch a replay from the browser and watch the replay play

<p align="center">
  <img src="../../images/replay_play.gif" alt="animated" />
</p>

Press H to view Control options

<p align="center">
  <img src="../../images/replay_controls.png" alt="animated" />
</p>

You can pause, advance frame by frame, go back, restart , and the new star : the Seek bar !
It let you with your mouse directly seek to the frame you want

<p align="center">
  <img src="../../images/replay_seekbar.gif" alt="animated" />
</p>

You can also export gif : use 2 right clicks to select the frames frontier and press G to export the gif (the gif is going ro be next to your replay in the Replays folder next to your Towerfall game)

<p align="center">
  <img src="../../images/replay_export.gif" alt="animated" />
</p>

and the result

<p align="center">
  <img src="../../images/replay_export_result.gif" alt="animated" />
</p>

You can record all mods except quest

<p align="center">
  <img src="../../images/replay_trials.gif" alt="animated" />
</p>

# Install

1. Install [FortRise](https://github.com/Terria-K/FortRise) 5.3.0 (5.X version at least)
2. Extract the release into `Mods/`

Requires **[TF.State](../state/README.md)** (which is bundled automatically if you get the pre release zip)

# Recording

Recording is automatic — TF.Replay records local matches on its own, and [TF.EX](../../EX-API.md), if
installed, takes over for netplay matches. Either way the replay is exported at the end
of a match, to:

```
<TowerFall>/Replays/yyyy-MM/<timestamp>.tow
```

Monthly folders keep the list navigable

Which modes get recorded is a toggle in the mod's options page (`OPTIONS` → `MOD OPTIONS` → `TF REPLAY`):

- `RECORD LAST MAN STANDING`
- `RECORD HEADHUNTERS`
- `RECORD TEAM DEATHMATCH`
- `TRIALS`

## Controls

Press `H` in a replay for the same list on screen.

| Input       | Action                                      |
| ----------- | ------------------------------------------- |
| `Space`     | Pause / resume                              |
| `Left`      | Step back one frame                         |
| `Right`     | Step forward one frame (hold `Down` to run) |
| Left click  | Seek — hold and drag to scrub               |
| Right click | Mark GIF in, then out (a third restarts)    |
| `G`         | Export the marked range as a GIF            |
| `F1`        | Toggle hurtboxes (needs TF.EX)              |
| `Esc`       | Quit to the main menu                       |
| `H`         | Show / hide the controls panel              |

## GIF export

Right-click the seek bar twice to mark the range, then `G`. The GIF is written next to the replay:

```
<replay folder>/<replay name>_<inFrame>-<outFrame>.gif
```

320×240 at 2× scale, capped at 200 captured frames — a longer selection lowers the frame rate rather
than being truncated

# API

For mods that want to record or drive playback themselves. Resolve it through FortRise:

```C#
var replay = context.Interop.GetApi<IMyReplaySlice>("TF.Replay");
```

The surface covers recording (`BeginRecording`, `AddRecord`, `Export`), playback (`StartPlayback`,
`SeekTo`, `UpdatePlaybackControls`), queries (`GetInputsAtFrame`, `GetStateAtFrame`) and the metadata a
netplay host pushes in (seats, archers, teams, seed). See `TF.Replay.Domain/Ports/ITfReplayApi.cs`.

**The record-driver lease** `SetRecordDriver(yourModName)` tells TF.Replay you own the frame loop, and
it stands its own drivers down; pass `null` to release, on teardown _and_ on unload
