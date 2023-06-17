using System.Collections.Generic;

namespace TF.EX.Domain.Models.State
{
    public class Scheduler
    {
        public List<string> SchedulerActions { get; set; }
        public List<float> SchedulerCounters { get; set; }
        public List<int> SchedulerStartCounters { get; set; }
    }
}
