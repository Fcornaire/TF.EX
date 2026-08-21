using MessagePack;
using TowerFall;

namespace TF.EX.Domain.Models.WebSocket
{
    public enum LobbyKind
    {
        Standard,
        QuickPlay,
        Private,
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class Lobby
    {
        public string Name { get; set; } = "";
        public string RoomId { get; set; } = "";
        public int MaxPlayers { get; set; } = 4;
        public ICollection<Player> Players { get; set; } = new List<Player>();
        public ICollection<Player> Spectators { get; set; } = new List<Player>();
        public GameData GameData { get; set; } = new GameData();
        public ICollection<EndGameVote> EndGameChoice { get; set; } = new List<EndGameVote>();
        public ICollection<CustomMod> Mods { get; set; } = new List<CustomMod>();
   
        public string Kind { get; set; } = nameof(LobbyKind.Standard);
        public string JoinCode { get; set; } = "";

        [IgnoreMember]
        public LobbyKind KindValue => Enum.TryParse<LobbyKind>(Kind, out var kind) ? kind : LobbyKind.Standard;

        [IgnoreMember]
        public bool IsPrivate => KindValue == LobbyKind.Private;

        [IgnoreMember]
        public bool IsQuickPlay => KindValue == LobbyKind.QuickPlay;

        [IgnoreMember]
        public bool IsEmpty => Players.Count == 0;

        [IgnoreMember]
        public bool IsTeamMode => !IsEmpty && (TowerFall.Modes)GameData.Mode == TowerFall.Modes.TeamDeathmatch;

        [IgnoreMember]
        public bool CanJoin { get; set; } = true;

        [IgnoreMember]
        public string CanNotJoinReason { get; set; } = "";
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class EndGameVote
    {
        public string Addr { get; set; } = "";
        public string Choice { get; set; } = "";
    }

    public class EndGameStatus
    {
        public int Seat { get; set; }
        public string Name { get; set; } = "";
        public string Choice { get; set; }
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class GameData
    {
        public int MapId { get; set; } = -1;
        public int Mode { get; set; } = (int)TowerFall.Modes.LastManStanding;
        public int MatchLength { get; set; } = (int)MatchSettings.MatchLengths.Standard;
        public ICollection<string> Variants { get; set; } = new List<string>();
        public int Seed { get; set; } = 0;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class Player
    {
        public string Name { get; set; } = "";
        public string Addr { get; set; } = "";
        public int ArcherIndex { get; set; } = 0;
        public int ArcherAltIndex { get; set; } = 0;
        public bool Ready { get; set; } = false;
        public string RoomPeerId { get; set; } = "";
        public bool IsHost { get; set; }
        public int Ping { get; set; } = 0;

        public int Seat { get; set; } = 0;

        public int Team { get; set; } = (int)TowerFall.Allegiance.Neutral;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class PlayerPing
    {
        public string Addr { get; set; } = "";
        public int Ping { get; set; } = 0;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class CustomMod
    {
        public string Name { get; set; } = "";
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }
}
