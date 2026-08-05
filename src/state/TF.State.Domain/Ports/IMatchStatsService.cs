namespace TF.State.Domain.Ports
{
    public interface IMatchStatsService
    {
        void SaveSnapshot(int frame, TowerFall.MatchStats[] stats);
        void RestoreSnapshot(int frame, TowerFall.MatchStats[] stats);
        void Reset();
    }
}
