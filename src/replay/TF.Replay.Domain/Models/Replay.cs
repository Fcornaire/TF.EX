using MessagePack;
using System.ComponentModel;

namespace TF.Replay.Domain.Models
{
    [MessagePackObject]
    public class Replay
    {
        [Key(0)]
        public ReplayInfo Informations { get; set; }

        [Key(1)]
        public List<Record> Record { get; set; } = new List<Record>();

        [Key(2)]
        public List<Record> Desynchs { get; set; } = new List<Record>();
    }

    [MessagePackObject]
    public class ReplayInfo
    {
        [Key(0)]
        public int Id { get; set; }

        [Key(1)]
        public int LocalSeat { get; set; }

        [DefaultValue(ReplayVersion.Unknown)]
        [Key(2)]
        public ReplayVersion Version { get; set; }

        [Key(3)]
        public string Name { get; set; }

        [Key(4)]
        public int Mode { get; set; }

        [Key(5)]
        public IEnumerable<ArcherInfo> Archers { get; set; } = Enumerable.Empty<ArcherInfo>();

        [Key(6)]
        public TimeSpan MatchLength { get; set; } = TimeSpan.Zero;

        [Key(7)]
        public int VersusMatchLength { get; set; } = 2;

        [Key(8)]
        public ICollection<string> Variants { get; set; } = new List<string>();

        [Key(9)]
        public ICollection<CustomMod> Mods { get; set; } = new List<CustomMod>();

        [Key(10)]
        public int PlayerCount { get; set; }

        [Key(11)]
        public int Seed { get; set; }

        [Key(12)]
        public int[] Teams { get; set; } = [];

        [Key(13)]
        public int TrialsLevelY { get; set; }

        [Key(14)]
        public long TrialsTimeTicks { get; set; }

        [Key(15)]
        public string StateSchema { get; set; }

        [Key(16)]
        public int CustomGoal { get; set; } //Only useful with Custom match lengths

        [IgnoreMember]
        public string StateSchemaOrLegacy => string.IsNullOrEmpty(StateSchema) ? ReplayInfo.LegacyStateSchema : StateSchema;

        public const string LegacyStateSchema = "TF.State/1";
    }

    [MessagePackObject]
    public class CustomMod
    {
        [Key(0)]
        public string Name { get; set; }

        [Key(1)]
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }

    [MessagePackObject]
    public class ArcherInfo
    {
        [Key(0)]
        public int Index { get; set; }

        [Key(1)]
        public ArcherTypes Type { get; set; }

        [Key(2)]
        public bool HasWon { get; set; }

        [Key(3)]
        public int Score { get; set; }

        [Key(4)]
        public string NetplayName { get; set; }
    }

    [MessagePackObject]
    public class Record
    {
        [Key(0)]
        public int[] Inputs { get; set; }

        [Key(1)]
        public byte[] State { get; set; }

        [Key(2)]
        public int Frame { get; set; }
    }

    // FROZEN AT V9
    public enum ReplayVersion
    {
        Unknown,
        V1,
        V2, //changed input struct to use int instead of bool
        V3, //added variants
        V4, //Switched serialization to MessagePack
        V5, //Added StuckTo actualdepth to Arrow instead of saving the stuck entity
        V6, //Added CustomMods to ReplayInfo
        V7, //Deterministic xoshiro256** RNG: Rng snapshot is now generator state words, not a draw log + chest rework
        V8, //PlayerDraw became LocalSeat, archers/inputs are seat-ordered and can be up to 3-4
        V9, //TF.Replay split: state is a byte[], inputs are a flat int[], records carry Frame
    }

    public static class ReplayVersionExtensions
    {
        public static ReplayVersion GetLatest()
        {
            return Enum.GetValues(typeof(ReplayVersion)).Cast<ReplayVersion>().Max();
        }
    }

    public enum ArcherTypes
    {
        Normal,
        Alt,
        Secret
    }
}
