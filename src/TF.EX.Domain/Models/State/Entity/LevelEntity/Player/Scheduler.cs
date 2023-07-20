namespace TF.EX.Domain.Models.State.Entity.LevelEntity.Player
{
    public class Scheduler
    {
        public List<string> SchedulerActions { get; set; }
        public List<float> SchedulerCounters { get; set; }
        public List<int> SchedulerStartCounters { get; set; }
    }
}
