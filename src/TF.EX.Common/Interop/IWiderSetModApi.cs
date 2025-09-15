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
            if (widerSetModApi == null)
            {
                return (false, "WIDERSET MOD REQUIRED");
            }

            if (widerSetData.TryGetValue("IsWide", out string isWide) && bool.TryParse(isWide, out bool parsed) && parsed)
            {
                var canUse = isReplay ?
                      widerSetModApi != null :
                      widerSetModApi != null && widerSetModApi.IsWide;
                if (!canUse)
                {
                    return (false, "WIDERSET MOD ON WIDE REQUIRED");
                }

                return (true, "");
            }

            return (false, "WIDERSET MOD ON WIDE REQUIRED");
        }
    }
}
