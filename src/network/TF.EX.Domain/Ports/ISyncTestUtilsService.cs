
namespace TF.EX.Domain.Ports
{
    public interface ISyncTestUtilsService
    {
        void AddFrame(int frame, byte[] state);
        void Remove(int fromFrame);
        string Compare(int frame);
        void Reset();
    }
}
