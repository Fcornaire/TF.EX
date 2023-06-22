using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Ports.TF;
using TF.EX.Patchs.Calc;
using TF.EX.Patchs.Extensions;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    public class LavaPatch : IHookable, IStateful<TowerFall.Lava, TF.EX.Domain.Models.State.LevelEntity.Lava>
    {

        private readonly IOrbService _orbService;

        public LavaPatch(IOrbService orbService)
        {
            _orbService = orbService;
        }

        public void Load()
        {
            On.TowerFall.Lava.Update += Lava_Update;
            On.TowerFall.Lava.BubbleComplete += Lava_BubbleComplete;
        }

        public void Unload()
        {
            On.TowerFall.Lava.Update -= Lava_Update;
            On.TowerFall.Lava.BubbleComplete -= Lava_BubbleComplete;
        }

        private void Lava_BubbleComplete(On.TowerFall.Lava.orig_BubbleComplete orig, TowerFall.Lava self, Sprite<int> bubble)
        {
            CalcPatch.IgnoreToRegisterRng();
            orig(self, bubble);
            CalcPatch.UnignoreToRegisterRng();
        }

        private void Lava_Update(On.TowerFall.Lava.orig_Update orig, TowerFall.Lava self)
        {
            var orb = _orbService.GetOrb();

            if (!orb.Lava.IsDefault())
            {
                LoadState(orb.Lava.Lavas, self);
            }

            orig(self);

            Save(self);
        }

        private void Save(TowerFall.Lava self)
        {
            var orb = _orbService.GetOrb();

            var lava = GetState(self);

            var shouldAdd = true;

            for (int i = 0; i < orb.Lava.Lavas.Length; i++)
            {
                var lavaSaved = orb.Lava.Lavas[i];
                if (lavaSaved.side == self.Side)
                {
                    orb.Lava.Lavas[i] = lava;
                    shouldAdd = false;
                }
            }

            if (shouldAdd)
            {
                var old = orb.Lava.Lavas;
                orb.Lava.Lavas = new Domain.Models.State.LevelEntity.Lava[orb.Lava.Lavas.Length + 1];

                for (int i = 0; i < old.Length; i++)
                {
                    orb.Lava.Lavas[i] = old[i];
                }
                orb.Lava.Lavas[orb.Lava.Lavas.Length - 1] = lava;
            }

            _orbService.Save(orb);
        }

        private void LoadState(TF.EX.Domain.Models.State.LevelEntity.Lava[] lavas, TowerFall.Lava self)
        {
            foreach (var lava in lavas)
            {
                if (lava.side == self.Side)
                {
                    LoadState(lava, self);
                }
            }
        }

        public Domain.Models.State.LevelEntity.Lava GetState(TowerFall.Lava entity)
        {
            var dynLava = DynamicData.For(entity);
            var sine = dynLava.Get<SineWave>("sine");

            return new TF.EX.Domain.Models.State.LevelEntity.Lava
            {
                side = entity.Side,
                is_collidable = entity.Collidable,
                position = entity.Position.ToModel(),
                percent = entity.Percent,
                sine_counter = sine.Counter,
            };
        }

        public void LoadState(Domain.Models.State.LevelEntity.Lava toLoad, TowerFall.Lava entity)
        {
            var dynLava = DynamicData.For(entity);
            var sine = dynLava.Get<SineWave>("sine");

            dynLava.Set("Collidable", toLoad.is_collidable);
            dynLava.Set("Position", toLoad.position.ToTFVector());
            dynLava.Set("Percent", toLoad.percent);
            sine.UpdateAttributes(toLoad.sine_counter);
        }
    }
}
