using Monocle;
using TF.State.Domain.Context;
using TF.State.Domain.Models;
using TF.State.Domain.Ports;

namespace TF.State.Domain.Services
{
    public class RngService : IRngService
    {
        private readonly IStateContext _stateContext;

        public RngService(IStateContext gameContext)
        {
            _stateContext = gameContext;
        }

        public Rng Get()
        {
            return _stateContext.GetRng();
        }

        public int GetSeed()
        {
            return _stateContext.GetSeed();
        }

        public System.Random Gameplay => _stateContext.GetGameplayRandom();

        public void SetSeed(int seed)
        {
            _stateContext.SetSeed(seed);

            Calc.Random = new System.Random(seed);
        }

        public void LoadState(Rng rng)
        {
            _stateContext.UpdateRng(rng);
        }

        public void Reset()
        {
            _stateContext.SetSeed(_stateContext.GetSeed());
        }
    }
}
