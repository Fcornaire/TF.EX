using TowerFall;

namespace TF.EX.Domain.Scenarios
{
    public sealed record ScenarioEntity(string Name, int x, int y, string Attributes = "")
    {
        public (int X, int Y)[] Nodes { get; init; } = [];
    }

    public sealed record ScriptedAct(
        int Frames,
        int MoveX = 0,
        int MoveY = 0,
        bool Jump = false,
        bool Shoot = false,
        bool Dodge = false,
        int AimX = 0,
        int AimY = 0,
        bool AltShoot = false);

    public sealed class Scenario
    {
        public string Name { get; init; }

        public string[] Covers { get; init; } = [];

        public string[] Variants { get; init; } = [];

        public int PlayerCount { get; init; } = 2;

        public Modes Mode { get; init; } = Modes.LastManStanding;

        public int Frames { get; init; }

        public int? Seed { get; init; }

        public ScriptedAct[][] Scripts { get; init; } = [];

        public (int X, int Y, int Width)[] Platforms { get; init; } = [];

        public (int X, int Y)[] Spawns { get; init; } = [];

        public ScenarioEntity[] Entities { get; init; } = [];

        public Func<Level, bool> Expect { get; init; }
    }
}
