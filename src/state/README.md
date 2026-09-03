# TF.State

A library mod. It has no gameplay of its own: it knows how to **save and restore a TowerFall level**.

`GetState` walks the live `Level`, every tracked entity, the players and their physics flags, arrows,
pickups, chests, the per-round data, the session, the RNG, and serializes it to an opaque `byte[]`.
`LoadState` puts it back, exactly, on any later frame.

Two mods are built on it:

- [TF.EX](../../EX-API.md) — rollback netplay. Rollback _is_ save/restore, several times a frame.
- [TF.Replay](../replay/README.md) — recording, playback and frame-accurate seeking.

If you write a mod that adds state of its own, TF.State is the mod you talk to so that state survives a
rollback or a replay seek — whether or not the player has TF.EX installed.

# Install

1. Install [FortRise](https://github.com/Terria-K/FortRise) 5.3.0
2. Extract the release into `Mods/`

TF.State is also pulled in as a dependency by TF.EX and TF.Replay, so you usually already have it.

# API

## Mod interop

The everyday surface, imported by name with MonoMod's `ModInterop`:

```C#
 /// <summary>
 /// Register custom SaveState/LoadState events for a variant.
 ///
 /// <para>Those are used by the rollback system to properly save/load variant custom properties</para>
 /// </summary>
public static Action<Mod, string, Func<byte[]>, Action<byte[]>> RegisterVariantStateEvents;
```

```C#
 /// <summary>
 /// Stop receiving SaveState/LoadState events for a variant.
 /// </summary>
public static Action<Mod, string> UnregisterVariantStateEvents;
```

```C#
 /// <summary>
 /// Bracket gameplay randomness so every peer draws the same numbers.
 /// </summary>
public static Action RegisterRng;
public static Action UnregisterRng;

 /// <summary>
 /// True while the rollback system is replaying frames it has already run once.
 /// </summary>
public static Func<bool> ShouldFreezeCosmetics;
```

**Each variant has to claim rollback support for itself**: EX hides a modded variant in netplay unless state events have been registered under that variant's own id.

Most variants have nothing of their own to save: they only change behaviour, and their effects land in state
TF.State already carries. Register them anyway, with an empty payload:

```C#
foreach (var variant in StatelessVariants)
{
    RegisterVariantStateEvents(this, variant, static () => [], static _ => { });
}
```

Registering at least one key also lets your mod's pickups through the treasure spawner, which drops anything
it cannot account for.

`RegisterRng` / `UnregisterRng` matter whenever your mod makes a decision both peers must agree on. Inside
the bracket `Monocle.Calc.Random` is the tracked RNG, whose state rides in the snapshot, so the draw
replays identically after a rollback and matches on every machine.

`ShouldFreezeCosmetics` is for untracked visual entities. Their `Update` would otherwise run several times
per real frame during a replay and animate at the speed of light, and the code that spawned them would run
again and duplicate them. Guard both the spawn and the `Update` with it. Anything that affects the
simulation must be saved through `RegisterVariantStateEvents` instead.

See `TF.State.Domain/Ports/ITfStateApi.cs` for the full surface.

## Reading a state

A state is an opaque `byte[]` for the reason above: the model cannot cross the mod boundary. So
TF.State decodes on your behalf and hands back plain types:

```C#
int      GetFrameOf(byte[] state);
string[] DescribePlayers(byte[] state);              // "index;position;speed;state" per player
string   DescribeState(byte[] state, int maxDepth);  // whole blob as text, 0 = default depth
string   CompareStates(byte[] a, byte[] b);
string   DumpEntities();                             // the LIVE level, not a blob
```

`DescribeState` is the escape hatch: it walks the decoded model and prints every field, capped at 64
entries per collection. It is for looking, not for parsing — the shape is the model's and changes with
it. If your mod needs a specific value on an ongoing basis, ask for a typed member instead and it can
be added to the API.

Note that you usually do **not** need any of this: the live `Level` and its entities are reachable
directly from your own mod.

# Modded variants

TF.State can carry custom variants through rollback and replay as long as the mod is made compatible.

It's your responsibility to make sure the mod is netplay compatible (Ask the mod author).

## Making a custom variant compatible (For developers)

This guide will help you implement your modded variant for netplay sessions.

Note that this isn't a simple process as both mods can conflict.

A full, working reference lives in [Additional Variants](https://github.com/FortRise/ExampleFortRiseMod/tree/main/AdditionalVariants):
everything under its `TFState/` folder is the integration described here.

### Prerequisites

There are 2 rules of thumb for making your mod compatible:

- Your custom variant acts deterministically. That means applying the same input on an X frame always results in the same Y frame every time.

- You are only saving/loading the custom part of your variant. That means you shouldn't try (for example) to save the player's position, because it's already done by TF.State. You should only focus on the things that are specific to your mod.

### Context

Although I recommend having at least a basic knowledge of how rollback netcode works (there are some great explanations/videos on the web), it's not mandatory for making the mod compatible.

TF.State already manages the work of saving/loading important pieces of information (State), it only needs what should be saved/loaded from your mod.

It uses an interop API to be able to let your mod interact with it.

```C#
public static Action<Mod, string, Func<byte[]>, Action<byte[]>> RegisterVariantStateEvents;
```

This function takes 4 parameters:

- A Mod (aka your core mod) used for identification
- A string that is the name of the modded variant
- A function that returns a byte[] (SaveState)
- A function that takes a byte[] (LoadState)

Let's look at the last two since they are more important:

- The `SaveState` delegate: a function that will return a serialized version of the custom variant state as a byte[].

That means it's your responsibility to know what your custom variant adds to the game.

- The `LoadState` delegate: a function that takes a serialized version of the mod state and expects you to load it.

The payload is opaque to TF.State: it rides inside the snapshot, so it must be deterministic and must
not depend on anything outside the simulation (wall-clock time, `Calc.Random` outside a tracked bracket,
the local player's settings...).

### Implementation

#### Import the mod

This is straightforward. Create a class like this one:

```C#
    [ModImportName("TF.State.API")]
    public static class TfStateAPIModImports
    {
        public static Action<Mod, string, Func<byte[]>, Action<byte[]>>? RegisterVariantStateEvents;
        public static Action<Mod, string>? UnregisterVariantStateEvents;

        static TfStateAPIModImports()
        {
            typeof(TfStateAPIModImports).ModInterop();
        }
    }
```

`[ModImportName("TF.State.API")]` is for MonoMod to be able to find the TF.State mod.

`static TfStateAPIModImports()` is just the constructor that will automatically make MonoMod detect TF.State mod.

Declare only the delegates you use. A delegate stays **null** when TF.State is not installed, which is
also how you detect it:

```C#
if (TfStateAPIModImports.RegisterVariantStateEvents is null)
{
    return;
}
```

Add TF.State to your `meta.json` so FortRise loads it first. Use `optionalDependencies` if your mod must
keep working without it:

```json
"optionalDependencies": [ { "name": "TF.State", "version": "0.9.7" } ]
```

#### Register the custom Save/Load delegate

Call the previous delegate with something like this:

```C#
TfStateAPIModImports.RegisterVariantStateEvents(this, "customVariantName", OnSaveState, OnLoadState);
```

with `OnSaveState` being your save state delegate and `OnLoadState` being your load state delegate.

Note the register function should be called **after** all mods finish loading, not while the mod is
loading — from your module's `OnInitialize`, not its constructor.

```C#
public sealed class MyModule : Mod
{
    public MyModule(IModContent content, IModuleContext context, ILogger logger) : base(content, context, logger)
    {
        OnInitialize = _ =>
        {
            TfStateAPIModImports.RegisterVariantStateEvents?.Invoke(this, "MyVariant", OnSaveState, OnLoadState);
        };
    }
}
```

One key per variant. The key is scoped to your mod (TF.State stores it as `<modName>-<key>`).

### A working example

[Additional Variants](https://github.com/FortRise/ExampleFortRiseMod/tree/main/AdditionalVariants) adds
23 variants; 5 of them own state that TF.State cannot know about, and each gets its own key. Four are
worth walking through because each one is a _different_ reason a variant needs an entry.

#### 1. State you made — Jester's Hat

Jester's Hat stashes a `List<Vector2>` of warp points and the last warp used on every player, through
`DynamicData`. Nothing in TF.State knows those keys exist, so they are yours to carry. The list order
matters too: the teleport sorts it in place with a comparator that isn't a total order, so the order at
rest is part of the state.

```C#
internal static class JesterHatState
{
    public const string Name = "JestersHat";

    public static byte[] OnSaveState()
    {
        var level = Engine.Instance?.Scene as Level;
        if (level is null)
        {
            return [];
        }

        var carriers = level.Players
            .OfType<Player>()
            .OrderBy(player => player.PlayerIndex)
            .Select(player => (Player: player, Data: DynamicData.For(player)))
            .Where(entry => entry.Data.TryGetValue<List<Vector2>>("warpPoints", out _))
            .ToList();

        if (carriers.Count == 0)
        {
            return [];
        }

        return StateBuffer.Save(writer =>
        {
            writer.Write(carriers.Count);

            foreach (var (player, data) in carriers)
            {
                data.TryGetValue<List<Vector2>>("warpPoints", out var warpPoints);
                data.TryGetValue<Vector2>("lastWarpPoint", out var lastWarpPoint);

                writer.Write(player.PlayerIndex);
                writer.Write(warpPoints.Count);

                foreach (var warpPoint in warpPoints)
                {
                    writer.WriteVector2(warpPoint);
                }

                writer.WriteVector2(lastWarpPoint);
            }
        });
    }

    public static void OnLoadState(byte[] state)
    {
        // read the same layout back, then, per player index:
        // data.Set("warpPoints", warpPoints);
        // data.Set("lastWarpPoint", lastWarpPoint);
    }
}
```

`StateBuffer` there is a `MemoryStream` + `BinaryWriter`/`BinaryReader` wrapper.
You obviously have the choice to choose what ever you want.

#### 2. A component you attach — Dash Stamina

Dash Stamina add a `DashStamina` component off the player and its gauge gates dodging, so the gauge is
gameplay state. Save the gauge, not the component.

The subtlety is on the way back in. TF.State restores a player **in place** when that player still exists,
so your component survives; but a player who was gone at the target frame is **re-created**, and its
`Added()` runs, so a `Player.Added` hook of yours fires again and hands you a fresh component with a
fresh gauge. So a classic case, handle both! Take the component if it is there, build it if it isn't, then write the value.

```C#
var data = DynamicData.For(player);

if (!data.TryGetValue<DashStamina>("dashStamina", out var stamina))
{
    stamina = new DashStamina(true, true);
    player.Add(stamina);
    data.Set("dashStamina", stamina);
}

stamina.Bar = toLoad.Bar;
```

#### 3. A vanilla field TF.State only carries for the entity that normally owns it — Drilling Arrow

`Arrow.HasDrilled` and `Arrow.NaivePush` live on the `Arrow` base class, but only a Drill Arrow ever sets
them in vanilla, so that is the only arrow TF.State round-trips them for. Drilling Arrow makes _every_
arrow drill, so after a load a normal arrow comes back with `HasDrilled == false` and drills a second
time. The variant that widened the field's reach is the one that has to carry it.

This is the check worth making for every variant: not "is this field mine?" but "does TF.State capture it
**for this entity**?". You can look at `TF.State.TowerFallExtensions` for the field before you assume it is covered.

Arrows are always deleted and re-created by a load, so match them by `actualDepth`, the identity
TF.State itself restores:

```C#
foreach (var arrow in level[GameTags.Arrow].OfType<Arrow>())
{
    var data = DynamicData.For(arrow);

    if (!saved.TryGetValue(data.Get<double>("actualDepth"), out var toLoad))
    {
        continue;
    }

    data.Set("HasDrilled", toLoad.HasDrilled);
    arrow.NaivePush = toLoad.NaivePush;
}
```

#### 4. Something that cannot be serialized at all — Fading Arrow

Fading Arrow calls `arrow.Flash(60, onFinish)`, where `onFinish` removes the arrow. TF.State carries the
flash timer (it is vanilla `LevelEntity` state) but a **delegate is not data**, and the re created arrow
comes back with a null callback: the timer expires, nothing happens, and the arrow lives on.

The fix is not to serialize anything — it is to rebuild the callback from state that is already there. Its
`OnSaveState` returns an empty array and only `OnLoadState` does any work:

```C#
public static byte[] OnSaveState() => [];

public static void OnLoadState(byte[] state)
{
    foreach (var arrow in level[GameTags.Arrow].OfType<Arrow>())
    {
        if (arrow is LaserArrow || !arrow.Flashing || arrow.PlayerIndex < 0)
        {
            continue;
        }

        if (!Variants.FadingArrow.IsActive(arrow.PlayerIndex))
        {
            continue;
        }

        var fading = arrow;
        DynamicData.For(arrow).Set("onFinish", (Action)(() =>
        {
            fading.StopFlashing();
            fading.RemoveSelf();
        }));
    }
}
```

An empty payload is fine and costs nothing, it never moves the checksum.

### Rules that fall out of the above

- **Write a deterministic layout.** Order collections by something stable (`PlayerIndex`, `actualDepth`).
- **Return an empty array when your variant is off.** Every registered key is called on every capture.
- **Save only what TF.State does not already carry for that entity**, you should verify that.
- **Match re-created entities by `actualDepth`.** Players are restored in place when they still exist and
  re-created (with `Added()`) otherwise; arrows, chests, corpses and pickups are always re-created.
- **Your handlers run last.** `LoadStates` is the final step of `Level.LoadState`, so the whole level is
  already in its target shape when you are called.
- **`DynamicData.TryGet<T>` unboxes unconditionally**, so a missing key with a struct `T` throws a null ref
  exception rather than returning false. Read struct typed keys through the untyped `TryGet(name, out object? value)` and pattern match.
- **Some variants cannot be made compatible at all, and no state key will save them.** A rollback replays
  the simulation, it does not rebuild the level. So anything decided _outside_ that window is beyond reach,
  in particular a variant that rewrites `MatchSettings.Variants` mid-match. The entity either exists in the level or it does not, and a replayed frame can neither create nor remove it.

### Testing

You can test with TF.EX installed, by launching it in **test mode**, which is a special mode that triggers a rollback every `check_distance` frame and checks if the state on each frame is the same. (equality by checksum)

For example, with a `check_distance` at 2, the game will rollback every 2 frames.

If there is a checksum mismatch, there will be an exception thrown that will show why the mismatch happened. (`DeepEqual.dll` is needed for that!)

You can launch EX test mode by launching Towerfall, open the console by pressing ² and paste a command like

`test LMS 1 2 3 4 JESTER'S_HAT`

with

- test : the mode we are lauching
- LMS : Last Man Standing
- 1 : the level where we should start
- 2 : the map where we should play
- 3 : the seed (for RNG) we want to apply
- 4 : the check distance (can be from 2 to 7)
- JESTER'S_HAT: the variant's **Title**, exactly as registered, with spaces replaced by underscores

Run one test per variant that owns state

A sync test runs in a single process, so it catches "the same code produced two different states" but
never "this value is random on each machine and never shared". For that class of bug, two real clients
are the only test.
