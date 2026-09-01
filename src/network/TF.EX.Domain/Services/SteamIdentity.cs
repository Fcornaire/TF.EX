namespace TF.EX.Domain.Services
{
    public static class SteamIdentity
    {
        private static string _id;

        // Reflection to find steam id when possible
        public static string TryGet()
        {
            if (_id != null)
            {
                return _id;
            }

            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Steamworks.NET");
                var getSteamId = assembly?.GetType("Steamworks.SteamUser")?.GetMethod("GetSteamID");
                var cSteamId = getSteamId?.Invoke(null, null);
                var raw = cSteamId?.GetType().GetField("m_SteamID")?.GetValue(cSteamId);

                if (raw is ulong steamId && steamId != 0)
                {
                    _id = $"steam:{steamId}";
                }
            }
            catch
            {
            }

            return _id;
        }
    }
}
