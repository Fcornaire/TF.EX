namespace TF.EX.Domain.Models
{
    public class GGRSConfig
    {
        public int InputDelay { get; set; }
        public string Name { get; set; }

        public NetplayConfig Netplay { get; set; }

        public TestConfig Test { get; set; }

        public static GGRSConfig DefaultTest(int checkDistance)
        {
            return new GGRSConfig
            {
                InputDelay = 2,
                Name = "TEST",
                Netplay = new NetplayConfig
                {

                },
                Test = new TestConfig
                {
                    CheckDistance = checkDistance
                }
            };
        }

        internal GGRSConfig DefaultLocal(string addr, PlayerDraw draw)
        {
            return new GGRSConfig
            {
                InputDelay = 2,
                Name = "LOCAL",
                Netplay = new NetplayConfig
                {
                    Local = new NetplayLocalConfig
                    {
                        RemoteAddr = addr,
                        Port = 7000,
                        PlayerDraw = draw
                    }
                },
            };
        }

        internal GGRSConfig DefaultServer(string roomUrl)
        {
            return new GGRSConfig
            {
                InputDelay = 2,
                Name = "SERVER",
                Netplay = new NetplayConfig
                {
                    Server = new NetplayServerConfig
                    {
                        RoomUrl = roomUrl
                    }
                },
            };
        }
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
