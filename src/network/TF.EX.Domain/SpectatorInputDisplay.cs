using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Monocle;
using TF.EX.Domain.Interop;
using TF.EX.Domain.Models;
using TowerFall;

namespace TF.EX.Domain
{
    public static class SpectatorInputDisplay
    {
        private const float GuideY = 226f;
        private const float GuideRight = 310f;
        private const float IconGap = 3f;
        private const float EntryGap = 8f;

        private static readonly Dictionary<string, Subtexture> _icons = new Dictionary<string, Subtexture>();

        private static int _seats;
        private static bool _started;
        private static bool _guideHidden;

        private static bool _toggleHeld;
        private static bool _brighterHeld;
        private static bool _dimmerHeld;
        private static bool _guideHeld;

        public static void Feed(IList<Input> inputs, int frame)
        {
            var api = InputDisplayerApi.Current;

            if (api == null || inputs == null || inputs.Count == 0)
            {
                return;
            }

            if (inputs.Count != _seats)
            {
                _seats = inputs.Count;
                _started = true;

                api.BeginSession(_seats);
            }

            for (int seat = 0; seat < _seats; seat++)
            {
                var input = inputs[seat];

                api.PushSeat(frame, seat, input.move_x, input.move_y,
                             input.jump_check != 0, input.shoot_check != 0,
                             input.alt_shoot_check != 0, input.dodge_check != 0);
            }
        }

        public static void Render(int frame)
        {
            var api = InputDisplayerApi.Current;

            if (api == null)
            {
                return;
            }

            Controls(api);

            if (api.IsEnabled)
            {
                api.RenderAt(frame);
            }
        }

        public static void Stop()
        {
            _icons.Clear();

            if (!_started)
            {
                return;
            }

            _started = false;
            _seats = 0;

            InputDisplayerApi.Current?.EndSession();
        }

        public static void RenderGuide(float uiOffset)
        {
            var api = InputDisplayerApi.Current;

            if (api == null)
            {
                return;
            }

            var right = GuideRight + uiOffset * 2f;

            if (_guideHidden)
            {
                DrawEntry("G", "GUIDE", ref right);
                return;
            }

            DrawEntry("G", "HIDE", ref right);
            DrawEntry("OemMinus", "INPUT OPACITY DOWN", ref right);
            DrawEntry("OemPlus", "INPUT OPACITY UP", ref right);
            DrawEntry("I", api.IsEnabled ? "INPUTS ON" : "INPUTS OFF", ref right);
        }

        private static void DrawEntry(string key, string label, ref float right)
        {
            var labelWidth = TFGame.Font.MeasureString(label).X;
            var icon = Icon(key);
            var iconWidth = icon?.Width ?? 0f;

            Draw.OutlineTextCentered(TFGame.Font, label,new Vector2(right - labelWidth / 2f, GuideY), Color.White, Color.Black);

            right -= labelWidth;

            if (icon != null)
            {
                right -= IconGap;
                Draw.TextureCentered(icon, new Vector2(right - iconWidth / 2f, GuideY), Color.White);
                right -= iconWidth;
            }

            right -= EntryGap;
        }

        private static Subtexture Icon(string key)
        {
            if (_icons.TryGetValue(key, out var cached))
            {
                return cached;
            }

            Subtexture icon = null;

            try
            {
                icon = TFGame.MenuAtlas[$"controls/keyboard/{key}"];
            }
            catch
            {
            }

            _icons[key] = icon;

            return icon;
        }

        private static void Controls(IInputDisplayerApi api)
        {
            if (Tapped(ref _toggleHeld, Keys.I))
            {
                api.SetEnabled(!api.IsEnabled);
            }

            if (Tapped(ref _brighterHeld, Keys.OemPlus, Keys.Add))
            {
                api.StepOpacity(1f);
            }

            if (Tapped(ref _dimmerHeld, Keys.OemMinus, Keys.Subtract))
            {
                api.StepOpacity(-1f);
            }

            if (Tapped(ref _guideHeld, Keys.G))
            {
                _guideHidden = !_guideHidden;
            }
        }

        private static bool Tapped(ref bool held, params Keys[] keys)
        {
            var down = false;

            foreach (var key in keys)
            {
                down |= MInput.Keyboard.Check(key);
            }

            var tapped = down && !held;
            held = down;

            return tapped;
        }

    }
}
