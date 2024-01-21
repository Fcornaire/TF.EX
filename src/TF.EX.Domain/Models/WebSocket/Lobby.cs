using MessagePack;
using TowerFall;

namespace TF.EX.Domain.Models.WebSocket
{
    [MessagePackObject(keyAsPropertyName: true)]
    public class Lobby
    {
        public string Name { get; set; } = "";
        public string RoomId { get; set; } = "";
        public string RoomChatId { get; set; } = "";
        public ICollection<Player> Players { get; set; } = new List<Player>();
        public ICollection<Player> Spectators { get; set; } = new List<Player>();
        public GameData GameData { get; set; } = new GameData();

        public bool IsEmpty => Players.Count == 0;
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
        public string RoomChatPeerId { get; set; } = "";
        public bool IsHost { get; set; }

    }
}
