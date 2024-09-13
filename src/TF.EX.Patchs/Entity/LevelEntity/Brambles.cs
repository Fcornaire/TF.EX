using Microsoft.Xna.Framework;
using MonoMod.Utils;
using System.Collections;
using TF.EX.Domain.Ports;
using TF.EX.Domain.Ports.TF;
using TF.EX.TowerFallExtensions;
using TF.EX.TowerFallExtensions.Entity.LevelEntity;
using TowerFall;

namespace TF.EX.Patchs.Entity.LevelEntity
{
    public class BramblesPatch : IHookable
    {
        private readonly INetplayManager netplayManager;
        private readonly ISessionService sessionService;

        public BramblesPatch(INetplayManager netplayManager, ISessionService sessionService)
        {
            this.netplayManager = netplayManager;
            this.sessionService = sessionService;
        }
        public void Load()
        {
            On.TowerFall.Brambles.Removed += Brambles_Removed;
            On.TowerFall.Brambles.CreateBrambles += Brambles_CreateBrambles;
            On.TowerFall.Brambles.MoveExactV += Brambles_MoveExactV;
            On.TowerFall.Brambles.Create += Brambles_Create;
        }

        public void Unload()
        {
            On.TowerFall.Brambles.Removed -= Brambles_Removed;
            On.TowerFall.Brambles.CreateBrambles -= Brambles_CreateBrambles;
            On.TowerFall.Brambles.MoveExactV -= Brambles_MoveExactV;
            On.TowerFall.Brambles.Create -= Brambles_Create;
        }

        private Brambles Brambles_Create(On.TowerFall.Brambles.orig_Create orig, int id, Vector2 position, int ownerIndex, int delay, bool shortTime)
        {
            return orig(id, position, ownerIndex, delay, shortTime);
        }

        private void Brambles_MoveExactV(On.TowerFall.Brambles.orig_MoveExactV orig, Brambles self, int move, Action<TowerFall.Platform> onCollide)
        {
            orig(self, move, onCollide);
        }

        private IEnumerator Brambles_CreateBrambles(On.TowerFall.Brambles.orig_CreateBrambles orig, Level level, Vector2 at, int ownerIndex, Action onComplete, int spread, bool shortTime)
        {
            if (!netplayManager.IsUpdating())
            {
                var movingPlatformsStates = level.GetAll<MovingPlatform>().Select(mp => mp.GetState()).ToList();

                sessionService.AddBramblesState(level.FrameCounter, movingPlatformsStates);
            }

            return orig(level, at, ownerIndex, onComplete, spread, shortTime);
        }

        private void Brambles_Removed(On.TowerFall.Brambles.orig_Removed orig, TowerFall.Brambles self)
        {
            orig(self);

            var dynBrambles = DynamicData.For(self);
            Stack<Brambles> cached = dynBrambles.Get<Stack<Brambles>>("cached");
            if (cached != null)
            {
                cached.Clear();
            }
        }
    }
}
