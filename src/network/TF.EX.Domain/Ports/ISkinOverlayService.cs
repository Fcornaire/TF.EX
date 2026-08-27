namespace TF.EX.Domain.Ports
{
    public interface ISkinOverlayService
    {
        TowerFall.ArcherData ResolveArcherSkinned(int seat, int characterIndex, int altIndex, TowerFall.ArcherData original);

        void SetReplaySkinSeats(int[] seats, string[] skinArcherIds);
        void ClearReplaySkinSeats();
        bool HasReplaySkins { get; }
    }
}
