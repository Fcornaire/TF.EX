using Monocle;
using MonoMod.Utils;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public enum RawMenuButton { Confirm, Back, Up, Down, Start }

    public static class RawMenuInput
    {
        public static bool Pressed(RawMenuButton button)
        {
            foreach (var input in TFGame.PlayerInputs)
            {
                if (input is KeyboardInput keyboard && KeyboardPressed(keyboard.Config, button))
                {
                    return true;
                }

                if (input is XGamepadInput pad && pad.Config != null && pad.XGamepad != null && PadPressed(pad, button))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool KeyboardPressed(KeyboardConfig config, RawMenuButton button)
        {
            if (config == null)
            {
                return false;
            }

            return button switch
            {
                RawMenuButton.Confirm => MInput.Keyboard.Pressed(config.Jump),
                RawMenuButton.Back => MInput.Keyboard.Pressed(config.Shoot, config.AltShoot),
                RawMenuButton.Up => MInput.Keyboard.Pressed(config.Up),
                RawMenuButton.Down => MInput.Keyboard.Pressed(config.Down),
                _ => MInput.Keyboard.Pressed(config.Start),
            };
        }

        private static bool PadPressed(XGamepadInput pad, RawMenuButton button)
        {
            return button switch
            {
                RawMenuButton.Confirm => PadButtonPressed(pad, pad.Config.Jump),
                RawMenuButton.Back => PadButtonPressed(pad, pad.Config.Shoot) || PadButtonPressed(pad, pad.Config.AltShoot),
                RawMenuButton.Up => PadButtonPressed(pad, pad.Config.Up) || pad.XGamepad.LeftStickUpPressed(0.5f),
                RawMenuButton.Down => PadButtonPressed(pad, pad.Config.Down) || pad.XGamepad.LeftStickDownPressed(0.5f),
                _ => PadButtonPressed(pad, pad.Config.Start),
            };
        }

        private static bool PadButtonPressed(XGamepadInput pad, Microsoft.Xna.Framework.Input.Buttons[] buttons)
        {
            return (bool)DynamicData.For(pad).Invoke("PressedButton", buttons);
        }
    }
}
