using TF.EX.Domain.Models.State;

namespace TF.EX.Domain.Ports
{
    public interface ISyncTestUtilsService
    {
        void AddFrame(int frame, GameState gs);
        void Remove(int fromFrame);
        string Compare(int frame);
    }
}
