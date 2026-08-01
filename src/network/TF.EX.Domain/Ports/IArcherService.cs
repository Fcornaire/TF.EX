using TF.EX.Domain.Models.WebSocket;

namespace TF.EX.Domain.Ports
{
    public interface IArcherService
    {
        public void Reset();
        public void AddArcher(int index, Player player);
        public IEnumerable<(int, string)> GetArchers();
        public IEnumerable<(int, string)> GetFinalArchers();
        public void CompactSeatsToHandles();
        public void ApplyToGame();
        void RemoveArcher(int playerIndex);
    }
}
