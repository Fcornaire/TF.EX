using TF.EX.Domain;

namespace TF.EX.Core.Api
{
    public class TfExApi
    {
        public void SetReplaySkinSeats(int[] seats, string[] skinArcherIds)
        {
            ServiceCollections.ResolveSkinOverlayService().SetReplaySkinSeats(seats, skinArcherIds);
        }

        public void ClearReplaySkinSeats()
        {
            ServiceCollections.ResolveSkinOverlayService().ClearReplaySkinSeats();
        }
    }
}
