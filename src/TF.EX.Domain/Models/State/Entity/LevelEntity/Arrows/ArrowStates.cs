using System;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows
{
    public enum ArrowStates : int
    {
        Shooting = 0,
        Drilling = 1,
        Gravity = 2,
        Falling = 3,
        Stuck = 4,
        LayingOnGround = 5,
        Buried = 6
    }

    public static class ArrowStatesExtensions
    {
        public static ArrowStates ToModel(this TowerFall.Arrow.ArrowStates arrowStates)
        {
            switch (arrowStates)
            {
                case TowerFall.Arrow.ArrowStates.Shooting: return ArrowStates.Shooting;
                case TowerFall.Arrow.ArrowStates.Drilling: return ArrowStates.Drilling;
                case TowerFall.Arrow.ArrowStates.Gravity: return ArrowStates.Gravity;
                case TowerFall.Arrow.ArrowStates.Falling: return ArrowStates.Falling;
                case TowerFall.Arrow.ArrowStates.Stuck: return ArrowStates.Stuck;
                case TowerFall.Arrow.ArrowStates.LayingOnGround: return ArrowStates.LayingOnGround;
                case TowerFall.Arrow.ArrowStates.Buried: return ArrowStates.Buried;

            }
            throw new NotImplementedException($"Can't create a ArrowStates for unkwon value {arrowStates}");
        }

        public static TowerFall.Arrow.ArrowStates ToTFModel(this ArrowStates playerStates)
        {
            switch (playerStates)
            {
                case ArrowStates.Shooting: return TowerFall.Arrow.ArrowStates.Shooting;
                case ArrowStates.Drilling: return TowerFall.Arrow.ArrowStates.Drilling;
                case ArrowStates.Gravity: return TowerFall.Arrow.ArrowStates.Gravity;
                case ArrowStates.Falling: return TowerFall.Arrow.ArrowStates.Falling;
                case ArrowStates.Stuck: return TowerFall.Arrow.ArrowStates.Stuck;
                case ArrowStates.LayingOnGround: return TowerFall.Arrow.ArrowStates.LayingOnGround;
                case ArrowStates.Buried: return TowerFall.Arrow.ArrowStates.Buried;
            }
            throw new NotImplementedException($"Can't create a Twoerfall ArrowStates for unkwon value {playerStates}");
        }

    }
}
