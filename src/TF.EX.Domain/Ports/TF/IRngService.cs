using TF.EX.Domain.Models.State;

namespace TF.EX.Domain.Ports.TF
{
    public interface IRngService
    {
        void SetSeed(int seed);
        Rng Get();

        int GetSeed();

        System.Random Gameplay { get; }

        void LoadState(Rng rng);

        void Reset();
    }
}
