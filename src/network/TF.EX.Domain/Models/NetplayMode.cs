namespace TF.EX.Domain.Models
{
    public enum NetplayMode : int
    {
        Uninitialized,
        Local,
        Test,
        Replay,
        Server,
        Spectator,
        Unknown
    }
}
