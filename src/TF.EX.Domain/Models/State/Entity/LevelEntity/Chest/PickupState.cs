using System;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Chest
{
    public enum PickupState : int
    {
        Arrows,
        BombArrows,
        SuperBombArrows,
        LaserArrows,
        BrambleArrows,
        DrillArrows,
        BoltArrows,
        FeatherArrows,
        TriggerArrows,
        PrismArrows,
        Shield,
        Wings,
        SpeedBoots,
        Mirror,
        TimeOrb,
        DarkOrb,
        LavaOrb,
        SpaceOrb,
        ChaosOrb,
        Bomb,
        Gem
    }

    public static class PickupsExtensions
    {
        public static PickupState ToModel(this TowerFall.Pickups pickups)
        {
            switch (pickups)
            {
                case TowerFall.Pickups.SpeedBoots: return PickupState.SpeedBoots;
                case TowerFall.Pickups.LavaOrb: return PickupState.LavaOrb;
                case TowerFall.Pickups.DarkOrb: return PickupState.DarkOrb;
                case TowerFall.Pickups.SpaceOrb: return PickupState.SpaceOrb;
                case TowerFall.Pickups.Wings: return PickupState.Wings;
                case TowerFall.Pickups.SuperBombArrows: return PickupState.SuperBombArrows;
                case TowerFall.Pickups.Arrows: return PickupState.Arrows;
                case TowerFall.Pickups.BoltArrows: return PickupState.BoltArrows;
                case TowerFall.Pickups.Bomb: return PickupState.Bomb;
                case TowerFall.Pickups.BombArrows: return PickupState.BombArrows;
                case TowerFall.Pickups.BrambleArrows: return PickupState.BrambleArrows;
                case TowerFall.Pickups.ChaosOrb: return PickupState.ChaosOrb;
                case TowerFall.Pickups.DrillArrows: return PickupState.DrillArrows;
                case TowerFall.Pickups.FeatherArrows: return PickupState.FeatherArrows;
                case TowerFall.Pickups.Gem: return PickupState.Gem;
                case TowerFall.Pickups.LaserArrows: return PickupState.LaserArrows;
                case TowerFall.Pickups.Mirror: return PickupState.Mirror;
                case TowerFall.Pickups.PrismArrows: return PickupState.PrismArrows;
                case TowerFall.Pickups.Shield: return PickupState.Shield;
                case TowerFall.Pickups.TimeOrb: return PickupState.TimeOrb;
                case TowerFall.Pickups.TriggerArrows: return PickupState.TriggerArrows;
            }
            throw new NotImplementedException($"Can't create a Pickups for unkwon value {pickups}");
        }

        public static TowerFall.Pickups ToTFModel(this PickupState playerStates)
        {
            switch (playerStates)
            {
                case PickupState.SpeedBoots: return TowerFall.Pickups.SpeedBoots;
                case PickupState.LavaOrb: return TowerFall.Pickups.LavaOrb;
                case PickupState.DarkOrb: return TowerFall.Pickups.DarkOrb;
                case PickupState.SpaceOrb: return TowerFall.Pickups.SpaceOrb;
                case PickupState.Wings: return TowerFall.Pickups.Wings;
                case PickupState.SuperBombArrows: return TowerFall.Pickups.SuperBombArrows;
                case PickupState.Arrows: return TowerFall.Pickups.Arrows;
                case PickupState.BoltArrows: return TowerFall.Pickups.BoltArrows;
                case PickupState.Bomb: return TowerFall.Pickups.Bomb;
                case PickupState.BombArrows: return TowerFall.Pickups.BombArrows;
                case PickupState.BrambleArrows: return TowerFall.Pickups.BrambleArrows;
                case PickupState.ChaosOrb: return TowerFall.Pickups.ChaosOrb;
                case PickupState.DrillArrows: return TowerFall.Pickups.DrillArrows;
                case PickupState.FeatherArrows: return TowerFall.Pickups.FeatherArrows;
                case PickupState.Gem: return TowerFall.Pickups.Gem;
                case PickupState.LaserArrows: return TowerFall.Pickups.LaserArrows;
                case PickupState.Mirror: return TowerFall.Pickups.Mirror;
                case PickupState.PrismArrows: return TowerFall.Pickups.PrismArrows;
                case PickupState.Shield: return TowerFall.Pickups.Shield;
                case PickupState.TimeOrb: return TowerFall.Pickups.TimeOrb;
                case PickupState.TriggerArrows: return TowerFall.Pickups.TriggerArrows;

            }
            throw new NotImplementedException($"Can't create a Twoerfall Pickups for unkwon value {playerStates}");
        }

    }
}
