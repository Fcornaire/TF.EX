using MessagePack;
using System.ComponentModel;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Models.WebSocket;

namespace TF.EX.Domain.Models
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
        public PlayerDraw PlayerDraw { get; set; }

        [DefaultValue(ReplayVersion.Unknown)]
        [Key(2)]
        public ReplayVersion Version { get; set; }

        [Key(3)]
        public string Name { get; set; }

        [Key(4)]
        public Modes Mode { get; set; }

        [Key(5)]
        public IEnumerable<ArcherInfo> Archers { get; set; } = Enumerable.Empty<ArcherInfo>();

        [Key(6)]
        public TimeSpan MatchLenght { get; set; } = TimeSpan.Zero;

        [Key(7)]
        public int VersusMatchLength { get; set; } = 2;

        [Key(8)]
        public ICollection<string> Variants { get; set; } = new List<string>();

        [Key(9)]
        public ICollection<CustomMod> Mods { get; set; } = new List<CustomMod>();

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
        public List<Input> Inputs { get; set; }

        [Key(1)]
        public GameState GameState { get; set; }
    }

    public enum ReplayVersion
    {
        Unknown,
        V1,
        V2, //changed input struct to use int instead of bool
        V3, //added variants
        V4, //Switched serialization to MessagePack
        V5, //Added StuckTo actualdepth to Arrow instead of saving the stuck entity
        V6, //Added CustomMods to ReplayInfo
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
