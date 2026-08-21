using TF.EX.Domain.Models;
using TF.EX.Domain.Scenarios;

namespace TF.EX.Domain
{
    public static class InputScripter
    {
        public static bool Enabled { get; private set; }

        public static int PlayerCount { get; private set; } = 2;

        private static ScriptedAct[][] _scripts = [];

        private static int _scriptOrigin = -1;

        public static void Start(int playerCount = 2, ScriptedAct[][] scripts = null)
        {
            PlayerCount = Math.Max(1, playerCount);
            _scripts = scripts ?? [];
            _scriptOrigin = -1;
            Enabled = true;
        }

        public static void MarkRoundBegun()
        {
            if (_scriptOrigin < 0)
            {
                _scriptOrigin = Externals.GGRSFFI.netplay_current_frame();
            }
        }

        public static void Stop()
        {
            Enabled = false;
        }

        public static Input[] GetAllInputs(int frame)
        {
            var inputs = new Input[PlayerCount];

            for (int seat = 0; seat < PlayerCount; seat++)
            {
                inputs[seat] = GetInput(frame, seat);
            }

            return inputs;
        }

        public static Input GetInput(int frame, int seat = 0)
        {
            if (frame < 0)
            {
                frame = 0;
            }

            if (seat < _scripts.Length && _scripts[seat] is { Length: > 0 } script)
            {
                if (_scriptOrigin < 0)
                {
                    return new Input();
                }

                return FromScript(script, Math.Max(0, frame - _scriptOrigin));
            }

            return new Input();
        }

        private static Input FromScript(ScriptedAct[] script, int frame)
        {
            var total = script.Sum(a => Math.Max(1, a.Frames));
            var at = frame % total;

            foreach (var act in script)
            {
                var length = Math.Max(1, act.Frames);

                if (at >= length)
                {
                    at -= length;
                    continue;
                }

                var edge = at == 0;

                return new Input
                {
                    move_x = act.MoveX,
                    move_y = act.MoveY,
                    jump_check = act.Jump ? 1 : 0,
                    jump_pressed = act.Jump && edge ? 1 : 0,
                    shoot_check = act.Shoot ? 1 : 0,
                    shoot_pressed = act.Shoot && edge ? 1 : 0,
                    alt_shoot_check = act.AltShoot ? 1 : 0,
                    alt_shoot_pressed = act.AltShoot && edge ? 1 : 0,
                    dodge_check = act.Dodge ? 1 : 0,
                    dodge_pressed = act.Dodge && edge ? 1 : 0,
                    arrow_pressed = 0,
                    aim_axis = new Vector2f { X = act.AimX, Y = act.AimY },
                    aim_right_axis = new Vector2f(),
                    disconnected = 0,
                };
            }

            return new Input();
        }
    }
}
