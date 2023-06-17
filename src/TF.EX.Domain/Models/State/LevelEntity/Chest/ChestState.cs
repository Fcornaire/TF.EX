using System;

namespace TF.EX.Domain.Models.State.LevelEntity.Chest
{
    public enum ChestState : int
    {
        WaitingToAppear,
        Appearing,
        Closed,
        Opening,
        Opened
    }

    public static class ChestStateExtensions
    {
        public static ChestState ToModel(this TowerFall.TreasureChest.States state)
        {
            switch (state)
            {
                case TowerFall.TreasureChest.States.Appearing: return ChestState.Appearing;
                case TowerFall.TreasureChest.States.Closed: return ChestState.Closed;
                case TowerFall.TreasureChest.States.Opened: return ChestState.Opened;
                case TowerFall.TreasureChest.States.Opening: return ChestState.Opening;
                case TowerFall.TreasureChest.States.WaitingToAppear: return ChestState.WaitingToAppear;
                default: throw new ArgumentOutOfRangeException(nameof(TowerFall.TreasureChest.States), state, null);
            }
        }

        public static TowerFall.TreasureChest.States ToTFModel(this ChestState playerStates)
        {
            switch (playerStates)
            {
                case ChestState.Appearing: return TowerFall.TreasureChest.States.Appearing;
                case ChestState.Closed: return TowerFall.TreasureChest.States.Closed;
                case ChestState.Opened: return TowerFall.TreasureChest.States.Opened;
                case ChestState.Opening: return TowerFall.TreasureChest.States.Opening;
                case ChestState.WaitingToAppear: return TowerFall.TreasureChest.States.WaitingToAppear;
                default: throw new ArgumentOutOfRangeException(nameof(ChestState), playerStates, null);
            }
        }
    }
}
