using TF.EX.Domain.Models.WebSocket;
using TowerFall;

namespace TF.EX.Domain.Extensions
{
    public static class ArcherDataExtensions
    {
        public const int VanillaArcherCount = 9;

        public static bool Exists(int archerIndex, int altIndex)
        {
            if (archerIndex < 0 || ArcherData.Archers == null || archerIndex >= ArcherData.Archers.Length)
            {
                return false;
            }

            try
            {
                return ArcherData.Get(archerIndex, (ArcherData.ArcherTypes)altIndex) != null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static string GetCustomArcherId(int archerIndex, int altIndex)
        {
            if (archerIndex < VanillaArcherCount)
            {
                return "";
            }

            try
            {
                var entries = Interop.ArcherRegistryApi.Current?.GetAllArchers();

                if (entries == null)
                {
                    return "";
                }

                var entry = entries.FirstOrDefault(e => e?.Index == archerIndex && (int)e.Type == altIndex)
                    ?? entries.FirstOrDefault(e => e?.Index == archerIndex && e.Type == FortRise.ArcherEntryType.Normal)
                    ?? entries.FirstOrDefault(e => e?.Index == archerIndex);

                return entry?.Name ?? "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public static int ResolveCustomArcher(string customArcherId)
        {
            if (string.IsNullOrEmpty(customArcherId))
            {
                return -1;
            }

            try
            {
                var registered = Interop.ArcherRegistryApi.Current?.RegisteredArchers;

                return registered != null && registered.TryGetValue(customArcherId, out var entry)
                    ? entry?.Index ?? -1
                    : -1;
            }
            catch (Exception)
            {
                return -1;
            }
        }

        public static List<string> GetInstalledArcherMods(Func<string, string> versionLookup)
        {
            try
            {
                var entries = Interop.ArcherRegistryApi.Current?.GetAllArchers();

                if (entries == null)
                {
                    return new List<string>();
                }

                return entries
                    .Select(entry => entry?.Name ?? "")
                    .Where(name => name.Contains('/'))
                    .Select(name => name.Substring(0, name.IndexOf('/')))
                    .Distinct()
                    .OrderBy(mod => mod)
                    .Select(mod => $"{mod}@{versionLookup?.Invoke(mod) ?? ""}")
                    .ToList();
            }
            catch (Exception)
            {
                return new List<string>();
            }
        }

        public static Dictionary<int, (int Index, int Alt)> ResolveArchers(Lobby lobby)
        {
            var result = new Dictionary<int, (int, int)>();
            var players = lobby.Players.OrderBy(player => player.Seat).ToList();
            var usedVanilla = new HashSet<int>();

            foreach (var player in players)
            {
                if (string.IsNullOrEmpty(player.CustomArcherId)
                    && player.ArcherIndex >= 0 && player.ArcherIndex < VanillaArcherCount
                    && IsValidAlt(player.ArcherAltIndex)
                    && Exists(player.ArcherIndex, player.ArcherAltIndex))
                {
                    result[player.Seat] = (player.ArcherIndex, player.ArcherAltIndex);
                    usedVanilla.Add(player.ArcherIndex);
                }
            }

            foreach (var player in players)
            {
                if (result.ContainsKey(player.Seat) || string.IsNullOrEmpty(player.CustomArcherId))
                {
                    continue;
                }

                if (IsSharedByAllPlayers(player.CustomArcherId, player, players))
                {
                    var index = ResolveCustomArcher(player.CustomArcherId);

                    if (index >= VanillaArcherCount)
                    {
                        var alt = IsValidAlt(player.ArcherAltIndex) && Exists(index, player.ArcherAltIndex)
                            ? player.ArcherAltIndex
                            : (int)ArcherData.ArcherTypes.Normal;

                        if (Exists(index, alt))
                        {
                            result[player.Seat] = (index, alt);
                        }
                    }
                }
            }

            foreach (var player in players)
            {
                if (result.ContainsKey(player.Seat))
                {
                    continue;
                }

                result[player.Seat] = PickUnusedVanilla(player.ArcherAltIndex, usedVanilla);
            }

            return result;
        }

        private static bool IsValidAlt(int altIndex)
        {
            return altIndex >= 0 && altIndex <= (int)ArcherData.ArcherTypes.Secret;
        }

        public static (int, int) PickUnusedVanilla(int requestedAlt, ISet<int> usedVanilla)
        {
            if (!IsValidAlt(requestedAlt))
            {
                requestedAlt = (int)ArcherData.ArcherTypes.Normal;
            }

            for (int index = 0; index < VanillaArcherCount; index++)
            {
                if (usedVanilla.Contains(index))
                {
                    continue;
                }

                var alt = Exists(index, requestedAlt) ? requestedAlt : (int)ArcherData.ArcherTypes.Normal;

                if (!Exists(index, alt))
                {
                    continue;
                }

                usedVanilla.Add(index);
                return (index, alt);
            }

            return (0, 0);
        }

        private static bool IsSharedByAllPlayers(string customArcherId, Models.WebSocket.Player owner, List<Models.WebSocket.Player> players)
        {
            var mod = customArcherId.Substring(0, Math.Max(0, customArcherId.IndexOf('/')));

            if (string.IsNullOrEmpty(mod))
            {
                return false;
            }

            var ownerTag = owner.ArcherMods?.FirstOrDefault(tag => tag.StartsWith($"{mod}@", StringComparison.OrdinalIgnoreCase));

            if (string.IsNullOrEmpty(ownerTag))
            {
                return false;
            }

            return players.All(player => player.ArcherMods?.Contains(ownerTag, StringComparer.OrdinalIgnoreCase) == true);
        }
    }
}
