namespace TF.EX.TowerFallExtensions
{
    public static class EventLogExtensions
    {
        public static TF.EX.Domain.Models.State.EventLog.EventLog ToModel(this List<TowerFall.EventLog> events)
        {
            var result = new TF.EX.Domain.Models.State.EventLog.EventLog();

            foreach (var evt in events)
            {
                switch (evt)
                {
                    case TowerFall.GainPointEvent gain:
                        result.GainPoints.Add(new TF.EX.Domain.Models.State.EventLog.GainPoint
                        {
                            ScoreIndex = gain.ScoreIndex,
                        });
                        break;
                    case TowerFall.LosePointEvent lose:
                        result.LosePoints.Add(new TF.EX.Domain.Models.State.EventLog.LosePoint
                        {
                            ScoreIndex = lose.ScoreIndex,
                        });
                        break;
                    case TowerFall.CrownChangeEvent crown:
                        result.CrownChanges.Add(new TF.EX.Domain.Models.State.EventLog.CrownChange
                        {
                            PlayerWithCrown = crown.HasCrown.ToArray(),
                        });
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }

            return result;
        }

        public static List<TowerFall.EventLog> ToTFModel(this TF.EX.Domain.Models.State.EventLog.EventLog eventLog)
        {
            var result = new List<TowerFall.EventLog>();

            foreach (var evt in eventLog.GainPoints)
            {
                result.Add(new TowerFall.GainPointEvent(evt.ScoreIndex));
            }

            foreach (var evt in eventLog.LosePoints)
            {
                result.Add(new TowerFall.LosePointEvent(evt.ScoreIndex));
            }

            foreach (var evt in eventLog.CrownChanges)
            {
                result.Add(new TowerFall.CrownChangeEvent(evt.PlayerWithCrown.ToArray()));
            }

            return result;
        }
    }
}
