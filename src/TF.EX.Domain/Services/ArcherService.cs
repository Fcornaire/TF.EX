using TF.EX.Domain.Context;
using TF.EX.Domain.Models.WebSocket;
using TF.EX.Domain.Ports;

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
            var archers = _gameContext.GetArchers();

            var finalArchers = new List<(int, string)>
            {
                (_gameContext.GetLocalPlayerIndex(),archers.First((archer) => archer.Item1 == 0).Item2),
                (_gameContext.GetRemotePlayerIndex(),archers.First((archer) => archer.Item1 == 1).Item2)
            };

            return finalArchers;
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
