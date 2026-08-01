using MessagePack;

namespace TF.State.Domain.Models.Entity.LevelEntity.Player
{
    [MessagePackObject]
    public class Scheduler
    {
        [Key(0)]
        public List<string> SchedulerActions { get; set; }

        [Key(1)]
        public List<float> SchedulerCounters { get; set; }

        [Key(2)]
        public List<int> SchedulerStartCounters { get; set; }
    }
}
