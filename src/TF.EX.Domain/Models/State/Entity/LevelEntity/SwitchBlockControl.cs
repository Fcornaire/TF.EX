using MessagePack;

namespace TF.EX.Domain.Models.State.Entity.LevelEntity
{
    [MessagePackObject]
    public class SwitchBlockControl
    {
        [Key(0)]
        public float Timer { get; set; }
    }
}
