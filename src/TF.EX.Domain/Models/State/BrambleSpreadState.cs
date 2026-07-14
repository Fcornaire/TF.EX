using MessagePack;

namespace TF.EX.Domain.Models.State
{
    /// <summary>
    /// Replacement for the <c>Brambles.CreateBrambles</c> iterator/coroutine.
    [MessagePackObject]
    public class BrambleSpreadState
    {
        [Key(0)]
        public int Id { get; set; }
        [Key(1)]
        public Vector2f At { get; set; }
        [Key(2)]
        public int OwnerIndex { get; set; }
        [Key(3)]
        public int Spread { get; set; }
        [Key(4)]
        public bool ShortTime { get; set; }
        [Key(5)]
        public List<Vector2f> ToCheck { get; set; } = new List<Vector2f>();
        [Key(6)]
        public List<int> Spreads { get; set; } = new List<int>();
        [Key(7)]
        public List<Vector2f> Made { get; set; } = new List<Vector2f>();
        [Key(8)]
        public int Waited { get; set; }
        [Key(9)]
        public bool SoundPlayed { get; set; }
        [Key(10)]
        public float TimeWaited { get; set; }
        [Key(11)]
        public bool IsComplete { get; set; }

        public BrambleSpreadState Clone()
        {
            return new BrambleSpreadState
            {
                Id = Id,
                At = At,
                OwnerIndex = OwnerIndex,
                Spread = Spread,
                ShortTime = ShortTime,
                ToCheck = new List<Vector2f>(ToCheck),
                Spreads = new List<int>(Spreads),
                Made = new List<Vector2f>(Made),
                Waited = Waited,
                SoundPlayed = SoundPlayed,
                TimeWaited = TimeWaited,
                IsComplete = IsComplete,
            };
        }
    }
}
