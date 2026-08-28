namespace TF.EX.Domain.Models
{
    public class GGRSConfig
    {
        public int InputDelay { get; set; }
        public int Fps { get; set; } = Constants.NETPLAY_FPS;
        public string Name { get; set; }

        public NetplayConfig Netplay { get; set; }

        public TestConfig Test { get; set; }

        public static GGRSConfig DefaultTest(int checkDistance, int numPlayers, int fps = Constants.NETPLAY_FPS)
        {
            return new GGRSConfig
            {
                InputDelay = 2,
                Fps = fps,
                Name = "TEST",
                Netplay = new NetplayConfig
                {
                    NumPlayers = numPlayers
                },
                Test = new TestConfig
                {
                    CheckDistance = checkDistance
                }
            };
        }

        internal GGRSConfig DefaultLocal(string addr, ushort localPort, PlayerDraw draw)
        {
            return new GGRSConfig
            {
                InputDelay = 2,
                Name = "LOCAL",
                Netplay = new NetplayConfig
                {
                    LocalConf = new NetplayLocalConfig
                    {
                        RemoteAddr = addr,
                        Port = localPort,
                        PlayerDraw = draw
                    }
                },
            };
        }

        internal GGRSConfig DefaultServer(string roomUrl, bool isHost)
        {
            return new GGRSConfig
            {
                InputDelay = 2,
                Name = "SERVER",
                Netplay = new NetplayConfig
                {
                    ServerConf = new NetplayServerConfig
                    {
                        RoomUrl = roomUrl,
                        IsHost = isHost
                    }
                },
            };
        }
    }

    public class NetplayConfig
    {
        public int NumPlayers { get; set; }
        public ICollection<string> Spectators { get; set; } = new List<string>();
        public ICollection<string> Players { get; set; } = new List<string>();
        public string LocalPeerId { get; set; } = "";
        public NetplayLocalConfig LocalConf { get; set; }
        public NetplayServerConfig ServerConf { get; set; }
        public NetplaySpectatorConfig SpectatorConf { get; set; }
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

        public bool IsHost { get; set; }
    }

    public class TestConfig
    {
        public int CheckDistance { get; set; }
    }

    public class NetplaySpectatorConfig
    {
        public string RoomUrl { get; set; }

        public string ToSpectate { get; set; }
    }
}
