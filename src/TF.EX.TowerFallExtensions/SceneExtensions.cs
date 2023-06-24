using Monocle;
using TowerFall;

namespace TF.EX.TowerFallExtensions
{
    public static class SceneExtensions
    {
        public static bool AreAllPlayersFrozen(this Scene scene)
        {
            foreach (Player player in scene[GameTags.Player])
            {
                if (player.State != Player.PlayerStates.Frozen)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsLocalPlayerFrozen(this Monocle.Scene scene)
        {
            if (scene == null)
            {
                return false;
            }

            var players = scene[GameTags.Player];
            if (players != null && players.Count > 0)
            {
                var localPlayer = (TowerFall.Player)scene[GameTags.Player][0];

                if (localPlayer != null && localPlayer.State.Equals(TowerFall.Player.PlayerStates.Frozen))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// This extension method removes sounds if the entity related to the sound is removed.
        /// </summary>
        /// <param name="scene"></param>
        public static void AdjustSFX(this Scene scene) //TODO: find a way fast fordward the sound in a rollback frame
        {
            if (scene.GetVersusStart() == null)
            {
                Sounds.sfx_multiStartLevel.Stop();
            }
        }

        public static VersusStart GetVersusStart(this Scene scene)
        {
            return scene.Layers.SelectMany(layer => layer.Value.Entities)
                .FirstOrDefault(ent => ent is VersusStart) as VersusStart;
        }

        public static void ClearVersusStart(this Scene scene)
        {
            var versusStart = scene.GetVersusStart();
            if (versusStart != null)
            {
                Sounds.sfx_multiStartLevel.Stop();
                Sounds.sfx_trainingStartLevelStone.Stop();
                Sounds.sfx_trainingStartLevelOut.Stop();
                scene.Layers.FirstOrDefault(layer => layer.Value.Index == versusStart.LayerIndex).Value.Entities.Remove(versusStart);
                versusStart.Removed();
            }
        }

        public static void Delete<T>(this Scene scene) where T : Monocle.Entity
        {
            var entity = scene.Layers.SelectMany(layer => layer.Value.Entities)
                .FirstOrDefault(ent => ent is T) as T;

            if (entity != null)
            {
                scene.Layers.FirstOrDefault(layer => layer.Value.Index == entity.LayerIndex).Value.Entities.Remove(entity);
                entity.Removed();
            }
        }

        public static T Get<T>(this Scene scene) where T : Monocle.Entity
        {
            return scene.Layers.SelectMany(layer => layer.Value.Entities)
                .FirstOrDefault(ent => ent is T) as T;
        }


        public static Layer GetMenuLayer(this Scene scene)
        {
            return scene.Layers.Where(l => l.Value.Index == -1).FirstOrDefault().Value;
        }

        public static FightButton GetFightButton(this Scene scene)
        {
            var layer = scene.GetMenuLayer();

            return layer.Entities.Where(ent => ent is FightButton).Select(ent => ent as FightButton).FirstOrDefault();
        }

        public static List<BladeButton> GetBladeButtons(this Scene scene)
        {
            var layer = scene.GetMenuLayer();

            return layer.Entities.Where(ent => ent is BladeButton).Select(ent => ent as BladeButton).ToList();
        }


        public static void Sort(this Scene scene, Comparison<Entity> comparison)
        {
            scene.GetGameplayLayer().Entities.Sort(comparison);
        }

        public static VersusMatchResults GetMatchResults(this Scene scene)
        {
            return scene.Layers.SelectMany(layer => layer.Value.Entities)
                .FirstOrDefault(ent => ent is VersusMatchResults) as VersusMatchResults;
        }

        public static VersusRoundResults GetRoundResults
            (this Scene scene)
        {
            return scene.Layers.SelectMany(layer => layer.Value.Entities)
                .FirstOrDefault(ent => ent is VersusRoundResults) as VersusRoundResults;
        }

        public static Layer GetGameplayLayer(this Scene scene)
        {
            return scene.Layers.Where(l => l.Value.Index == 0).FirstOrDefault().Value;
        }

        public static Layer GetVersusStartLayer(this Scene scene)
        {
            return scene.Layers.Where(l => l.Value.Index == 3).FirstOrDefault().Value;
        }

        public static List<TreasureChest> GetChests(this Scene scene)
        {
            if (scene[GameTags.TreasureChest] != null && scene[GameTags.TreasureChest].Count > 0)
            {
                return scene[GameTags.TreasureChest].Select(arrow => arrow as TowerFall.TreasureChest).ToList();
            }
            return new List<TreasureChest>();
        }

        public static List<Pickup> GetPickups(this Scene scene)
        {
            var gameplayLayer = scene.GetGameplayLayer();

            var res = new List<Pickup>();

            foreach (Entity entity in gameplayLayer.Entities)
            {
                if (entity is Pickup)
                {
                    res.Add((Pickup)entity);
                }
            }

            return res;
        }

        public static List<Lantern> GetLanterns(this Scene scene)
        {
            var gameplayLayer = scene.GetGameplayLayer();

            var res = new List<Lantern>();

            foreach (Entity entity in gameplayLayer.Entities)
            {
                if (entity is Lantern)
                {
                    res.Add((Lantern)entity);
                }
            }

            return res;
        }

        public static void ResetState(this Scene scene)
        {
            scene.ClearArrows();
            scene.ClearChests();
            scene.ClearPickups();
            scene.ClearPlayerCorpses();
        }

        public static void ClearArrows(this Scene scene)
        {
            var originalArrow = scene.GetGameplayLayer().Entities.Where(ent => ent is Arrow).Select((arrow) => arrow as TowerFall.Arrow).ToList();

            if (originalArrow.Count > 0)
            {
                originalArrow.ForEach((arrowToRemove) =>
                {
                    scene.GetGameplayLayer().Entities.Remove(arrowToRemove);
                    arrowToRemove.Removed();
                });
            }
        }

        public static void ClearHats(this Scene scene)
        {
            var originalHat = scene[GameTags.Hat].Select((arrow) => arrow as TowerFall.Hat).ToList();

            if (originalHat.Count > 0)
            {
                originalHat.ForEach((hatToRemove) =>
                {
                    scene.GetGameplayLayer().Entities.Remove(hatToRemove);
                    hatToRemove.Removed();
                });
            }
        }

        public static void ClearChests(this Scene scene)
        {
            var originalChests = scene[GameTags.TreasureChest].Select((chest) => chest as TowerFall.TreasureChest).ToList();

            if (originalChests.Count > 0)
            {
                originalChests.ForEach((chestToRemove) =>
                {
                    scene.GetGameplayLayer().Entities.Remove(chestToRemove);
                    chestToRemove.Removed();
                });
            }
        }

        public static void ClearPickups(this Scene scene)
        {
            var originalPickups = scene.GetPickups();

            if (originalPickups.Count > 0)
            {
                originalPickups.ForEach((pickupToRemove) =>
                {
                    scene.GetGameplayLayer().Entities.Remove(pickupToRemove);
                    pickupToRemove.Removed();
                });
            }
        }

        public static void ClearPlayerCorpses(this Scene scene)
        {
            var originalCorpses = scene[GameTags.Corpse].Select((chest) => chest as TowerFall.PlayerCorpse).ToList();

            if (originalCorpses.Count > 0)
            {
                originalCorpses.ForEach((toRemove) =>
                {
                    scene.GetGameplayLayer().Entities.Remove(toRemove);
                    toRemove.Removed();
                });
            }
        }

        public static Miasma GetMiasma(this Scene scene)
        {
            var gameplay = scene.GetGameplayLayer();

            var entity = gameplay.Entities.Where(ent => ent is Miasma).FirstOrDefault();

            if (entity != null)
            {
                return entity as Miasma;
            }

            return null;
        }

        public static PlayerCorpse GetPlayerCorpseByPlayerIndex(this Scene scene, int playerIndex)
        {
            var gameplay = scene.GetGameplayLayer();
            var entity = gameplay.Entities.Where(ent => ent is PlayerCorpse && (ent as PlayerCorpse).PlayerIndex == playerIndex).FirstOrDefault();
            if (entity != null)
            {
                return entity as PlayerCorpse;
            }
            return null;
        }

        public static LavaControl GetLavaControl(this Scene scene)
        {
            var gameplay = scene.GetGameplayLayer();

            var entity = gameplay.Entities.Where(ent => ent is LavaControl).FirstOrDefault();

            if (entity != null)
            {
                return entity as LavaControl;
            }

            return null;
        }

        public static Lava[] GetLavas(this Scene scene)
        {
            var gameplay = scene.GetGameplayLayer();

            var entities = gameplay.Entities.Where(ent => ent is Lava).Select(ent => ent as Lava).ToArray();

            return entities;
        }


        //TODO: refacto to use generic method
        public static void RemoveMiasma(this Scene scene)
        {
            var gameplay = scene.GetGameplayLayer();

            var entity = gameplay.Entities.Where(ent => ent is Miasma).FirstOrDefault();

            if (entity != null)
            {
                gameplay.Entities.Remove(entity);
                entity.Removed();
            }
        }

        public static List<Platform> GetPlatforms(this Scene scene)
        {
            return scene.Layers.SelectMany(layer => layer.Value.Entities)
                .Where(ent => ent is Platform)
                .Select(ent => ent as Platform)
                .ToList();
        }
    }
}
