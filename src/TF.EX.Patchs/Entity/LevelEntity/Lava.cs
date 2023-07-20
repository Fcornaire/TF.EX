using Monocle;
using TF.EX.Domain.Models.State.Entity.LevelEntity;
using TF.EX.Domain.Ports.TF;
using TF.EX.Patchs.Calc;
using TF.EX.TowerFallExtensions.Entity.LevelEntity;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    public class LavaPatch : IHookable
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

            var lava = self.GetState();

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
                orb.Lava.Lavas = new Lava[orb.Lava.Lavas.Length + 1];

                for (int i = 0; i < old.Length; i++)
                {
                    orb.Lava.Lavas[i] = old[i];
                }
                orb.Lava.Lavas[orb.Lava.Lavas.Length - 1] = lava;
            }

            _orbService.Save(orb);
        }

        private void LoadState(Lava[] lavas, TowerFall.Lava self)
        {
            foreach (var lava in lavas)
            {
                if (lava.side == self.Side)
                {
                    self.LoadState(lava);
                }
            }
        }
    }
}
