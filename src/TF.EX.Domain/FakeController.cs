using Monocle;
using TowerFall;

namespace TF.EX.Domain
{
    public class FakeController : PlayerInput
    {
        private Subtexture icon;

        private Subtexture iconMove;

        private Subtexture iconJump;

        private Subtexture iconShoot;

        private Subtexture iconAltShoot;

        private Subtexture iconDodge;

        private Subtexture iconStart;

        private Subtexture iconSkipReplay;

        private Subtexture iconConfirm;

        private Subtexture iconBack;

        private Subtexture iconAlt;

        private Subtexture iconAlt2;

        public FakeController()
        {
            icon = TFGame.MenuAtlas["controls/xb360/player1"];
            iconMove = TFGame.MenuAtlas["controls/xb360/stick"];
            iconJump = TFGame.MenuAtlas["controls/xb360/a"];
            iconShoot = TFGame.MenuAtlas["controls/xb360/x"];
            iconAltShoot = TFGame.MenuAtlas["controls/xb360/b"];
            iconDodge = TFGame.MenuAtlas["controls/xb360/rt"];
            iconStart = TFGame.MenuAtlas["controls/xb360/start"];
            iconConfirm = TFGame.MenuAtlas["controls/xb360/a"];
            iconBack = TFGame.MenuAtlas["controls/xb360/b"];
            iconAlt = TFGame.MenuAtlas["controls/xb360/rt"];
            iconAlt2 = TFGame.MenuAtlas["controls/xb360/lt"];
            iconSkipReplay = TFGame.MenuAtlas["controls/xb360/start"];
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

        public override Subtexture Icon => icon;

        public override Subtexture MoveIcon => iconMove;

        public override Subtexture JumpIcon => iconJump;

        public override Subtexture ShootIcon => iconShoot;

        public override Subtexture AltShootIcon => iconAltShoot;

        public override Subtexture DodgeIcon => iconDodge;

        public override Subtexture StartIcon => iconStart;

        public override Subtexture ConfirmIcon => iconConfirm;

        public override Subtexture BackIcon => iconBack;

        public override Subtexture AltIcon => iconAlt;

        public override Subtexture Alt2Icon => iconAlt2;

        public override Subtexture SkipReplayIcon => iconSkipReplay;

        public override Subtexture SaveReplayIcon => Alt2Icon;

        public override bool Attached => true;

        public override string Name => "Fake";

        public override string ID => Guid.NewGuid().ToString();

        public override InputState GetState()
        {
            return new InputState();
        }
    }
}
