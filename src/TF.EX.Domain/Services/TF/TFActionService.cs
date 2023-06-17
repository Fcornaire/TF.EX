using TF.EX.Domain.Ports.TF;
using TowerFall;

namespace TF.EX.Domain.Services.TF
{
    public class TFActionService : ITFActionService
    {
        public void StartTest(Modes mode)
        {
            for (int i = 0; i < 4; i++)
            {
                TFGame.Players[i] = TFGame.PlayerInputs[i] != null;
            }

            MatchSettings matchSettings = MatchSettings.GetDefaultVersus();
            matchSettings.Mode = mode;
            (matchSettings.LevelSystem as VersusLevelSystem).StartOnLevel(4);
            new Session(matchSettings).StartGame();
        }
    }
}
