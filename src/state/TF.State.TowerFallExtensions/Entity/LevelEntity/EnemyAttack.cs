using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Utils;
using TF.State.Domain.Extensions;
using TF.State.Domain.Models.Entity.LevelEntity;

namespace TF.State.TowerFallExtensions.Entity.LevelEntity
{
    public static class EnemyAttackExtensions
    {
        public static EnemyAttack GetState(this TowerFall.EnemyAttack entity)
        {
            var dynAttack = DynamicData.For(entity);
            var enemy = dynAttack.Get<TowerFall.Enemy>("enemy");
            var hitbox = dynAttack.Get<TowerFall.WrapHitbox>("hitbox");

            return new EnemyAttack
            {
                ActualDepth = dynAttack.Get<double>("actualDepth"),
                EnemyActualDepth = DynamicData.For(enemy).Get<double>("actualDepth"),
                Offset = dynAttack.Get<Vector2>("offset").ToModel(),
                Width = hitbox.Width,
                Height = hitbox.Height,
                Timer = dynAttack.Get<Counter>("timer").Value,
            };
        }

        public static void LoadState(this TowerFall.EnemyAttack entity, EnemyAttack toLoad, TowerFall.Enemy enemy)
        {
            var dynAttack = DynamicData.For(entity);
            dynAttack.Set("Scene", TowerFall.TFGame.Instance.Scene);
            entity.Added();

            dynAttack.Set("actualDepth", toLoad.ActualDepth);
            dynAttack.Set("enemy", enemy);
            dynAttack.Set("offset", toLoad.Offset.ToTFVector());
            dynAttack.Set("deflectsArrows", false);
            dynAttack.Set("deflectionDirection", null);

            var hitbox = dynAttack.Get<TowerFall.WrapHitbox>("hitbox");
            hitbox.Width = toLoad.Width;
            hitbox.Height = toLoad.Height;

            DynamicData.For(dynAttack.Get<Counter>("timer")).Set("counter", toLoad.Timer);

            System.Action onHit = null;
            if (enemy is TowerFall.Bat bat)
            {
                var batType = DynamicData.For(bat).Get<TowerFall.Bat.BatType>("batType");
                if (batType == TowerFall.Bat.BatType.Bomb || batType == TowerFall.Bat.BatType.SuperBomb)
                {
                    onHit = () => DynamicData.For(bat).Invoke("Explode");
                }
            }
            dynAttack.Set("onHit", onHit);

            var facing = (float)(int)enemy.Facing;
            entity.Position = enemy.Position
                + new Vector2(toLoad.Offset.X * facing, toLoad.Offset.Y)
                + new Vector2(-toLoad.Width / 2f, -toLoad.Height / 2f);
        }
    }
}
