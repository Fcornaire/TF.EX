using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity
{
    [MessagePackObject]
    public class QuestSpawnPortal
    {
        [Key(0)]
        public double ActualDepth { get; set; }

        [Key(1)]
        public Vector2f Position { get; set; }

        [Key(2)]
        public bool Appeared { get; set; }

        [Key(3)]
        public List<string> ToSpawn { get; set; } = new List<string>();

        [Key(4)]
        public int LastFacing { get; set; }

        [Key(5)]
        public bool AutoDisappear { get; set; }

        [Key(6)]
        public float AddCounter { get; set; }

        [Key(7)]
        public Sprite<int> Sprite { get; set; }

        [Key(8)]
        public float AppearCounter { get; set; }
    }
}
