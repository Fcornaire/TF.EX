using MonoMod.Utils;
using TF.EX.Domain.Ports;
using TowerFall;

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
            On.Monocle.Entity.Removed += Entity_Removed;
        }

        public void Unload()
        {
            On.Monocle.Entity.RemoveSelf -= Entity_RemoveSelf;
            On.Monocle.Entity.Removed -= Entity_Removed;
        }

        private void Entity_Removed(On.Monocle.Entity.orig_Removed orig, Monocle.Entity self)
        {
            if (self is TowerFall.Lava) //Hack !!
            {
                var dynLava = DynamicData.For(self);
                dynLava.Set("Scene", TowerFall.TFGame.Instance.Scene); //TODO: Remove this hack, we should have a scene here, dunno why it's null
            }

            orig(self);
        }

        private void Entity_RemoveSelf(On.Monocle.Entity.orig_RemoveSelf orig, Monocle.Entity self)
        {
            if (!_netplayManager.IsUpdating()) //This is mainly to prevent arrowCushion deleting arrow on RBF but should be usefull for all entities/scenario
            {
                if (self is Explosion)  //Hack because cache i think
                {
                    var dynExplosion = DynamicData.For(self);
                    dynExplosion.Set("Scene", TowerFall.TFGame.Instance.Scene);
                    dynExplosion.Set("Level", TowerFall.TFGame.Instance.Scene as Level);
                }

                orig(self);
            }
        }
    }
}
