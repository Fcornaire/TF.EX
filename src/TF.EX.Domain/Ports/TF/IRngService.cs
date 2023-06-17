using System.Collections.Generic;
using TF.EX.Domain.Models.State;

namespace TF.EX.Domain.Ports.TF
{
    public interface IRngService
    {
        void SetSeed(int seed);
        Rng Get();

        int GetSeed();
        void UpdateState(List<RngGenType> genTypes);
        void AddGen(RngGenType genType);
    }
}
