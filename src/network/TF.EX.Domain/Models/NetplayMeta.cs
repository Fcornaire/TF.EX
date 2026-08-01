namespace TF.EX.Domain.Models
{
    public class NetplayMeta
    {
        public int InputDelay { get; set; }
        public string Name { get; set; }

        public NetplayMeta()
        {
            Name = "PLAYER";
            InputDelay = 2;
        }
    }
}
