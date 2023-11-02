using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod.Utils;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class Notification : Entity
    {
        private Canvas canvas;

        private string description;

        private int length;

        private Vector2 initialPosition;
        private Vector2 initialTweenPosition;

        private int appearDuration;
        private int stayingDuration;
        private bool isSticky;

        private Notification(string text, int layer, int appearDuration = 20, int stayingDuration = 250, bool isSticky = false, bool withoutAnimation = false) : base(layer)
        {
            this.isSticky = isSticky;
            description = text.ToUpper();
            length = (int)Math.Ceiling(TFGame.Font.MeasureString(description).X / 10.0) + 1;

            initialTweenPosition = new Vector2(-length * 10, 10);
            initialPosition = initialTweenPosition;

            if (!withoutAnimation)
            {
                Position = initialPosition;
            }
            else
            {
                Position = new Vector2(length, 10);
            }

            this.appearDuration = appearDuration;
            this.stayingDuration = stayingDuration;

            canvas = new Canvas(length * 10, 14);

            if (!withoutAnimation)
            {
                StartAnimation();
            }

        }

        public static Notification Create(Scene scene, string text, int appearDuration = 20, int stayingDuration = 250, bool isSticky = false, bool withoutAnimation = false)
        {
            // Remove all previous notifications (TODO: manage multiple notifications ?)

            var layerIndex = scene is MainMenu ? -1 : 4;
            Clear(scene, layerIndex);

            var notification = new Notification(text, layerIndex, appearDuration, stayingDuration, isSticky, withoutAnimation);

            var dynNotification = DynamicData.For(notification);
            dynNotification.Set("Scene", scene);

            var layer = scene.Layers.Single(l => l.Key == layerIndex).Value;
            layer.Entities.Add(notification);

            return notification;
        }

        public static void Clear(Scene scene, int layerIndex)
        {
            var layer = scene.Layers.Single(l => l.Key == layerIndex).Value;

            var dynLayer = DynamicData.For(layer);
            List<Entity> toAdd = dynLayer.Get<List<Entity>>("toAdd");

            toAdd.Where(ent => ent is Notification).ToList().ForEach(ent =>
            {
                toAdd.Remove(ent);
                ent.Removed();
            });

            var notifs = layer.Entities.Where(ent => ent is Notification).ToList();

            foreach (var notif in notifs)
            {
                notif.RemoveSelf();
            }
        }
        private void StartAnimation()
        {
            Tween tween = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, appearDuration, true);
            tween.OnUpdate = (Tween t) =>
            {
                Position = Vector2.Lerp(initialPosition, new Vector2(length, Position.Y), t.Eased);
            };

            if (!isSticky)
            {
                tween.OnComplete = (Tween t) =>
                {
                    initialTweenPosition = Position;
                    Alarm alarm = Alarm.Create(Alarm.AlarmMode.Oneshot, null, stayingDuration, true);
                    alarm.OnComplete = () =>
                    {
                        Tween tween2 = Tween.Create(Tween.TweenMode.Oneshot, Ease.CubeOut, 10, true);
                        tween2.OnUpdate = (Tween t) =>
                        {
                            Position = Vector2.Lerp(initialTweenPosition, initialPosition, t.Eased);
                        };
                        tween2.OnComplete = (Tween t) =>
                        {
                            RemoveSelf();
                        };
                        Add(tween2);
                    };
                    Add(alarm);
                };
            }
            Add(tween);
        }

        public override void Render()
        {
            base.Render();

            Draw.Texture(TFGame.MenuAtlas["variants/bubbleEdge"], Position, Color.White, Vector2.Zero, Vector2.One);
            for (int index = 1; index < length - 1; ++index)
            {
                Draw.Texture(TFGame.MenuAtlas["variants/bubbleMiddle"], new Vector2(index * 10 + Position.X, Position.Y), Color.White, Vector2.Zero, Vector2.One);
            }
            Draw.Texture(TFGame.MenuAtlas["variants/bubbleEdge"], new Vector2(canvas.Width - 10 + Position.X, Position.Y), Color.White, Vector2.Zero, 1f, 0.0f, SpriteEffects.FlipHorizontally);
            Draw.TextCentered(TFGame.Font, description, new Vector2(canvas.Width / 2 + Position.X, Position.Y + 7f), Color.Black);
        }
    }
}
