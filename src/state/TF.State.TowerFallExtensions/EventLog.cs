namespace TF.State.TowerFallExtensions
{
    public static class EventLogExtensions
    {
        public static TF.State.Domain.Models.EventLog.EventLog ToModel(this List<TowerFall.EventLog> events)
        {
            var result = new TF.State.Domain.Models.EventLog.EventLog();

            var order = 0;
            foreach (var evt in events)
            {
                switch (evt)
                {
                    case TowerFall.GainPointEvent gain:
                        result.GainPoints.Add(new TF.State.Domain.Models.EventLog.GainPoint
                        {
                            ScoreIndex = gain.ScoreIndex,
                            Order = order,
                        });
                        break;
                    case TowerFall.LosePointEvent lose:
                        result.LosePoints.Add(new TF.State.Domain.Models.EventLog.LosePoint
                        {
                            ScoreIndex = lose.ScoreIndex,
                            Order = order,
                        });
                        break;
                    case TowerFall.CrownChangeEvent crown:
                        result.CrownChanges.Add(new TF.State.Domain.Models.EventLog.CrownChange
                        {
                            PlayerWithCrown = crown.HasCrown.ToArray(),
                            Order = order,
                        });
                        break;
                    default:
                        throw new NotImplementedException();
                }

                order++;
            }

            return result;
        }

        public static List<TowerFall.EventLog> ToTFModel(this TF.State.Domain.Models.EventLog.EventLog eventLog)
        {
            var ordered = new List<(int Order, TowerFall.EventLog Event)>();

            foreach (var evt in eventLog.GainPoints)
            {
                ordered.Add((evt.Order, new TowerFall.GainPointEvent(evt.ScoreIndex)));
            }

            foreach (var evt in eventLog.LosePoints)
            {
                ordered.Add((evt.Order, new TowerFall.LosePointEvent(evt.ScoreIndex)));
            }

            foreach (var evt in eventLog.CrownChanges)
            {
                ordered.Add((evt.Order, new TowerFall.CrownChangeEvent(evt.PlayerWithCrown.ToArray())));
            }

            return ordered.OrderBy(entry => entry.Order).Select(entry => entry.Event).ToList();
        }
    }
}
