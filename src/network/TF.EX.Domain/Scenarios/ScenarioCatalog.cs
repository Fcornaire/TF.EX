using MonoMod.Utils;
using TowerFall;

namespace TF.EX.Domain.Scenarios
{
    public static class ScenarioCatalog
    {
        private const int SpawnY = ScenarioMapWriter.SpawnY;

        public static readonly string[] TrackedTypes =
        [
            "Arrow", "Bat", "Brambles", "Chain", "Chest", "CrackedPlatform", "CrackedWall",
            "CrumbleBlock", "DarkPortalsSequence", "Explosion", "GhostPlatform", "Hat", "Icicle",
            "JumpPad", "Lantern", "Lava", "LavaControl", "Layer", "LoopPlatform", "MoonGlassBlock",
            "MovingPlatform", "Orb", "Pickup", "Player", "PlayerCorpse", "PlayerGhost", "Prism",
            "ProximityBlock", "RotatePlatform", "RoundLogic", "SensorBlock", "Session", "ShiftBlock",
            "Slime", "SnowClump", "Spikeball", "SwitchBlock", "SwitchBlockControl", "TeamReviver",
        ];

        private static readonly string[] Always = ["Layer", "RoundLogic", "Session"];

        private const int AIM_FRAMES = 10;
        private const int READY_FRAMES = 180;

        private static ScriptedAct[] AimAndFire(int aimX, int aimY) =>
        [
            new ScriptedAct(AIM_FRAMES, AimX: aimX, AimY: aimY, Shoot: true),
            new ScriptedAct(1, AimX: aimX, AimY: aimY),
        ];

        private static ScriptedAct[] Wait(int frames) => [new ScriptedAct(frames)];

        private static ScriptedAct[] AltFire() => [new ScriptedAct(1, AltShoot: true)];

        private static ScriptedAct[] Ready() => [new ScriptedAct(READY_FRAMES)];

        private static ScriptedAct[] ChestUp() => [new ScriptedAct(180)];

        private static ScriptedAct[] Walk(int dir, int frames) => [new ScriptedAct(frames, MoveX: dir)];

        private static ScriptedAct[] Dodge(int dir = 0) => [new ScriptedAct(6, MoveX: dir, AimX: dir, Dodge: true)];

        private static ScriptedAct[] Jump(int frames) => [new ScriptedAct(frames, Jump: true)];

        private static ScriptedAct[] Flap(int times) =>
        [
            .. Enumerable.Range(0, times).SelectMany(_ => new ScriptedAct[]
            {
                new ScriptedAct(8, Jump: true),
                new ScriptedAct(4),
            }),
        ];

        private static ScriptedAct[] Drift(int aimX, int aimY, int frames) => [new ScriptedAct(frames, AimX: aimX, AimY: aimY)];

        private static readonly ScriptedAct[] HoldShoot = [new ScriptedAct(600, AimX: 1, Shoot: true)];

        private static ScriptedAct[] Sequence(params ScriptedAct[][] parts) => [.. parts.SelectMany(p => p)];

        private static readonly ScriptedAct[] Still = [new ScriptedAct(600)];

