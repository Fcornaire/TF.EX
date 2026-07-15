using Monocle;
using TF.EX.Domain.Context;
using TF.EX.Domain.Models.State;
using TF.EX.Domain.Ports.TF;

namespace TF.EX.Domain.Services.TF
{
    public class RngService : IRngService
    {
        private readonly IGameContext _gameContext;

        public RngService(IGameContext gameContext)
        {
            _gameContext = gameContext;
        }

        public Rng Get()
        {
            return _gameContext.GetRng();
        }

        public int GetSeed()
        {
            return _gameContext.GetSeed();
        }

        public System.Random Gameplay => _gameContext.GetGameplayRandom();

        public void SetSeed(int seed)
        {
            _gameContext.SetSeed(seed);

            Calc.Random = new System.Random(seed);
        }

        public void LoadState(Rng rng)
        {
            _gameContext.UpdateRng(rng);
        }

        public void Reset()
        {
            _gameContext.SetSeed(_gameContext.GetSeed());
        }
    }
}
