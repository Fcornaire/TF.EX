using System.Net;

namespace TF.EX.Domain.Ports
{
    public interface IUpnpService
    {
        void DiscoverAndMap();
        void DropMap();
        IPAddress GetNatIPAddress();

        int GetPortTcp();
        int GetPortUdp();

        bool IsEnabled();
    }
}
