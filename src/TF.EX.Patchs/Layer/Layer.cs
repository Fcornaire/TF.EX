using MonoMod.Utils;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.TowerFallExtensions;
using TowerFall;

namespace TF.EX.Patchs.Layer
{
    internal class LayerPatch : IHookable
    {
        private readonly INetplayManager _netplayManager;
        private readonly IHUDService _hudService;

        public LayerPatch(INetplayManager netplayManager, IHUDService hudService)
        {
            _netplayManager = netplayManager;
            _hudService = hudService;
        }

        public void Load()
        {
            On.Monocle.Layer.UpdateEntityList += Layer_UpdateEntityList;
            On.Monocle.Layer.Remove_Entity += Layer_Remove_Entity;
        }

        public void Unload()
        {
            On.Monocle.Layer.UpdateEntityList -= Layer_UpdateEntityList;
            On.Monocle.Layer.Remove_Entity -= Layer_Remove_Entity;
        }

        private void Layer_Remove_Entity(On.Monocle.Layer.orig_Remove_Entity orig, Monocle.Layer self, Monocle.Entity entity)
        {
            orig(self, entity);

            if (entity is VersusStart)
            {
                _hudService.Update(new Domain.Models.State.HUD.HUD
                {
                    VersusStart = new Domain.Models.State.HUD.VersusStart()
                });
            }
        }

        private void Layer_UpdateEntityList(On.Monocle.Layer.orig_UpdateEntityList orig, Monocle.Layer self)
        {
            if (self.IsGameplayLayer())
            {
                if (_netplayManager.IsRollbackFrame()) //Remove entities from the precedent frame
                {
                    var dynLayer = DynamicData.For(self);
                    var toAdd = dynLayer.Get<List<Monocle.Entity>>("toAdd");
                    var toRemove = dynLayer.Get<HashSet<Monocle.Entity>>("toRemove");
                    var toRemoveCache = dynLayer.Get<HashSet<Monocle.Entity>>("toRemoveCache");

                    toAdd.Clear();
                    toRemove.Clear();
                    toRemoveCache.Clear();
                }
            }

            orig(self);
        }
    }
}
