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

        private const float RowHeight = 16f;

        private static readonly List<Notification> _lives = new List<Notification>();

        private static readonly Queue<string> _deferred = new Queue<string>();

        public static bool IsDeferedOn;

        private float restingX;

        private float baseY;

        private bool leaving;

        private Notification(string text, int layer, float yOffset, int appearDuration = 20, int stayingDuration = 250, bool isSticky = false, bool withoutAnimation = false) : base(layer)
        {
            this.isSticky = isSticky;
            description = text.ToUpper();
            length = (int)Math.Ceiling(TFGame.Font.MeasureString(description).X / 10.0) + 1;

            restingX = Math.Max(0f, CenterX(layer) - length * 10f / 2f);

            baseY = 10 + yOffset;

            initialTweenPosition = new Vector2(-length * 10, baseY);
            initialPosition = initialTweenPosition;

            _lives.Add(this);

            Depth = -1000;

            if (!withoutAnimation)
            {
                Position = initialPosition;
            }
            else
            {
                Position = new Vector2(restingX, 10 + yOffset);
            }

            this.appearDuration = appearDuration;
            this.stayingDuration = stayingDuration;

            canvas = new Canvas(length * 10, 14);

            if (!withoutAnimation)
            {
                StartAnimation();
            }
            else if (!isSticky && stayingDuration > 0)
            {
                ScheduleRemoval();
            }
        }

        public override void Update()
        {
            base.Update();

            if (leaving)
            {
                return;
            }

            var slot = 0;

            foreach (var other in _lives)
            {
                if (other == this)
                {
                    break;
                }

                if (other.Scene == Scene)
                {
                    slot++;
                }
            }

            var targetY = baseY + slot * RowHeight;
            Position.Y += (targetY - Position.Y) * 0.25f;
        }

        public override void Removed()
        {
            base.Removed();
            _lives.Remove(this);
        }

        public override void SceneEnd()
        {
            base.SceneEnd();
            _lives.Remove(this);
        }

        private void ScheduleRemoval()
        {
            Alarm alarm = Alarm.Create(Alarm.AlarmMode.Oneshot, null, stayingDuration, true);
            alarm.OnComplete = RemoveSelf;

            Add(alarm);
        }

        public static Notification Create(Scene scene, string text, int appearDuration = 20, int stayingDuration = 250, bool isSticky = false, bool withoutAnimation = false)
        {
            var layerIndex = scene is MainMenu ? -1 : 4;
            RemoveSameText(scene, layerIndex, text.ToUpper());

            var yOffset = scene is MainMenu menu ? menu.UILayer.Camera.Y : 0f;

            var notification = new Notification(text, layerIndex, yOffset, appearDuration, stayingDuration, isSticky, withoutAnimation);

            switch (scene)
            {
                case MainMenu mainMenu:
                    mainMenu.Add(notification);
                    break;
                case Level level:
                    var dynNotification = DynamicData.For(notification);
                    dynNotification.Set("Scene", scene);

                    var layer = scene.Layers.Single(l => l.Key == layerIndex).Value;
                    layer.Entities.Add(notification);
                    break;
                default:
                    //FortRise.Logger.Error($"Notification not supported for scene {scene.GetType().Name}");
                    break;
            }

            return notification;
        }

        public static void CreateOrDefer(Scene scene, string text)
        {
            if (IsDeferedOn)
            {
                _deferred.Enqueue(text);
                return;
            }

            Create(scene, text);
        }

        public static void FlushDeferred(Scene scene)
        {
            while (_deferred.Count > 0)
            {
                Create(scene, _deferred.Dequeue());
            }
        }

        public static void ClearDeferred() => _deferred.Clear();

        private static float CenterX(int layerIndex)
        {
            var widerSet = layerIndex == 4 ? ServiceCollections.ResolveWiderSetModApi() : null;

            return 160f + (widerSet?.UIXOffset ?? 0f);
        }

        private static void RemoveSameText(Scene scene, int layerIndex, string text)
        {
            var layer = scene.Layers.Single(l => l.Key == layerIndex).Value;

            var toAdd = DynamicData.For(layer).Get<List<Entity>>("toAdd");

            foreach (var pending in toAdd.OfType<Notification>().Where(n => n.description == text).ToList())
            {
                toAdd.Remove(pending);
                pending.Removed();
            }

            foreach (var shown in layer.Entities.OfType<Notification>().Where(n => n.description == text).ToList())
            {
                shown.RemoveSelf();
            }
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
                Position = Vector2.Lerp(initialPosition, new Vector2(restingX, Position.Y), t.Eased);
            };

            if (!isSticky)
            {
                tween.OnComplete = (Tween t) =>
                {
                    initialTweenPosition = Position;
                    Alarm alarm = Alarm.Create(Alarm.AlarmMode.Oneshot, null, stayingDuration, true);
                    alarm.OnComplete = () =>
                    {
                        leaving = true;
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
