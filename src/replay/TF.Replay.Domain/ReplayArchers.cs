namespace TF.Replay.Domain
{
    public static class ReplayArchers
    {
        public static string NameForSeat(int seat)
        {
            if (seat < 0)
            {
                return null;
            }

            var archers = ServiceCollections.ResolveReplayService()?.GetReplay()?.Informations?.Archers;
            var name = archers?.ElementAtOrDefault(seat)?.NetplayName;

            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
    }
}
