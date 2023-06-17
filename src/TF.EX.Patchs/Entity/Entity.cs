using TF.EX.Domain.Ports;

namespace TF.EX.Patchs.Entity
{
    internal class EntityPatch : IHookable
    {
        private INetplayManager _netplayManager;

        public EntityPatch(INetplayManager netplayManager)
        {
            _netplayManager = netplayManager;
        }

        public void Load()
        {
            On.Monocle.Entity.RemoveSelf += Entity_RemoveSelf;
        }

        public void Unload()
        {
            On.Monocle.Entity.RemoveSelf -= Entity_RemoveSelf;
        }

        private void Entity_RemoveSelf(On.Monocle.Entity.orig_RemoveSelf orig, Monocle.Entity self)
        {
            if (!_netplayManager.IsUpdating()) //This is mainly to prevent arrowCushion deleting arrow on RBF but should be usefull for all entities/scenario
            {
                orig(self);
            }
        }
    }
}
