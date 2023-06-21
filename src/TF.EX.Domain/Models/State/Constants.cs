namespace TF.EX.Domain.Models.State
{
    public static class Constants //TODO: move ton Common project
    {
        public static readonly int GAMEPLAY_LAYER = 0;
        public static readonly float INITIAL_END_COUNTER = 90.0f;
        public static readonly float DEFAULT_MIASMA_COUNTER = 1500.0f;
        public static readonly int DEFAULT_COUROUTINE_TIMER = 0;
        public static readonly int MAX_CHEST = 6;
        public static readonly int CODE_LENGTH = 5;

        public static readonly float INITIAL_GAME_RATE_TARGET = 1f;

        public static readonly double MIASMA_CUSTOM_DEPTH = -1000000.0; //Miasma doesn't have an actual depth, we need id at the end of the level entity list
    }
}
