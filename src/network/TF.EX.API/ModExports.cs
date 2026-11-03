using FortRise;
using System;
using MonoMod.ModInterop;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Interop;
using TowerFall;

namespace TF.EX.API
{
    /// <summary>
    /// Mod interop with other mods.
    /// </summary>
    [ModExportName("TF.EX.API")]
    public static class ModExports
    {
        /// <summary>
        /// Determine if the game is currently playing online.
        ///
        /// <para>Might be useful i dont know</para>
        /// </summary>
        public static bool IsPlayingOnline()
        {
            return TF.EX.Domain.ServiceCollections.ResolveNetplayManager().IsInit();
        }

        /// <summary>
        /// Add a game mode to the online lobbies, next to Last Man Standing, Headhunters and Team Deathmatch. Returns its id ("ModName/name").
        ///
        /// <para><c>createRoundLogic</c> makes the mode's round logic; null plays the mode on EX's own (as Last Man Standing, or Team
        /// Deathmatch for a team mode), its rules in the mod's hooks. <c>spawnPlayers</c> spawns a round's players: EX calls it once the
        /// session is up, at the next rounds and when a replay starts.</para>
        /// </summary>
        public static string RegisterGameMode(Mod module, string name, bool isTeamMode, int minimumPlayers, Func<Session, RoundLogic> createRoundLogic, Action<RoundLogic> spawnPlayers)
        {
            return NetplayGameModes.Register(module.Meta.Name, name, isTeamMode, minimumPlayers, createRoundLogic, spawnPlayers).Id;
        }

        /// <summary>
        /// Remove a game mode from the online lobbies.
        /// </summary>
        public static void UnregisterGameMode(Mod module, string name)
        {
            NetplayGameModes.Unregister(module.Meta.Name, name);
        }

        /// <summary>
        /// Register custom SaveState/LoadState events for a game mode, saved only while that mode is played.
        /// </summary>
        public static void RegisterGameModeStateEvents(Mod module, string name, Func<byte[]> OnGetState, Action<byte[]> OnLoadState)
        {
            var id = $"{module.Meta.Name}/{name}";

            StateApi.Current?.RegisterStateEvents(module.Meta.Name, name, () => NetplayGameModes.Selected()?.Id == id ? OnGetState() : [], OnLoadState);
        }

        /// <summary>
        /// The id of the game mode played online.
        /// </summary>
        public static string NetplayGameMode()
        {
            return NetplayGameModes.Selected()?.Id ?? "";
        }

        /// <summary>
        /// To call in the mode's RoundLogic.OnLevelLoadFinish. True when the round is played online: EX spawns the players
        /// (<c>spawnPlayers</c>), so the round logic must not.
        /// </summary>
        public static bool OnNetplayLevelLoadFinish(RoundLogic roundLogic)
        {
            if (!roundLogic.Session.MatchSettings.Mode.IsNetplay() || roundLogic is TF.EX.Domain.Ports.IRoundPlayerSpawner)
            {
                return false;
            }

            NetplayGameModes.OnLevelLoadFinish(roundLogic, () => NetplayGameModes.TrySpawnRoundPlayers(roundLogic, withVersusStart: true));

            return true;
        }
    }
}
