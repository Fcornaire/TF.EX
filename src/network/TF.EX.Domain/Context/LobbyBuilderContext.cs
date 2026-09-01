using TF.EX.Domain.Models.WebSocket;

namespace TF.EX.Domain.Context
{
    public static class LobbyBuilderContext
    {
        public static bool IsPrivate;

        public static bool IsEditing { get; private set; }

        private static int snapshotMaxPlayers;
        private static GameData snapshotGameData;

        public static void BeginEdit(Lobby lobby)
        {
            IsEditing = true;
            snapshotMaxPlayers = lobby.MaxPlayers;
            snapshotGameData = Clone(lobby.GameData);
        }

        public static void CancelEdit(Lobby lobby)
        {
            if (!IsEditing)
            {
                return;
            }

            lobby.MaxPlayers = snapshotMaxPlayers;
            lobby.GameData = Clone(snapshotGameData);

            EndEdit();
        }

        public static void EndEdit()
        {
            IsEditing = false;
            snapshotGameData = null;
        }

        public static bool HasChanges(Lobby lobby, ICollection<string> variantTitles)
        {
            if (!IsEditing || snapshotGameData == null)
            {
                return false;
            }

            return lobby.MaxPlayers != snapshotMaxPlayers
                || lobby.GameData.MapId != snapshotGameData.MapId
                || lobby.GameData.Mode != snapshotGameData.Mode
                || lobby.GameData.MatchLength != snapshotGameData.MatchLength
                || !variantTitles.OrderBy(title => title).SequenceEqual(snapshotGameData.Variants.OrderBy(title => title));
        }

        private static GameData Clone(GameData gameData)
        {
            return new GameData
            {
                MapId = gameData.MapId,
                Mode = gameData.Mode,
                MatchLength = gameData.MatchLength,
                Variants = new List<string>(gameData.Variants),
                Seed = gameData.Seed,
            };
        }
    }
}
