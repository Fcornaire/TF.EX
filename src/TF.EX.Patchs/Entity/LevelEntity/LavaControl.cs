using Monocle;
using MonoMod.Utils;
using TF.EX.Domain;
using TF.EX.Domain.Models.State.LevelEntity;
using TF.EX.Domain.Ports.TF;
using TF.EX.Patchs.Calc;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    public class LavaControlPatch : IHookable, IStateful<TowerFall.LavaControl, LavaControl>
    {
        private readonly IOrbService _orbService;

        public LavaControlPatch(IOrbService orbService)
        {
            _orbService = orbService;
        }


        public void Load()
        {
            On.TowerFall.LavaControl.ctor += LavaControl_ctor;
            On.TowerFall.LavaControl.Added += LavaControl_Added;
            On.TowerFall.LavaControl.Update += LavaControl_Update;
            On.TowerFall.LavaControl.Extend += LavaControl_Extend;
        }


        public void Unload()
        {
            On.TowerFall.LavaControl.ctor -= LavaControl_ctor;
            On.TowerFall.LavaControl.Added -= LavaControl_Added;
            On.TowerFall.LavaControl.Update -= LavaControl_Update;
            On.TowerFall.LavaControl.Extend -= LavaControl_Extend;
        }

        private void LavaControl_Extend(On.TowerFall.LavaControl.orig_Extend orig, TowerFall.LavaControl self, int ownerIndex)
        {
            CalcPatch.ShouldRegisterRng = true;
            orig(self, ownerIndex);
            CalcPatch.ShouldRegisterRng = false;

            Save(self);
        }

        private void LavaControl_Update(On.TowerFall.LavaControl.orig_Update orig, TowerFall.LavaControl self)
        {
            var lava = _orbService.GetOrb().Lava;

            if (!lava.IsDefault())
            {
                LoadState(lava, self);
            }

            orig(self);

            Save(self);
        }

        private void LavaControl_Added(On.TowerFall.LavaControl.orig_Added orig, TowerFall.LavaControl self)
        {
            CalcPatch.ShouldRegisterRng = true;
            orig(self);
            CalcPatch.ShouldRegisterRng = false;

            Save(self);
        }

        private void LavaControl_ctor(On.TowerFall.LavaControl.orig_ctor orig, TowerFall.LavaControl self, TowerFall.LavaControl.LavaMode mode, int ownerIndex)
        {
            CalcPatch.ShouldRegisterRng = true;
            orig(self, mode, ownerIndex);
            CalcPatch.ShouldRegisterRng = false;
        }

        public LavaControl GetState(TowerFall.LavaControl entity)
        {
            var dynLavaControl = DynamicData.For(entity);
            var lavas = dynLavaControl.Get<TowerFall.Lava[]>("lavas");

            var lavasToSave = new Lava[lavas.Length];

            for (int i = 0; i < lavasToSave.Length; i++)
            {
                var currentLava = lavas[i];
                var toSave = ServiceCollections.ServiceProvider.GetLavaPatch().GetState(currentLava);
                lavasToSave[i] = toSave;
            }

            var mode = dynLavaControl.Get<TowerFall.LavaControl.LavaMode>("mode");
            var ownerIndex = entity.OwnerIndex;
            var targetCounter = dynLavaControl.Get<Counter>("targetCounter");
            var target = entity.Target;

            return new LavaControl
            {
                Mode = mode,
                OwnerIndex = ownerIndex,
                TargetCounter = targetCounter.Value,
                Target = target,
                Lavas = lavasToSave
            };
        }

        public void LoadState(LavaControl toLoad, TowerFall.LavaControl entity)
        {
            var dynLavaControl = DynamicData.For(entity);
            var counter = dynLavaControl.Get<Counter>("targetCounter");
            var targetCounter = DynamicData.For(counter);
            targetCounter.Set("counter", toLoad.TargetCounter);

            entity.Target = toLoad.Target;

            var lavas = new TowerFall.Lava[toLoad.Lavas.Length];

            for (int i = 0; i < toLoad.Lavas.Length; i++)
            {
                var currentLava = toLoad.Lavas[i];
                var lavaToLoad = new TowerFall.Lava(entity, currentLava.side);
                ServiceCollections.ServiceProvider.GetLavaPatch().LoadState(currentLava, lavaToLoad);
                lavas[i] = lavaToLoad;
            }
            dynLavaControl.Set("lavas", lavas);
        }

        private void Save(TowerFall.LavaControl self)
        {
            var orb = _orbService.GetOrb();

            orb.Lava = GetState(self);

            _orbService.Save(orb);
        }
    }
}
