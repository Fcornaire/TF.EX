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
            return Get().Seed;
        }

        public void SetSeed(int seed)
        {
            _gameContext.SetSeed(seed);
            Calc.Random = new Random(seed);
        }

        public void UpdateState(List<RngGenType> genTypes)
        {
            var rng = Get();
            rng.Gen_type = genTypes.ToList();
            _gameContext.UpdateRng(rng);
        }

        public void AddGen(RngGenType genType)
        {
            var rng = Get();
            rng.Gen_type.Add(genType);
            _gameContext.UpdateRng(rng);
        }
    }
}
