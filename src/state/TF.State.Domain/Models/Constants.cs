namespace TF.State.Domain.Models
{
    public static class Constants
    {
        public static readonly int PLAYER_DEPTH = -100;
        public static readonly int PLAYER_PRISM_DEPTH = -98;
        public static readonly int MAX_SFX_DELAY = 10;
        public static readonly int SFX_STATE_LIFETIME = 60; //Desired sfx expire by age
        public static readonly int SFX_SNAPSHOT_HISTORY = 240;
        public static readonly float INITIAL_END_COUNTER = 90.0f;
        public static readonly float DEFAULT_MIASMA_COUNTER = 1500.0f;

        public static readonly float INITIAL_GAME_RATE_TARGET = 1f;

        public static readonly double MIASMA_CUSTOM_DEPTH = -1000000.0; //Miasma doesn't have an actual depth, we need id at the end of the level entity list

        public const string INVENTORY_INVISIBLE_DELEGATE = "InvisibleDelegate";
        public const string INVENTORY_SHIELD_DELEGATE = "ShieldDelegate";
    }
}
