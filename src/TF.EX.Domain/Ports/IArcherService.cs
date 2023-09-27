namespace TF.EX.Domain.Ports
{
    public interface IArcherService
    {
        public void Reset();
        public void AddArcher(int index, string archer_alt);
        public IEnumerable<(int, string)> GetArchers();
        public IEnumerable<(int, string)> GetFinalArchers();
    }
}
