using System;
using FortRise;
using MonoMod.ModInterop;
using TF.EX.Domain.Extensions;
using TF.EX.Domain.Interop;
using TF.EX.Domain.Ports;
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
        /// Make a versus mode of the mod playable online: the lobby offers it and the Netplay mode runs its round logic.
        ///
        /// <para>name is the one given to RegisterVersusGameMode. spawnPlayers spawns the players of a round (and sets
        /// RoundLogic.Players), as the mode's OnLevelLoadFinish does offline; online, TF.EX calls it once the session
        /// is established, at the next rounds and when a replay starts.</para>
        /// <para>A mode without a round logic of its own (one returning the game's, its rules in hooks) passes null: online it
        /// plays on TF.EX's round logic as the built-in mode it resembles (Last Man Standing, Team Deathmatch for a team mode).</para>
        /// </summary>
        public static void RegisterNetplayVersusMode(Mod module, string name, Action<RoundLogic> spawnPlayers)
        {
            NetplayVersusModes.Register($"{module.Meta.Name}/{name}", spawnPlayers);
        }

        /// <summary>
        /// The mod mode played online (its FortRise id, "ModName/ModeName"), empty when it isn't one. Online the match's
        /// MatchSettings.Mode is the Netplay mode: a mode's own hooks check this too.
        /// </summary>
        public static string NetplayVersusMode()
        {
            return NetplayVersusModes.Selected()?.Name ?? "";
        }

        /// <summary>
        /// The mode is no longer offered online.
        /// </summary>
        public static void UnregisterNetplayVersusMode(Mod module, string name)
        {
            NetplayVersusModes.Unregister($"{module.Meta.Name}/{name}");
        }

        /// <summary>
        /// To call in the mode's RoundLogic.OnLevelLoadFinish, after base.OnLevelLoadFinish(). True when the round is
        /// played online: TF.EX spawns the players, so the round logic must not.
        /// </summary>
        public static bool OnNetplayLevelLoadFinish(RoundLogic roundLogic)
        {
            if (NetplayVersusModes.Selected() == null || !roundLogic.Session.MatchSettings.Mode.IsNetplay() || roundLogic is IRoundPlayerSpawner)
            {
                return false;
            }

            NetplayVersusModes.OnLevelLoadFinish(roundLogic, () => NetplayVersusModes.TrySpawnRoundPlayers(roundLogic, withVersusStart: true));

            return true;
        }
    }
}
