using Monocle;
using TowerFall;

namespace TF.Replay.Domain
{
    public class PlaybackController : PlayerInput
    {
        private readonly int seat;

        public PlaybackController(int seat)
        {
            this.seat = seat;
        }

        public override bool MenuConfirm => false;

        public override bool MenuConfirmCheck => false;

        public override bool MenuBack => false;

        public override bool MenuStart => false;

        public override bool MenuStartCheck => false;

        public override bool MenuAlt => false;

        public override bool MenuAlt2 => false;

        public override bool MenuUp => false;

        public override bool MenuDown => false;

        public override bool MenuRight => false;

        public override bool MenuLeft => false;

        public override bool MenuUpCheck => false;

        public override bool MenuDownCheck => false;

        public override bool MenuLeftCheck => false;

        public override bool MenuRightCheck => false;

        public override bool MenuAltCheck => false;

        public override bool MenuAlt2Check => false;

        public override bool MenuBackCheck => false;

        public override bool MenuSkipReplay => false;

        public override bool MenuSaveReplay => false;

        public override bool MenuSaveReplayCheck => false;

        public override Subtexture Icon => TFGame.MenuAtlas["controls/xb360/player1"];

        public override Subtexture MoveIcon => TFGame.MenuAtlas["controls/xb360/stick"];

        public override Subtexture JumpIcon => TFGame.MenuAtlas["controls/xb360/a"];

        public override Subtexture ShootIcon => TFGame.MenuAtlas["controls/xb360/x"];

        public override Subtexture AltShootIcon => TFGame.MenuAtlas["controls/xb360/b"];

        public override Subtexture DodgeIcon => TFGame.MenuAtlas["controls/xb360/rt"];

        public override Subtexture StartIcon => TFGame.MenuAtlas["controls/xb360/start"];

        public override Subtexture ConfirmIcon => TFGame.MenuAtlas["controls/xb360/a"];

        public override Subtexture BackIcon => TFGame.MenuAtlas["controls/xb360/b"];

        public override Subtexture AltIcon => TFGame.MenuAtlas["controls/xb360/rt"];

        public override Subtexture Alt2Icon => TFGame.MenuAtlas["controls/xb360/lt"];

        public override Subtexture SkipReplayIcon => TFGame.MenuAtlas["controls/xb360/start"];

        public override Subtexture SaveReplayIcon => Alt2Icon;

        public override bool Attached => true;

        public override string Name => "Playback";

        public override string ID => Guid.NewGuid().ToString();

        public override Subtexture ArrowsIcon => TFGame.MenuAtlas["controls/xb360/y"];

        public override bool MenuArrows => false;

        public override bool MenuArrowsCheck => false;

        public override InputState GetState() => PlaybackInputs.ForSeat(seat);
    }
}
