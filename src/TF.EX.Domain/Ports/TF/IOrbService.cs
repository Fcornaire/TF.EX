using TF.EX.Domain.Models.State.Orb;

namespace TF.EX.Domain.Ports.TF
{
    public interface IOrbService
    {
        void Clear();
        Orb GetOrb();
        void Save(Orb orb);
    }
}
