using TowerFall;

namespace TF.EX.TowerFallExtensions
{
    public static class TFGameExtensions
    {
        public static void ResetVersusChoices()
        {
            for (int i = 0; i < TFGame.Players.Length; i++)
            {
                TFGame.Players[i] = false;
            }

            for (int i = 0; i < TFGame.Characters.Length; i++)
            {
                TFGame.Characters[i] = i;
            }
        }
    }
}
