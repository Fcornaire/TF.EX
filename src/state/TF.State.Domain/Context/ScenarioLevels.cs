namespace TF.State.Domain.Context
{
    public static class ScenarioLevels
    {
        public static List<string> Levels { get; private set; }

        public static bool IsActive => Levels != null && Levels.Count > 0;

        public static void Set(IEnumerable<string> levels)
        {
            Levels = levels?.ToList();
        }

        public static void Clear()
        {
            Levels = null;
        }
    }
}
