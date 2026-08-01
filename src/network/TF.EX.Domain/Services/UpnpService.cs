namespace TF.EX.Domain.Services
{
    //[Obsolete("useless right now, might be usefull one day or when server are down (Use OpenNat lib)", true)]
    //public class UpnpService : IUpnpService
    //{
    //    private Mapping _mappingTcp;
    //    private Mapping _mappingUdp;
    //    private NatDevice _device;

    //    private const string TCP_DESCRIPTION = "Towerfall netplay Tcp";
    //    private const string UDP_DESCRIPTION = "Towerfall netplay Udp";

    //    private IPAddress iPAddress = null;
    //    private int _portUdp;
    //    private int _portTcp;
    //    private bool _isEnabled = false;

    //    public UpnpService()
    //    {
    //        _portUdp = GetAvailablePort();
    //        _portTcp = GetAvailablePort();
    //        _mappingTcp = new Mapping(Protocol.Tcp, _portTcp, Constants.PORT, TCP_DESCRIPTION);
    //        _mappingUdp = new Mapping(Protocol.Udp, _portUdp, Constants.PORT, UDP_DESCRIPTION);
    //    }

    //    public void DiscoverAndMap()
    //    {
    //        Task.Run(async () =>
    //        {
    //            EnsureAvailablePortMapping();

    //            await EnsureDevice();
    //            await _device.CreatePortMapAsync(_mappingUdp);
    //            await _device.CreatePortMapAsync(_mappingTcp);
    //            iPAddress = await _device.GetExternalIPAsync();
    //            _isEnabled = true;
    //        }).GetAwaiter().GetResult();
    //    }

    //    private void EnsureAvailablePortMapping()
    //    {
    //        Task.Run(async () =>
    //        {
    //            await EnsureDevice();
    //            var mappings = await _device.GetAllMappingsAsync();
    //            var mapping = mappings.FirstOrDefault(m => m.Description == TCP_DESCRIPTION);
    //            if (mapping != null)
    //            {
    //                await _device.DeletePortMapAsync(mapping);
    //            }
    //            mapping = mappings.FirstOrDefault(m => m.Description == UDP_DESCRIPTION);
    //            if (mapping != null)
    //            {
    //                await _device.DeletePortMapAsync(mapping);
    //            }
    //        }).GetAwaiter().GetResult();
    //    }

    //    public void DropMap()
    //    {
    //        Task.Run(async () =>
    //        {
    //            await EnsureDevice();

    //            await _device.DeletePortMapAsync(_mappingTcp);
    //            await _device.DeletePortMapAsync(_mappingUdp);
    //            iPAddress = null;
    //        }).GetAwaiter().GetResult();
    //    }

    //    private int GetAvailablePort()
    //    {
    //        TcpListener l = new TcpListener(IPAddress.Loopback, 0);
    //        l.Start();
    //        int port = ((IPEndPoint)l.LocalEndpoint).Port;
    //        l.Stop();
    //        return port;
    //    }

    //    public IPAddress GetNatIPAddress()
    //    {
    //        if (iPAddress != null)
    //        {
    //            return iPAddress;
    //        }

    //        Task.Run(async () =>
    //        {
    //            await EnsureDevice();
    //            iPAddress = await _device.GetExternalIPAsync();
    //        });

    //        return iPAddress;
    //    }

    //    private async Task EnsureDevice()
    //    {
    //        if (_device == null)
    //        {
    //            NatDiscoverer discoverer = new NatDiscoverer();
    //            _device = await discoverer.DiscoverDeviceAsync();
    //        }
    //    }

    //    public int GetPortTcp()
    //    {
    //        return _portTcp;
    //    }

    //    public int GetPortUdp()
    //    {
    //        return _portUdp;
    //    }

    //    public bool IsEnabled()
    //    {
    //        return _isEnabled;
    //    }
    //}
}
