using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity
{
    [MessagePackObject]
    public class SwitchBlockControl
    {
        [Key(0)]
        public float Timer { get; set; }
    }
}
