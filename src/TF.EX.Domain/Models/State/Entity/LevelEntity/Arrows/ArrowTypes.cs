using System;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Arrows
{
    public enum ArrowTypes : int
    {
        Normal,
        Bomb,
        SuperBomb,
        Laser,
        Bramble,
        Drill,
        Bolt,
        Toy,
        Feather,
        Trigger,
        Prism
    }

    public static class ArrowTypesExtensions
    {
        public static ArrowTypes ToModel(this TowerFall.ArrowTypes arrowTypes)
        {
            switch (arrowTypes)
            {
                case TowerFall.ArrowTypes.Normal: return ArrowTypes.Normal;
                case TowerFall.ArrowTypes.Bomb: return ArrowTypes.Bomb;
                case TowerFall.ArrowTypes.SuperBomb: return ArrowTypes.SuperBomb;
                case TowerFall.ArrowTypes.Laser: return ArrowTypes.Laser;
                case TowerFall.ArrowTypes.Bramble: return ArrowTypes.Bramble;
                case TowerFall.ArrowTypes.Drill: return ArrowTypes.Drill;
                case TowerFall.ArrowTypes.Bolt: return ArrowTypes.Bolt;
                case TowerFall.ArrowTypes.Toy: return ArrowTypes.Toy;
                case TowerFall.ArrowTypes.Feather: return ArrowTypes.Feather;
                case TowerFall.ArrowTypes.Trigger: return ArrowTypes.Trigger;
                case TowerFall.ArrowTypes.Prism: return ArrowTypes.Prism;
            }
            throw new NotImplementedException($"Can't create a ArrowTypes for unkwon value {arrowTypes}");
        }

        public static TowerFall.ArrowTypes ToTFModel(this ArrowTypes arrowTypes)
        {
            switch (arrowTypes)
            {
                case ArrowTypes.Normal: return TowerFall.ArrowTypes.Normal;
                case ArrowTypes.Bomb: return TowerFall.ArrowTypes.Bomb;
                case ArrowTypes.SuperBomb: return TowerFall.ArrowTypes.SuperBomb;
                case ArrowTypes.Laser: return TowerFall.ArrowTypes.Laser;
                case ArrowTypes.Bramble: return TowerFall.ArrowTypes.Bramble;
                case ArrowTypes.Drill: return TowerFall.ArrowTypes.Drill;
                case ArrowTypes.Bolt: return TowerFall.ArrowTypes.Bolt;
                case ArrowTypes.Toy: return TowerFall.ArrowTypes.Toy;
                case ArrowTypes.Feather: return TowerFall.ArrowTypes.Feather;
                case ArrowTypes.Trigger: return TowerFall.ArrowTypes.Trigger;
                case ArrowTypes.Prism: return TowerFall.ArrowTypes.Prism;
            }
            throw new NotImplementedException($"Can't create a Twoerfall ArrowTypes for unkwon value {arrowTypes}");
        }

    }
}
