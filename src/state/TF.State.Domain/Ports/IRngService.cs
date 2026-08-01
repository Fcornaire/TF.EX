using TF.State.Domain.Models;

namespace TF.State.Domain.Ports
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
