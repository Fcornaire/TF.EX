using MonoMod.Utils;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Models.State;

namespace TF.EX.TowerFallExtensions
{
    public static class SpriteExtensions
    {
        public static Sprite<T> GetState<T>(this Monocle.Sprite<T> sprite)
        {
            var dynSprite = DynamicData.For(sprite);
            var timer = dynSprite.Get<float>("timer");

            return new Sprite<T>
            {
                CurrentAnimID = sprite.CurrentAnimID,
                CurrentFrame = sprite.CurrentFrame,
                AnimationFrame = sprite.AnimationFrame,
                Timer = timer,
                Finished = sprite.Finished,
                Playing = sprite.Playing,
                Rate = sprite.Rate,
                Rotation = sprite.Rotation,
                Scale = sprite.Scale.ToModel(),
            };
        }

        public static void LoadState<T>(this Monocle.Sprite<T> sprite, Sprite<T> toLoad)
        {
            var dynSprite = DynamicData.For(sprite);

            if (toLoad.CurrentAnimID != null)
            {
                dynSprite.Set("CurrentAnimID", default(T));
                sprite.Play(toLoad.CurrentAnimID, true);
            }

            dynSprite.Set("CurrentAnimID", toLoad.CurrentAnimID);
            dynSprite.Set("CurrentFrame", toLoad.CurrentFrame);
            dynSprite.Set("AnimationFrame", toLoad.AnimationFrame);
            dynSprite.Set("timer", toLoad.Timer);
            dynSprite.Set("Playing", toLoad.Playing);
            dynSprite.Set("Finished", toLoad.Finished);
            dynSprite.Set("Rate", toLoad.Rate);
            dynSprite.Set("Rotation", toLoad.Rotation);
            dynSprite.Set("Scale", toLoad.Scale.ToTFVector());
        }
    }
}
