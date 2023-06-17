namespace TF.EX.Domain.Models
{
    public class GGRSConfig
    {
        public int InputDelay { get; set; }
        public string Name { get; set; }
        public NetplayConfig Netplay { get; set; }

        public TestConfig Test { get; set; }
    }

    public class NetplayConfig
    {
        public NetplayLocalConfig Local { get; set; }
        public NetplayServerConfig Server { get; set; }
    }

    public class NetplayLocalConfig
    {
        public string RemoteAddr { get; set; }

        public ushort Port { get; set; }
        public PlayerDraw PlayerDraw { get; set; }
    }

    public class NetplayServerConfig
    {
        public string RoomUrl { get; set; }
    }

    public class TestConfig
    {
        public int CheckDistance { get; set; }
    }
}