        public static readonly Scenario[] All =
        [
            new Scenario
            {
                Name = "icicle",
                Covers = ["Icicle", "Player", "PlayerCorpse"],
                Frames = 420,
                Spawns = [(60, 40), (260, SpawnY)],
                Entities =
                [
                    new ScenarioEntity("Icicle", 60, 110),
                    new ScenarioEntity("Icicle", 260, SpawnY),
                    new ScenarioEntity("Icicle", 160, 60),
                ],
                Expect = level => AllOf<Icicle>(level).Any(i => DynamicData.For(i).Get<bool>("falling")),
            },
            new Scenario
            {
                Name = "corpse-prism",
                Covers = ["PlayerCorpse", "Prism", "Arrow", "Player"],
                Variants = ["START WITH PRISM ARROWS"],
                Frames = 1200,
                Spawns = [(60, SpawnY), (140, SpawnY)],
                Scripts =
                [
                    Sequence(AimAndFire(1, 0), Wait(200), AimAndFire(1, 0), Wait(400)),
                    Sequence(Wait(18), Dodge(-1), Wait(600)),
                ],
                Expect = level => AllOf<Prism>(level).Any(p => p.EncasedPlayer != null)
                    || AllOf<PlayerCorpse>(level).Any(c => c.PrismHit),
            },
            new Scenario
            {
                Name = "corpse-bramble",
                Covers = ["PlayerCorpse", "Brambles", "Arrow"],
                Variants = ["START WITH BRAMBLE ARROWS", "INFINITE BRAMBLES"],
                Frames = 1200,
                Spawns = [(60, SpawnY), (225, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, 0), Wait(289)), Still],
                Expect = level => AllOf<Brambles>(level).Any() && AllOf<PlayerCorpse>(level).Any(),
            },
            new Scenario
            {
                Name = "bramble-direct",
                Covers = ["PlayerCorpse", "Brambles", "Arrow"],
                Variants = ["START WITH BRAMBLE ARROWS", "INFINITE BRAMBLES"],
                Frames = 1200,
                Spawns = [(60, SpawnY), (140, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, 0), Wait(289)), Still],
                Expect = level => AllOf<PlayerCorpse>(level).Any(),
            },
            new Scenario
            {
                Name = "arrows-basic",
                Covers = ["Arrow", "Player", "Hat"],
                Variants = ["MAX ARROWS"],
                Frames = 1200,
                Spawns = [(60, SpawnY), (140, SpawnY)],
                Scripts =
                [
                    Sequence(Ready(),
                        AimAndFire(1, 0), Wait(140),
                        AimAndFire(1, 0), Wait(140),
                        AimAndFire(1, 0), Wait(140),
                        AimAndFire(1, -1), Wait(140),
                        AimAndFire(1, 0), Wait(200)),
                    Sequence(Wait(195), Dodge(-1), Wait(145), Dodge(-1), Wait(130),
                        AimAndFire(-1, 0), Wait(502)),
                ],
                Expect = level => AllOf<PlayerCorpse>(level).Any() && AllOf<Arrow>(level).Any(),
            },
            new Scenario
            {
                Name = "shiftblock",
                Covers = ["ShiftBlock", "Player", "PlayerCorpse"],
                Frames = 1200,
                Spawns = [(60, SpawnY), (260, SpawnY)],
                Scripts =
                [
                    Sequence(Wait(520), Walk(-1, 70), Wait(610)),
                    Sequence(Wait(520), Walk(-1, 70), Wait(610)),
                ],
                Entities =
                [
                    new ScenarioEntity("ShiftBlock", 150, 150, "width=\"40\" height=\"20\"")
                    {
                        Nodes = [(150, 190)],
                    },
                    new ScenarioEntity("ShiftBlock", 270, 150, "width=\"40\" height=\"20\"")
                    {
                        Nodes = [(270, 190)],
                    },
                ],
                Expect = level => AllOf<PlayerCorpse>(level).Any(),
            },
            new Scenario
            {
                Name = "jumppad-snowclump",
                Covers = ["JumpPad", "SnowClump"],
                Frames = 900,
                Spawns = [(150, 190), (60, SpawnY)],
                Scripts =
                [
                    Sequence(Wait(60), Walk(1, 12), Wait(20),
                        Jump(30), Wait(50), Jump(30), Wait(50), Jump(30), Wait(50),
                        Jump(30), Wait(50), Jump(30), Wait(400)),
                    Still,
                ],
                Entities =
                [
                    new ScenarioEntity("JumpPad", 140, SpawnY, "width=\"40\""),
                    new ScenarioEntity("SnowClump", 150, SpawnY),
                    new ScenarioEntity("SnowClump", 160, SpawnY),
                ],
                Expect = level => AllOf<JumpPad>(level).Any(p => DynamicData.For(p).Get<bool>("on")),
            },
            new Scenario
            {
                Name = "chain-lantern",
                Covers = ["Chain", "Lantern"],
                Frames = 900,
                Spawns = [(60, SpawnY), (280, SpawnY)],
                Scripts = [Sequence(Walk(1, 65), Wait(400)), Still],
                Entities =
                [
                    new ScenarioEntity("Chain", 160, 20, "height=\"170\""),
                    new ScenarioEntity("Lantern", 160, 190, "Dead=\"False\""),
                ],
                Expect = level => AllOf<Lantern>(level)
                    .Any(l => DynamicData.For(l).Get<bool>("falling") || DynamicData.For(l).Get<bool>("dead")),
            },
            new Scenario
            {
                Name = "lantern-shot",
                Covers = ["Chain", "Lantern"],
                Frames = 900,
                Platforms = [(90, 120, 60), (190, 120, 60)],
                Spawns = [(120, 110), (220, 110)],
                Scripts =
                [
                    Sequence(Wait(60), AimAndFire(1, 0), Wait(90), AimAndFire(-1, 0), Wait(300)),
                    Sequence(Wait(60), AimAndFire(1, 0), Wait(90), AimAndFire(-1, 0), Wait(300)),
                ],
                Entities =
                [
                    new ScenarioEntity("Chain", 170, 20, "height=\"90\""),
                    new ScenarioEntity("Lantern", 170, 110, "Dead=\"False\""),
                ],
                Expect = level => AllOf<Lantern>(level)
                    .Any(l => DynamicData.For(l).Get<bool>("falling") || DynamicData.For(l).Get<bool>("dead")),
            },
            new Scenario
            {
                Name = "moonglass",
                Covers = ["MoonGlassBlock", "Player", "Arrow"],
                Frames = 900,
                Spawns = [(160, 140), (160, SpawnY)],
                Scripts = [Still, Sequence(AimAndFire(0, -1), Wait(139))],
                Entities = [new ScenarioEntity("MoonGlassBlock", 140, 150, "width=\"60\" height=\"20\"")],
                Expect = level => AllOf<Player>(level).Any(p => p.Y <= 152f) && AllOf<Arrow>(level).Any(),
            },
            new Scenario
            {
                Name = "ghosts",
                Covers = ["PlayerGhost", "Session", "Player", "PlayerCorpse"],
                Variants = ["RETURN AS GHOSTS", "MAX ARROWS"],
                Frames = 1800,
                PlayerCount = 4,
                Platforms = [(0, 150, 40)],
                Spawns = [(60, SpawnY), (140, SpawnY), (240, SpawnY), (10, 140)],
                Scripts =
                [
                    Sequence(AimAndFire(1, 0), Wait(690),
                        AimAndFire(1, 0), Wait(60),
                        AimAndFire(1, 0), Wait(60),
                        AimAndFire(1, 0), Wait(60),
                        AimAndFire(1, 0), Wait(600)),

                    Sequence(Wait(600),
                        Drift(0, 1, 20),
                        Drift(1, 0, 25), Drift(1, 1, 10), Wait(60),
                        Drift(-1, 0, 30), Wait(900)),

                    Still,
                    Still,
                ],
                Expect = level => AllOf<PlayerGhost>(level).Any()
                    && AllOf<PlayerCorpse>(level).Count() >= 2,
            },
            new Scenario
            {
                Name = "chest",
                Covers = ["Chest", "Pickup", "Arrow", "Player"],
                Frames = 1200,
                Spawns = [(160, SpawnY), (300, SpawnY)],
                Scripts =
                [
                    Sequence(Ready(), ChestUp(),
                        AimAndFire(-1, 0), Wait(40), AimAndFire(1, 0), Wait(40),
                        Walk(-1, 60), Wait(30), Walk(1, 120), Wait(200)),
                    Still,
                ],
                Entities =
                [
                    new ScenarioEntity("TreasureChest", 100, SpawnY),
                    new ScenarioEntity("TreasureChest", 220, SpawnY),
                ],
                Expect = level => AllOf<Pickup>(level).Any(),
            },
            new Scenario
            {
                Name = "chest-big",
                Covers = ["Chest", "Pickup", "Player"],
                Variants = ["ALWAYS BIG TREASURE"],
                Frames = 1200,
                Spawns = [(60, SpawnY), (300, SpawnY)],
                Scripts = [Sequence(Ready(), ChestUp(), Walk(1, 90), Wait(300)), Still],
                Entities = [new ScenarioEntity("BigTreasureChest", 160, SpawnY)],
                Expect = level => AllOf<Pickup>(level).Count() >= 2,
            },
            new Scenario
            {
                Name = "cracked-explode",
                Covers = ["CrackedWall", "CrumbleBlock", "Explosion", "Arrow"],
                Variants = ["START WITH BOMB ARROWS", "MAX ARROWS"],
                Frames = 900,
                Spawns = [(160, SpawnY), (300, SpawnY)],
                Scripts =
                [
                    Sequence(AimAndFire(1, 0), Wait(30), AimAndFire(-1, 0), Wait(30)),
                    Still,
                ],
                Entities =
                [
                    new ScenarioEntity("CrackedWall", 75, 190),
                    new ScenarioEntity("CrumbleBlock", 225, 170, "width=\"20\" height=\"40\""),
                ],
                Expect = level => AllOf<CrackedWall>(level).Any(w => w.Active)
                    || AllOf<CrumbleBlock>(level).Any(b => b.Active),
            },
            new Scenario
            {
                Name = "cracked-platform",
                Covers = ["CrackedPlatform"],
                Frames = 600,
                Spawns = [(150, 150), (60, SpawnY)],
                Entities = [new ScenarioEntity("CrackedPlatform", 140, 160, "width=\"40\"")],
                Expect = level => AllOf<CrackedPlatform>(level).Any(p => !p.Collidable),
            },
            new Scenario
            {
                Name = "ghost-platform",
                Covers = ["GhostPlatform"],
                Frames = 400,
                Spawns = [(170, 150), (60, SpawnY)],
                Scripts =
                [
                    Sequence(Walk(1, 8), Wait(25), Walk(-1, 8), Wait(25)),
                    Still,
                ],
                Entities = [new ScenarioEntity("GhostPlatform", 140, 160, "width=\"60\"")],
                Expect = level => AllOf<GhostPlatform>(level)
                    .Any(p => DynamicData.For(p).Get<float>("sinkAmount") > 0f),
            },
            new Scenario
            {
                Name = "loop-platform",
                Covers = ["LoopPlatform"],
                Frames = 600,
                Spawns = [(160, SpawnY), (60, SpawnY)],
                Scripts =
                [
                    Sequence(Jump(25), Wait(600)),
                    Still,
                ],
                Entities = [new ScenarioEntity("LoopPlatform", 140, 180, "width=\"60\" Direction=\"Left\"")],
                Expect = level => AllOf<LoopPlatform>(level)
                    .Any(p => DynamicData.For(p).Get<float>("sinkAmount") > 0f),
            },
            new Scenario
            {
                Name = "moving-platform",
                Covers = ["MovingPlatform"],
                Frames = 900,
                Spawns = [(110, 140), (60, SpawnY)],
                Entities =
                [
                    new ScenarioEntity("MovingPlatform", 100, 150, "width=\"40\" height=\"20\"")
                    {
                        Nodes = [(220, 150)],
                    },
                ],
                Expect = level => AllOf<MovingPlatform>(level).Any(p => p.X != 100f),
            },
            new Scenario
            {
                Name = "rotate-platform",
                Covers = ["RotatePlatform"],
                Frames = 900,
                Spawns = [(150, 95), (60, SpawnY)],
                Entities =
                [
                    new ScenarioEntity("RotatePlatformsCenter", 160, 150,"Amount=\"3\" Radius=\"45\" Width=\"40\" DegSpeed=\"0.6\""),
                ],
                Expect = level => AllOf<RotatePlatform>(level).Count() == 3 && AllOf<RotatePlatform>(level).Any(p => DynamicData.For(p).Get<float>("sinkAmount") > 0f),
            },
            new Scenario
            {
                Name = "switchblock",
                Covers = ["SwitchBlock", "SwitchBlockControl"],
                Frames = 800,
                Spawns = [(210, 140), (60, SpawnY)],
                Entities =
                [
                    new ScenarioEntity("BlueSwitchBlock", 140, 150, "width=\"40\" height=\"20\""),
                    new ScenarioEntity("RedSwitchBlock", 200, 150, "width=\"40\" height=\"20\""),
                ],
                Expect = level => AllOf<SwitchBlock>(level).Any(b => b.Collidable && b.X == 140f),
            },
            new Scenario
            {
                Name = "proximity-block",
                Covers = ["ProximityBlock"],
                Frames = 900,
                Spawns = [(60, SpawnY), (300, SpawnY)],
                Scripts =
                [
                    Sequence(Ready(), Walk(1, 70), Wait(120), Walk(-1, 90), Wait(400)),
                    Still,
                ],
                Entities = [new ScenarioEntity("ProximityBlock", 160, SpawnY)],
                Expect = level => AllOf<ProximityBlock>(level).Any(b => !b.Collidable),
            },
            new Scenario
            {
                Name = "sensor-block",
                Covers = ["SensorBlock", "PlayerCorpse"],
                Frames = 900,
                Spawns = [(60, SpawnY), (300, SpawnY)],
                Scripts = [Sequence(Ready(), Walk(1, 90), Wait(400)), Still],
                Entities = [new ScenarioEntity("SensorBlock", 140, 60, "width=\"60\"")],
                Expect = level => AllOf<SensorBlock>(level).Any(b => b.Y > 60f),
            },
            new Scenario
            {
                Name = "spikeball",
                Covers = ["Spikeball", "Explosion", "PlayerCorpse"],
                Frames = 600,
                Spawns = [(160, SpawnY), (60, SpawnY)],
                Entities =
                [
                    new ScenarioEntity("SpikeBall", 160, 65, "Explodes=\"True\"")
                    {
                        Nodes = [(160, 135)],
                    },
                ],
                Expect = level => AllOf<PlayerCorpse>(level).Any(),
            },
            new Scenario
            {
                Name = "orb",
                Covers = ["Orb", "Explosion", "Arrow"],
                Frames = 900,
                Platforms = [(40, 120, 60), (250, 120, 60)],
                Spawns = [(70, 110), (280, 110)],
                Scripts =
                [
                    Sequence(Ready(), AimAndFire(1, 0), Wait(400)),
                    Sequence(Ready(), AimAndFire(-1, 0), Wait(400)),
                ],
                Entities =
                [
                    new ScenarioEntity("Orb", 150, 110),
                    new ScenarioEntity("ExplodingOrb", 210, 110),
                ],
                Expect = level => AllOf<Orb>(level).Any(o => DynamicData.For(o).Get<bool>("falling")),
            },
            new Scenario
            {
                Name = "lava",
                Covers = ["Lava", "LavaControl"],
                Variants = ["ALWAYS LAVA"],
                Frames = 1200,
                Expect = level => AllOf<Lava>(level).Any(),
            },
            new Scenario
            {
                Name = "dark-portals",
                Covers = ["DarkPortalsSequence", "Bat", "Slime"],
                Variants = ["DARK PORTALS"],
                Seed = 3,
                Frames = 2400,
                Entities =
                [
                    new ScenarioEntity("Spawner", 100, 150, "name=\"---\""),
                    new ScenarioEntity("Spawner", 220, 150, "name=\"---\""),
                ],
                Expect = level => AllOf<QuestSpawnPortal>(level).Any()
                    && AllOf<Bat>(level).Any()
                    && AllOf<Slime>(level).Any(),
            },
            new Scenario
            {
                Name = "team-reviver",
                Covers = ["TeamReviver", "PlayerCorpse", "Arrow", "Player"],
                Mode = Modes.TeamDeathmatch,
                Variants = ["TEAM REVIVE"],
                PlayerCount = 4,
                Frames = 1200,
                Spawns = [(140, SpawnY), (60, SpawnY), (188, SpawnY), (300, SpawnY)],
                Scripts =
                [
                    Still,
                    Sequence(AimAndFire(1, 0), Wait(600)),
                    Still,
                    Still,
                ],
                Expect = level => AllOf<TeamReviver>(level).Any(r => DynamicData.For(r).Get<bool>("reviving")),
            },
            new Scenario
            {
                Name = "dodge-catch",
                Covers = ["Player", "Arrow", "PlayerCorpse"],
                Variants = ["CURSED DODGES"],
                PlayerCount = 3,
                Frames = 1200,
                Spawns = [(60, SpawnY), (140, SpawnY), (300, SpawnY)],
                Scripts =
                [
                    Sequence(AimAndFire(1, 0), Wait(200), Dodge(1), Wait(600)),
                    Sequence(Wait(24), Dodge(-1), Wait(600)),
                    Still,
                ],
                Expect = level => AllOf<Player>(level).Any(p => p.PlayerIndex == 1)
                    && !AllOf<Player>(level).Any(p => p.PlayerIndex == 0)
                    && AllOf<PlayerCorpse>(level).Any(),
            },
            new Scenario
            {
                Name = "drill-arrow",
                Covers = ["Arrow", "CrumbleBlock", "Player", "PlayerCorpse"],
                Variants = ["START WITH DRILL ARROWS", "MAX ARROWS"],
                Frames = 900,
                Spawns = [(160, SpawnY), (60, SpawnY)],
                Scripts = [Sequence(AimAndFire(-1, 0), Wait(600)), Still],
                Entities =
                [
                    new ScenarioEntity("CrumbleBlock", 80, 170, "width=\"20\" height=\"40\""),
                ],
                Expect = level => AllOf<PlayerCorpse>(level).Any(),
            },
            new Scenario
            {
                Name = "arrow-squish",
                Covers = ["Arrow", "MovingPlatform", "Player"],
                Frames = 900,
                Spawns = [(60, SpawnY), (300, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, 0), Wait(600)), Still],
                Entities =
                [
                    new ScenarioEntity("MovingPlatform", 190, 150, "width=\"40\" height=\"20\"")
                    {
                        Nodes = [(190, 200)],
                    },
                ],
                Expect = level => AllOf<Arrow>(level).Any(a => DynamicData.For(a).Get<bool>("squished")),
            },
            new Scenario
            {
                Name = "team-arrow-owner",
                Covers = ["Arrow", "PlayerCorpse", "Player"],
                Mode = Modes.TeamDeathmatch,
                Variants = ["MAX ARROWS"],
                PlayerCount = 4,
                Frames = 1200,
                Spawns = [(80, SpawnY), (160, SpawnY), (40, SpawnY), (280, SpawnY)],
                Scripts =
                [
                    Sequence(AimAndFire(1, 0), Wait(600)),
                    Sequence(AimAndFire(1, 0), Wait(600)),
                    Still,
                    Sequence(Wait(34), Dodge(-1), Wait(600)),
                ],
                Expect = level => AllOf<PlayerCorpse>(level).Any()
                    && AllOf<Player>(level).Any(p => DynamicData.For(p).Get<Arrow>("lastCaught") != null),
            },
            new Scenario
            {
                Name = "bolt-arrow",
                Covers = ["Arrow", "Player", "PlayerCorpse"],
                Variants = ["START WITH BOLT ARROWS", "MAX ARROWS"],
                Frames = 900,
                Platforms = [(40, 160, 40)],
                Spawns = [(60, 150), (110, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, 0), Wait(600)), Still],
                Expect = level => AllOf<BoltArrow>(level).Any(a => a.Turns > 0),
            },
            new Scenario
            {
                Name = "laser-arrow",
                Covers = ["Arrow", "Player", "PlayerCorpse"],
                Variants = ["START WITH LASER ARROWS", "MAX ARROWS"],
                Frames = 900,
                Platforms = [(70, 170, 40)],
                Spawns = [(60, SpawnY), (125, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, -1), Wait(600)), Still],
                Expect = level => AllOf<LaserArrow>(level).Any(a => a.Bounced >= 2) && AllOf<PlayerCorpse>(level).Any(),
            },
            new Scenario
            {
                Name = "superbomb-arrow",
                Covers = ["Arrow", "Explosion", "Player", "PlayerCorpse"],
                Variants = ["START WITH SUPER BOMB ARROWS", "MAX ARROWS"],
                Frames = 900,
                Spawns = [(60, SpawnY), (240, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, 0), Wait(600)), Still],
                Expect = level => AllOf<Explosion>(level).Any() && AllOf<PlayerCorpse>(level).Any(),
            },
            new Scenario
            {
                Name = "feather-arrow",
                Covers = ["Arrow", "Player", "PlayerCorpse"],
                Variants = ["START WITH FEATHER ARROWS", "MAX ARROWS"],
                Frames = 900,
                Spawns = [(60, SpawnY), (216, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, 0), Wait(600)), Still],
                Expect = level => AllOf<FeatherArrow>(level).Any() && AllOf<PlayerCorpse>(level).Any(),
            },
            new Scenario
            {
                Name = "toy-arrow",
                Covers = ["Arrow", "Player"],
                Variants = ["START WITH TOY ARROWS", "MAX ARROWS"],
                Frames = 900,
                Spawns = [(60, SpawnY), (140, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, 0), Wait(600)), Still],
                Expect = level => AllOf<ToyArrow>(level).Any(),
            },
            new Scenario
            {
                Name = "trigger-arrow",
                Covers = ["Arrow", "Explosion", "Player", "PlayerCorpse"],
                Variants = ["START WITH TRIGGER ARROWS", "MAX ARROWS"],
                Frames = 900,
                Spawns = [(60, SpawnY), (212, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, 0), Wait(90), AltFire(), Wait(600)), Still],
                Expect = level => AllOf<Explosion>(level).Any() && AllOf<PlayerCorpse>(level).Any(),
            },
            new Scenario
            {
                Name = "always-dark",
                Covers = ["OrbLogic", "Layer"],
                Variants = ["ALWAYS DARK"],
                Frames = 900,
                Expect = level => DynamicData.For(level.OrbLogic).Get<bool>("darkened"),
            },
            new Scenario
            {
                Name = "slow-time",
                Covers = ["OrbLogic"],
                Variants = ["SLOW TIME"],
                Frames = 2400,
                PlayerCount = 3,
                Spawns = [(60, SpawnY), (140, SpawnY), (280, SpawnY)],
                Scripts =
                [
                    Sequence(Walk(1, 30), Jump(24), Walk(-1, 30), Dodge(1), Wait(20), AimAndFire(1, 0), Wait(160)),
                    Still,
                    Sequence(Walk(-1, 25), Jump(24), Walk(1, 25), Dodge(-1), Wait(60)),
                ],
                Expect = level => DynamicData.For(level.OrbLogic).Get<float>("gameRateTarget") == 0.5f && AllOf<PlayerCorpse>(level).Any(),
            },
            new Scenario
            {
                Name = "always-scrolling",
                Covers = ["OrbLogic"],
                Variants = ["ALWAYS SCROLLING"],
                Frames = 1200,
                Expect = level =>
                    DynamicData.For(level.OrbLogic).Get<Microsoft.Xna.Framework.Vector2>("spaceSpeed") != Microsoft.Xna.Framework.Vector2.Zero,
            },
            new Scenario
            {
                Name = "offset-world",
                Covers = ["OrbLogic"],
                Variants = ["OFFSET WORLD"],
                Frames = 900,
                Spawns = [(60, SpawnY), (240, SpawnY)],
                Scripts =
                [
                    Sequence(Walk(1, 30), Jump(24), Walk(-1, 30), Wait(30)),
                    Sequence(Walk(-1, 30), Jump(24), Walk(1, 30), Wait(30)),
                ],
                Expect = level => DynamicData.For(level.OrbLogic).Get<Monocle.Tween>("spaceTween") != null,
            },
            new Scenario
            {
                Name = "sudden-death",
                Covers = ["Miasma", "Player", "PlayerCorpse", "RoundLogic"],
                Variants = ["SUDDEN DEATH"],
                Frames = 1800,
                PlayerCount = 4,
                Spawns = [(130, SpawnY), (190, SpawnY), (150, SpawnY), (170, SpawnY)],
                Scripts =
                [
                    Sequence(Walk(-1, 20), Jump(24), Walk(1, 20), Wait(30)),
                    Sequence(Walk(1, 20), Jump(24), Walk(-1, 20), Wait(30)),
                    Sequence(Walk(1, 20), Wait(20), Walk(-1, 20), Jump(24), Wait(30)),
                    Sequence(Walk(-1, 20), Wait(20), Walk(1, 20), Jump(24), Wait(30)),
                ],
                Expect = level => AllOf<Miasma>(level).Any() && AllOf<PlayerCorpse>(level).Any(),
            },
            new Scenario
            {
                Name = "exploding-corpses",
                Covers = ["PlayerCorpse", "Explosion", "Player", "Arrow"],
                Variants = ["EXPLODING CORPSES"],
                PlayerCount = 3,
                Frames = 900,
                Spawns = [(60, SpawnY), (140, SpawnY), (200, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, 0), Wait(600)), Still, Still],
                Expect = level => AllOf<PlayerCorpse>(level).Any(c => c.PlayerIndex == 2),
            },
            new Scenario
            {
                Name = "trigger-corpses",
                Covers = ["PlayerCorpse", "Explosion", "Player", "Arrow"],
                Variants = ["TRIGGER CORPSES"],
                PlayerCount = 3,
                Frames = 900,
                Spawns = [(60, SpawnY), (140, SpawnY), (200, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, 0), Wait(600)), Sequence(Wait(150), HoldShoot), Still],
                Expect = level => AllOf<PlayerCorpse>(level).Any(c => c.PlayerIndex == 2),
            },
            new Scenario
            {
                Name = "corpses-drop-arrows",
                Covers = ["PlayerCorpse", "Arrow", "Player"],
                Variants = ["CORPSES DROP ARROWS", "MAX ARROWS"],
                PlayerCount = 3,
                Frames = 900,
                Spawns = [(60, SpawnY), (140, SpawnY), (300, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, 0), Wait(600)), Still, Still],
                Expect = level => AllOf<PlayerCorpse>(level).Any() && AllOf<Arrow>(level).Count() >= 4,
            },
            new Scenario
            {
                Name = "start-with-shields",
                Covers = ["Player", "Arrow"],
                Variants = ["START WITH SHIELDS", "MAX ARROWS"],
                Frames = 900,
                Spawns = [(60, SpawnY), (140, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, 0), Wait(600)), Still],
                Expect = level => AllOf<Player>(level).Any(p => p.PlayerIndex == 1 && !p.HasShield),
            },
            new Scenario
            {
                Name = "start-with-wings",
                Covers = ["Player"],
                Variants = ["START WITH WINGS"],
                Frames = 900,
                Spawns = [(60, SpawnY), (260, SpawnY)],
                Scripts = [Sequence(Flap(12), Jump(90), Wait(90)), Still],
                Expect = level => AllOf<Player>(level).Any(p => p.PlayerIndex == 0 && p.Y < 150f),
            },
            new Scenario
            {
                Name = "start-with-speed-boots",
                Covers = ["Player"],
                Variants = ["START WITH SPEED BOOTS"],
                Frames = 900,
                Spawns = [(60, SpawnY), (280, SpawnY)],
                Scripts = [Sequence(Walk(1, 60), Wait(2000)), Still],
                Expect = level => AllOf<Player>(level).Any(p => p.PlayerIndex == 0 && p.X > 163f),
            },
            new Scenario
            {
                Name = "start-invisible",
                Covers = ["Player"],
                Variants = ["START INVISIBLE", "MAX ARROWS"],
                Frames = 900,
                Spawns = [(60, SpawnY), (260, SpawnY)],
                Scripts = [Sequence(Wait(120), AimAndFire(1, 0), Wait(200)), Still],
                Expect = level => AllOf<Player>(level).Any(p => p.Invisible && p.InvisOpacity < 1f),
            },
            new Scenario
            {
                Name = "double-jumping",
                Covers = ["Player"],
                Variants = ["DOUBLE JUMPING"],
                Frames = 900,
                Spawns = [(60, SpawnY), (260, SpawnY)],
                Scripts = [Sequence(Jump(22), Wait(4), Jump(22), Wait(60)), Still],
                Expect = level => AllOf<Player>(level).Any(p => p.PlayerIndex == 0 && p.Y < 150f),
            },
            new Scenario
            {
                Name = "slippery-floors",
                Covers = ["Player"],
                Variants = ["SLIPPERY FLOORS"],
                Frames = 900,
                Spawns = [(60, SpawnY), (280, SpawnY)],
                Scripts = [Sequence(Walk(1, 60), Wait(2000)), Still],
                Expect = level => AllOf<Player>(level).Any(p => p.PlayerIndex == 0 && p.X > 180f),
            },
            new Scenario
            {
                Name = "clumsy-archers",
                Covers = ["Player", "Arrow"],
                Variants = ["CLUMSY ARCHERS", "MAX ARROWS"],
                Frames = 900,
                Spawns = [(60, SpawnY), (260, SpawnY)],
                Scripts = [Sequence(Wait(30), Dodge(1), Wait(40), Dodge(-1), Wait(40)), Still],
                Expect = level => AllOf<Arrow>(level).Any(),
            },
            new Scenario
            {
                Name = "anti-gravity-arrows",
                Covers = ["Arrow", "Player", "PlayerCorpse"],
                Variants = ["ANTI GRAVITY ARROWS", "MAX ARROWS"],
                Frames = 900,
                Spawns = [(60, SpawnY), (260, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, 0), Wait(600)), Still],
                Expect = level => AllOf<PlayerCorpse>(level).Any(c => c.PlayerIndex == 1),
            },
            new Scenario
            {
                Name = "infinite-lasers",
                Covers = ["Arrow", "Player"],
                Variants = ["START WITH LASER ARROWS", "INFINITE LASERS", "ANTI GRAVITY ARROWS", "MAX ARROWS"],
                Frames = 900,
                Platforms = [(70, 180, 180)],
                Spawns = [(60, SpawnY), (280, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, -1), Wait(600)), Still],
                Expect = level => AllOf<LaserArrow>(level).Any(a => a.Bounced >= 9),
            },
            new Scenario
            {
                Name = "infinite-drills",
                Covers = ["Arrow", "CrumbleBlock", "Player", "PlayerCorpse"],
                Variants = ["START WITH DRILL ARROWS", "INFINITE DRILLS", "MAX ARROWS"],
                Frames = 900,
                Spawns = [(200, SpawnY), (60, SpawnY)],
                Scripts = [Sequence(AimAndFire(-1, 0), Wait(600)), Still],
                Entities =
                [
                    new ScenarioEntity("CrumbleBlock", 150, 170, "width=\"20\" height=\"40\""),
                    new ScenarioEntity("CrumbleBlock", 100, 170, "width=\"20\" height=\"40\""),
                ],
                Expect = level => AllOf<PlayerCorpse>(level).Any(),
            },
            new Scenario
            {
                Name = "regenerating-arrows",
                Covers = ["Arrow", "Player"],
                Variants = ["REGENERATING ARROWS"],
                Frames = 900,
                Spawns = [(60, SpawnY), (280, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, 0), Wait(30)), Still],
                Expect = level => AllOf<Arrow>(level).Count() >= 5,
            },
            new Scenario
            {
                Name = "cursed-bows",
                Covers = ["Player", "PlayerCorpse", "Arrow"],
                Variants = ["CURSED BOWS"],
                Frames = 900,
                PlayerCount = 3,
                Spawns = [(60, SpawnY), (270, SpawnY), (300, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, 0), Wait(20)), Still, Still],
                Expect = level => AllOf<PlayerCorpse>(level).Any(c => c.PlayerIndex == 0),
            },
            new Scenario
            {
                Name = "regenerating-shields",
                Covers = ["Player"],
                Variants = ["REGENERATING SHIELDS"],
                Frames = 900,
                Expect = level => AllOf<Player>(level).Any(p => p.HasShield),
            },
            new Scenario
            {
                Name = "start-with-random-arrows",
                Covers = ["Arrow", "Player"],
                Variants = ["START WITH RANDOM ARROWS", "MAX ARROWS"],
                Seed = 42,
                Frames = 900,
                Spawns = [(60, SpawnY), (280, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, 0), Wait(600)), Still],
                Expect = level => AllOf<Arrow>(level).Any(a => a is not DefaultArrow),
            },
            new Scenario
            {
                Name = "gunn-style",
                Covers = ["Player", "PlayerCorpse", "Arrow"],
                Variants = ["GUNN STYLE"],
                Frames = 900,
                PlayerCount = 3,
                Spawns = [(60, SpawnY), (140, SpawnY), (300, SpawnY)],
                Scripts = [Sequence(AimAndFire(1, 0), Wait(600)), Still, Still],
                Expect = level => level.Frozen,
            },
            new Scenario
            {
                Name = "bomb-chests",
                Covers = ["Chest", "Pickup", "Explosion", "Player"],
                Variants = ["BOMB CHESTS"],
                Frames = 1200,
                PlayerCount = 3,
                Spawns = [(100, SpawnY), (300, SpawnY), (40, SpawnY)],
                Entities =
                [
                    new ScenarioEntity("TreasureChest", 100, SpawnY),
                    new ScenarioEntity("TreasureChest", 220, SpawnY),
                ],
                Expect = level => AllOf<PlayerCorpse>(level).Any(c => c.PlayerIndex == 0),
            },
            new Scenario
            {
                Name = "bottomless-treasure",
                Covers = ["Chest", "Pickup", "Player"],
                Variants = ["BOTTOMLESS TREASURE"],
                Frames = 1200,
                Spawns = [(60, SpawnY), (300, SpawnY)],
                Scripts = [Sequence(Ready(), ChestUp(), Walk(1, 90), Wait(400)), Still],
                Entities = [new ScenarioEntity("BigTreasureChest", 160, SpawnY)],
                Expect = level => AllOf<TreasureChest>(level).Any() && AllOf<Pickup>(level).Any(),
            },
            new Scenario
            {
                Name = "space-orb",
                Covers = ["Chest", "Pickup", "OrbLogic", "Player"],
                Variants =
                [
                    "MAX TREASURE",
                    "IGNORE TOWER ITEM SET",
                    "NO EXTRA ARROWS", "NO BOMB ARROWS", "NO LASER ARROWS", "NO BRAMBLE ARROWS",
                    "NO DRILL ARROWS", "NO BOLT ARROWS", "NO SUPER BOMB ARROWS", "NO FEATHER ARROWS",
                    "NO TRIGGER ARROWS", "NO PRISM ARROWS",
                    "NO SHIELD", "NO WINGS", "NO SPEED BOOTS", "NO LOOKING GLASS", "NO BOMB",
                    "NO DARK ORB", "NO TIME ORB", "NO LAVA ORB",
                ],
                Frames = 1200,
                Spawns = [(160, SpawnY), (300, SpawnY)],
                Scripts = [Sequence(Jump(24), Wait(20)), Still],
                Entities = [new ScenarioEntity("TreasureChest", 160, SpawnY)],
                Expect = level => DynamicData.For(level.OrbLogic).Get<Monocle.Tween>("spaceTween") != null,
            },
            new Scenario
            {
                Name = "icicle-ghost",
                Covers = ["Icicle", "PlayerGhost", "Player", "PlayerCorpse", "Arrow"],
                Variants = ["RETURN AS GHOSTS", "MAX ARROWS"],
                Frames = 1200,
                PlayerCount = 3,
                Spawns = [(60, SpawnY), (140, SpawnY), (300, SpawnY)],
                Entities =
                [
                    new ScenarioEntity("Icicle", 180, 180),
                    new ScenarioEntity("Icicle", 190, 180),
                    new ScenarioEntity("Icicle", 200, 180),
                ],
                Scripts =
                [
                    Sequence(AimAndFire(1, 0), Wait(600)),
                    Sequence(Wait(100), Drift(0, -1, 20), Drift(0, 1, 100), Wait(400)),
                    Still,
                ],
                Expect = level => AllOf<Icicle>(level).Any(i =>
                    DynamicData.For(i).Get<LevelEntity>("cannotHit") is PlayerGhost
                    && DynamicData.For(i).Get<bool>("falling")),
            },
            new Scenario
            {
                Name = "ghost-stomp",
                Covers = ["PlayerGhost", "Player", "PlayerCorpse", "Arrow"],
                Variants = ["RETURN AS GHOSTS", "MAX ARROWS"],
                Frames = 1200,
                PlayerCount = 3,
                Spawns = [(60, SpawnY), (140, SpawnY), (188, SpawnY)],
                Scripts =
                [
                    Sequence(AimAndFire(1, 0), Wait(600)),
                    Still,
                    Sequence(Wait(75), Jump(24), Wait(600)),
                ],
                Expect = level => AllOf<PlayerGhost>(level).Any(g => g.State == 3)
                    && AllOf<Player>(level).Any(p => p.PlayerIndex == 2),
            },
        ];

        public static Scenario[] Resolve(IEnumerable<string> names)
        {
            var wanted = names?.ToArray() ?? [];

            if (wanted.Length == 0)
            {
                return All;
            }

            return [.. All.Where(s => wanted.Contains(s.Name, StringComparer.OrdinalIgnoreCase))];
        }

        private static IEnumerable<T> AllOf<T>(Level level) where T : Monocle.Entity
        {
            return level.Layers.SelectMany(layer => layer.Value.Entities).OfType<T>();
        }

        public static void LogCoverage(Action<string> log)
        {
            var covered = All.SelectMany(s => s.Covers).Concat(Always)
                .Distinct().ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = TrackedTypes.Where(t => !covered.Contains(t)).ToArray();

            log($"[sweep] coverage: {TrackedTypes.Length - missing.Length}/{TrackedTypes.Length} have a scenario");

            if (missing.Length > 0)
            {
                log($"[sweep] no scenario yet: {string.Join(", ", missing)}");
            }
        }
    }
}
