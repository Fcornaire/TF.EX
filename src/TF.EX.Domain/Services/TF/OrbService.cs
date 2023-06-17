using TF.EX.Domain.Context;
using TF.EX.Domain.Models.State.Orb;
using TF.EX.Domain.Ports.TF;

namespace TF.EX.Domain.Services.TF
{
    public class OrbService : IOrbService
    {
        private readonly IGameContext _context;

        public OrbService(IGameContext context)
        {
            _context = context;
        }

        public void Clear()
        {
            _context.ClearOrb();
        }

        public Orb GetOrb()
        {
            return _context.GetOrb();
        }

        public void Save(Orb orb)
        {
            _context.SaveOrb(orb);
        }
    }
}
