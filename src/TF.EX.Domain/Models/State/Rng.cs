using MessagePack;
using TF.EX.Domain.Randomness;

namespace TF.EX.Domain.Models.State
{
    [MessagePackObject]
    public class Rng
    {
        [Key(0)]
        public int Seed { get; set; } = -1;

        [Key(1)]
        public ulong S0 { get; set; }
        [Key(2)]
        public ulong S1 { get; set; }
        [Key(3)]
        public ulong S2 { get; set; }
        [Key(4)]
        public ulong S3 { get; set; }

        public DeterministicRandom.State ToState() => new DeterministicRandom.State(S0, S1, S2, S3);

        public void SetState(DeterministicRandom.State state)
        {
            S0 = state.S0;
            S1 = state.S1;
            S2 = state.S2;
            S3 = state.S3;
        }

        public string Debug() => $"SEED: {Seed}, STATE: {S0:x16}/{S1:x16}/{S2:x16}/{S3:x16}";
    }
}
