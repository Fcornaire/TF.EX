using Microsoft.Xna.Framework;
using Monocle;
using TowerFall;

namespace TF.EX.Domain.CustomComponent
{
    public class LobbyVersusCoinButton : LobbyBorderButton
    {

        private string[] lengthNames = new string[4] { "INSTANT MATCH", "QUICK MATCH", "STANDARD MATCH", "EPIC MATCH" };
        private static float[] GoalMultiplier = new float[4] { 0.01f, 0.5f, 1f, 2f };

        private Sprite<int> coinSprite;

        public LobbyVersusCoinButton(Vector2 position, Vector2 tweenFrom) : base(position, tweenFrom, 168, 34)
        {
            coinSprite = VersusCoinButton.GetCoinSprite();
            coinSprite.CenterOrigin();
            coinSprite.Y = -1f;
            coinSprite.Visible = true;
            Add(coinSprite);
        }

        public override void Update()
        {
            base.Update();

            if (base.Selected)
            {
                var matchLength = (MatchSettings.MatchLengths)ownLobby.GameData.MatchLength;

                if (MenuInput.Right && matchLength < MatchSettings.MatchLengths.Epic)
                {
                    Sounds.ui_move2.Play();
                    ownLobby.GameData.MatchLength++;
                    base.OnConfirm();
                    UpdateSides((MatchSettings.MatchLengths)ownLobby.GameData.MatchLength);
                }
                else if (MenuInput.Left && matchLength > MatchSettings.MatchLengths.Instant)
                {
                    Sounds.ui_move2.Play();
                    ownLobby.GameData.MatchLength--;
                    base.OnConfirm();
                    UpdateSides((MatchSettings.MatchLengths)ownLobby.GameData.MatchLength);
                }
            }
        }

        private void UpdateSides(MatchSettings.MatchLengths length)
        {
            DrawRight = length < MatchSettings.MatchLengths.Epic;
            DrawLeft = length > MatchSettings.MatchLengths.Instant;
        }


        public override void Render()
        {
            Sprite<int> sprite;

            sprite = coinSprite;

            string text = "PLAY TO: ";
            string text2 = " x " + GetGoalScore();
            float x = TFGame.Font.MeasureString(text).X;
            float x2 = TFGame.Font.MeasureString(text2).X;
            float num = x + sprite.Width + x2;
            Draw.OutlineTextCentered(TFGame.Font, lengthNames[(int)ownLobby.GameData.MatchLength], Position + new Vector2(0f, -6f), base.DrawColor, 2f);
            Draw.OutlineTextCentered(TFGame.Font, text, Position + new Vector2((0f - num) / 2f + x / 2f, 6f), base.DrawColor, 1f);
            Draw.OutlineTextCentered(TFGame.Font, text2, Position + new Vector2(num / 2f - x2 / 2f, 6f), base.DrawColor, 1f);
            sprite.X = (0f - num) / 2f + x + sprite.Width / 2f;
            sprite.Y = 6f;
            sprite.DrawOutline();

            base.Render();
        }

        public int GetGoalScore()
        {
            int num;
            switch ((TowerFall.Modes)ownLobby.GameData.Mode)
            {
                case Modes.LastManStanding:
                    num = PlayerGoals(5, 4, 3);
                    break;
                case Modes.HeadHunters:
                case Modes.Warlord:
                    num = PlayerGoals(5, 8, 10);
                    break;
                case Modes.TeamDeathmatch:
                    num = 5;
                    break;
                case Modes.Trials:
                case Modes.LevelTest:
                    num = 1;
                    break;
                default:
                    throw new Exception("No Goal value defined for this mode!");
            }

            return (int)Math.Ceiling((float)num * GoalMultiplier[(int)ownLobby.GameData.MatchLength]);
        }

        private int PlayerGoals(int p2goal, int p3goal, int p4goal)
        {
            return TFGame.PlayerAmount switch
            {
                3 => p3goal,
                4 => p4goal,
                _ => p2goal,
            };
        }
    }
}
