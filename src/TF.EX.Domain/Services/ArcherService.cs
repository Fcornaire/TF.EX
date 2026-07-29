using TF.EX.Domain.Context;
using TF.EX.Domain.Models.WebSocket;
using TF.EX.Domain.Ports;
using ArcherData = TowerFall.ArcherData;
using TFGame = TowerFall.TFGame;

namespace TF.EX.Domain.Services
{
    internal class ArcherService : IArcherService
    {
        private readonly IGameContext _gameContext;

        public ArcherService(IGameContext gameContext)
        {
            _gameContext = gameContext;
        }

        public void AddArcher(int index, Player player)
        {
            _gameContext.AddArcher(index, player);
        }

        public IEnumerable<(int, string)> GetArchers()
        {
            return _gameContext.GetArchers();
        }

        public IEnumerable<(int, string)> GetFinalArchers()
        {
            return _gameContext.GetArchers().OrderBy(archer => archer.Item1);
        }

        public void CompactSeatsToHandles()
        {
            var compacted = _gameContext.GetPlayers()
                .OrderBy(entry => entry.Item1)
                .Select((entry, handle) => (handle, entry.Item2))
                .ToList();

            _gameContext.ResetArcherSelections();

            foreach ((var handle, var player) in compacted)
            {
                _gameContext.AddArcher(handle, player);
            }
        }

        public void ApplyToGame()
        {
            for (int seat = 0; seat < TFGame.Players.Length; seat++)
            {
                TFGame.Players[seat] = false;
            }

            foreach ((var handle, var archerAlt) in GetFinalArchers())
            {
                var splitted = archerAlt.Split('-');

                Enum.TryParse(splitted[1], out ArcherData.ArcherTypes alt);

                TFGame.Characters[handle] = int.Parse(splitted[0]);
                TFGame.AltSelect[handle] = alt;
                TFGame.Players[handle] = true;
            }
        }

        public void RemoveArcher(int playerIndex)
        {
            _gameContext.RemoveArcher(playerIndex);
        }

        public void Reset()
        {
            _gameContext.ResetArcherSelections();
        }
    }
}
