namespace TF.EX.Common.Interop
{
    public interface IWiderSetModApi
    {
        bool IsWide { get; set; }
    }

    public class WiderSetModApiData
    {
        public static string Name => "Teuria.WiderSet";

        public static (bool, string) CanUseWiderSet(Dictionary<string, string> widerSetData, IWiderSetModApi widerSetModApi, bool isReplay = false)
        {
            if (!widerSetData.TryGetValue("IsWide", out string isWide))
            {
                return (true, "");
            }

            if (!bool.TryParse(isWide, out bool parsed))
            {
                return (false, "WIDERSET MOD MISSING VALUE");
            }

            if (parsed)
            {
                if (widerSetModApi == null)
                {
                    return (false, "WIDERSET MOD REQUIRED");
                }

                var canUse = isReplay ?
                      widerSetModApi != null :
                      widerSetModApi != null && widerSetModApi.IsWide;
                if (!canUse)
                {
                    return (false, "WIDERSET MOD ON WIDE REQUIRED");
                }

                return (true, "");
            }
            else
            {
                var canUse = widerSetModApi == null || !widerSetModApi.IsWide;

                if (!canUse)
                {
                    return (false, "WIDERSET MOD NOT WIDE REQUIRED");
                }

                return (true, "");
            }
        }
    }
}
