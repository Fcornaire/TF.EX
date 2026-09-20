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

            var name = ServiceCollections.ResolveReplayService()?.ArcherNamesBySeat()?.ElementAtOrDefault(seat);

            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
    }
}
