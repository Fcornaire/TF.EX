using Microsoft.Xna.Framework.Input;
using Monocle;
using TowerFall;

namespace TF.Replay.Domain
{
    public static class ReplayBrowserKeys
    {
        public static bool IsFavoritePressed() => MenuInput.MenuInputs.Any(input => input != null && input.MenuArrows);

        public static bool IsDeletePressed() => MInput.Keyboard.Pressed(Keys.Delete) || Takeover.PadPressed(Buttons.Back);

        public static string DeleteKeyLabel(PlayerInput input) => input is XGamepadInput ? "SELECT" : "DEL";
    }
}
