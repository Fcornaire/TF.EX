using System.Runtime.Serialization;

namespace TF.EX.Domain.Models.WebSocket.Client
{
    [DataContract]
    public class UpdateLobbySettingsMessage
    {
        [DataMember(Name = "UpdateLobbySettings")]
        public UpdateLobbySettings UpdateLobbySettings { get; set; } = new UpdateLobbySettings();
    }

    [DataContract]
    public class UpdateLobbySettings
    {
        [DataMember(Name = "max_players")]
        public int MaxPlayers { get; set; }

        [DataMember(Name = "game_data")]
        public GameData GameData { get; set; }

        [DataMember(Name = "mods")]
        public ICollection<CustomMod> Mods { get; set; } = new List<CustomMod>();
    }
}
