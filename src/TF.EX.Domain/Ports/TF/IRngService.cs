using TF.EX.Domain.Models.State;

namespace TF.EX.Domain.Ports.TF
{
    public interface IRngService
    {
        void SetSeed(int seed);
        Rng Get();

        int GetSeed();
        void UpdateState(ICollection<RngGenType> genTypes);
        void AddGen(RngGenType genType);
        void Reset();

    }
}
