using Monocle;
using TowerFall;

namespace TF.EX.TowerFallExtensions
{
    public static class PlayerExtensions
    {

        public static int GetIndexAsPlayer(this Actor actor, Scene scene)
        {
            var index = -1;

            for (int i = 0; i < scene[GameTags.Player].Count; i++)
            {
                if (scene[GameTags.Player][i].Equals(actor))
                {
                    index = i; break;
                }
            }

            return index;
        }
    }
}
