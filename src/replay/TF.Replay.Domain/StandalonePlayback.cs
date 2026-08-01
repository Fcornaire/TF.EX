namespace TF.Replay.Domain
{
    public static class StandalonePlayback
    {
        public static int Generation { get; private set; }

        public static void BeginSession() => Generation++;

        public static bool IsActive
        {
            get
            {
                var api = ServiceCollections.ResolveApi();
                var service = ServiceCollections.ResolveReplayService();

                return api != null
                    && service != null
                    && service.IsPlayback
                    && string.IsNullOrEmpty(api.GetRecordDriver());
            }
        }
    }
}
