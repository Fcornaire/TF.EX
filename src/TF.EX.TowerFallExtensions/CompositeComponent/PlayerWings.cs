using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.EX.Domain.Models.State.Entity.LevelEntity.Player;

namespace TF.EX.TowerFallExtensions.CompositeComponent
{
    public static class PlayerWingsExtensions
    {
        public static PlayerWings GetState(this TowerFall.PlayerWings compositeComponent)
        {
            var dynEntity = DynamicData.For(compositeComponent);
            var wings = dynEntity.Get<Monocle.Sprite<string>>("sprite");
            var isGaining = dynEntity.Get<bool>("gaining");
            Tween tween = compositeComponent.Components.FirstOrDefault(c => c is Tween) as Tween;
            var spriteScaleTweenTimer = -1f;
            if (tween != null)
            {
                spriteScaleTweenTimer = tween.FramesLeft;
            }

            return new PlayerWings
            {
                Wings = wings != null ? wings.GetState() : null,
                IsGaining = isGaining,
                SpriteScaleTweenTimer = spriteScaleTweenTimer,
            };
        }

        public static void LoadState(this TowerFall.PlayerWings compositeComponent, PlayerWings toLoad)
        {
            var dynEntity = DynamicData.For(compositeComponent);

            var wingsSprite = dynEntity.Get<Monocle.Sprite<string>>("sprite");
            if (wingsSprite != null && toLoad.Wings != null)
            {
                wingsSprite.LoadState(toLoad.Wings);
            }

            dynEntity.Set("gaining", toLoad.IsGaining);

            compositeComponent.Components.Remove(compositeComponent.Components.FirstOrDefault(c => c is Tween) as Tween);

            if (toLoad.SpriteScaleTweenTimer > 0)
            {
                Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.BigBackOut, 40, start: true);
                var dynTween = DynamicData.For(tween);
                dynTween.Set("FramesLeft", toLoad.SpriteScaleTweenTimer);
                tween.OnUpdate = delegate (Tween t)
                {
                    var sprite = dynEntity.Get<Monocle.Sprite<string>>("sprite");
                    sprite.Scale = Vector2.One * t.Eased;
                };
                tween.OnComplete = delegate
                {
                    dynEntity.Set("gaining", false);
                };
                compositeComponent.Add(tween);
            }
        }
    }
}
