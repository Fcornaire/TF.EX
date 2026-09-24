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
        public int MaxPlayers { get; set; } = 2;
        public ICollection<Player> Players { get; set; } = new List<Player>();
        public ICollection<Player> Spectators { get; set; } = new List<Player>();
        public GameData GameData { get; set; } = new GameData();
        public ICollection<EndGameVote> EndGameChoice { get; set; } = new List<EndGameVote>();
        public ICollection<CustomMod> Mods { get; set; } = new List<CustomMod>();

        public bool InGame { get; set; } = false;

        public string Kind { get; set; } = nameof(LobbyKind.Standard);
        public string JoinCode { get; set; } = "";
        public Series Series { get; set; } = null;

        [IgnoreMember]
        public LobbyKind KindValue => Enum.TryParse<LobbyKind>(Kind, out var kind) ? kind : LobbyKind.Standard;

        [IgnoreMember]
        public bool IsSeriesLobby => GameData.BestOf > 0;

        [IgnoreMember]
        public bool IsSeriesInProgress => Series?.IsInProgress == true;

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

        public List<string> MissingVariants { get; set; } = new List<string>();
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class EndGameVote
    {
        public string RoomPeerId { get; set; } = "";
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
        public int BestOf { get; set; } = 0;
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class Series
    {
        public const string InProgress = "InProgress";
        public const string Finished = "Finished";
        public const string Aborted = "Aborted";

        public int BestOf { get; set; } = 0;
        public List<List<int>> Sides { get; set; } = [];
        public List<SeriesGame> Games { get; set; } = [];
        public string Status { get; set; } = InProgress;
        public int? WinnerSide { get; set; }
        public int? PickerSide { get; set; }
        public int? NextMapId { get; set; }
        public string AbortReason { get; set; }
        public bool AwaitingResult { get; set; }
        public List<SeriesPick> Picks { get; set; } = new List<SeriesPick>();

        [IgnoreMember]
        public bool IsInProgress => Status == InProgress;

        [IgnoreMember]
        public bool IsFinished => Status == Finished;

        [IgnoreMember]
        public bool IsAborted => Status == Aborted;

        [IgnoreMember]
        public int WinsNeeded => BestOf / 2 + 1;

        [IgnoreMember]
        public IEnumerable<int> PlayedMapIds => Games.Select(game => game.MapId);

        public int Wins(int side)
        {
            return Games.Count(game => game.WinnerSide == side);
        }

        public int? SideOfSeat(int seat)
        {
            var side = Sides.FindIndex(seats => seats.Contains(seat));

            return side >= 0 ? side : null;
        }

        public int? PickOf(int seat)
        {
            return Picks.FirstOrDefault(pick => pick.Seat == seat)?.MapId;
        }
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class SeriesPick
    {
        public int Seat { get; set; }
        public int MapId { get; set; }
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class SeriesGame
    {
        public int MapId { get; set; }
        public int WinnerSide { get; set; }
        public List<int> Scores { get; set; } = new List<int>();
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class Player
    {
        public string Name { get; set; } = "";
        public int ArcherIndex { get; set; } = 0;
        public int ArcherAltIndex { get; set; } = 0;
        public bool Ready { get; set; } = false;
        public string RoomPeerId { get; set; } = "";
        public bool IsHost { get; set; }

        public int Seat { get; set; } = 0;

        public int Team { get; set; } = (int)TowerFall.Allegiance.Neutral;

        public string CustomArcherId { get; set; } = "";

        public List<string> ArcherMods { get; set; } = new List<string>();

        public List<string> CustomVariants { get; set; } = new List<string>();

        [IgnoreMember]
        public bool HasCustomArcher => !string.IsNullOrEmpty(CustomArcherId);

        public Player WithResolvedArcher(int archerIndex, int archerAltIndex)
        {
            return new Player
            {
                Name = Name,
                ArcherIndex = archerIndex,
                ArcherAltIndex = archerAltIndex,
                Ready = Ready,
                RoomPeerId = RoomPeerId,
                IsHost = IsHost,
                Seat = Seat,
                Team = Team,
                CustomArcherId = CustomArcherId,
                ArcherMods = ArcherMods,
            };
        }
    }

    [MessagePackObject(keyAsPropertyName: true)]
    public class CustomMod
    {
        public const string VersionKey = "Version";

        public string Name { get; set; } = "";
        public Dictionary<string, string> Data { get; set; } = new Dictionary<string, string>();
    }
}
