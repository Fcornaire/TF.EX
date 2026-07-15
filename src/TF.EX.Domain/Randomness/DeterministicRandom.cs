using System;

namespace TF.EX.Domain.Randomness
{
    /// <summary>
    /// A deterministic Random replacement for rollback using xoshiro256** algorithm
    /// </summary>
    /// <remarks>
    /// Reference: https://prng.di.unimi.it/
    /// </remarks>
    public sealed class DeterministicRandom : Random
    {
        private ulong _s0;
        private ulong _s1;
        private ulong _s2;
        private ulong _s3;

        public readonly struct State
        {
            public readonly ulong S0;
            public readonly ulong S1;
            public readonly ulong S2;
            public readonly ulong S3;

            public State(ulong s0, ulong s1, ulong s2, ulong s3)
            {
                S0 = s0;
                S1 = s1;
                S2 = s2;
                S3 = s3;
            }
        }

        public DeterministicRandom(int seed)
        {
            Seed(unchecked((ulong)seed));
        }

        public DeterministicRandom(long seed)
        {
            Seed(unchecked((ulong)seed));
        }

        public DeterministicRandom(State state)
        {
            Restore(state);
        }

        public void Seed(ulong seed)
        {
            _s0 = SplitMix64(ref seed);
            _s1 = SplitMix64(ref seed);
            _s2 = SplitMix64(ref seed);
            _s3 = SplitMix64(ref seed);

            if ((_s0 | _s1 | _s2 | _s3) == 0)
            {
                _s0 = 0x9E3779B97F4A7C15UL;
            }
        }

        public State Snapshot() => new State(_s0, _s1, _s2, _s3);

        public void Restore(State state)
        {
            _s0 = state.S0;
            _s1 = state.S1;
            _s2 = state.S2;
            _s3 = state.S3;
        }

        private static ulong SplitMix64(ref ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            ulong z = x;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        private static ulong Rotl(ulong x, int k) => (x << k) | (x >> (64 - k));

        public ulong NextULong()
        {
            ulong result = Rotl(_s1 * 5UL, 7) * 9UL;

            ulong t = _s1 << 17;
            _s2 ^= _s0;
            _s3 ^= _s1;
            _s1 ^= _s2;
            _s0 ^= _s3;
            _s2 ^= t;
            _s3 = Rotl(_s3, 45);

            return result;
        }


        protected override double Sample()
        {
            return (NextULong() >> 11) * (1.0 / 9007199254740992.0); // 1 / 2^53
        }

        public override double NextDouble() => Sample();

        public override int Next()
        {
            return Next(0, int.MaxValue);
        }

        public override int Next(int maxValue)
        {
            if (maxValue < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxValue));
            }

            return (int)(Sample() * maxValue);
        }

        public override int Next(int minValue, int maxValue)
        {
            if (minValue > maxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(minValue));
            }

            long range = (long)maxValue - minValue;
            return (int)((long)(Sample() * range) + minValue);
        }

        public override void NextBytes(byte[] buffer)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            int i = 0;
            while (i + 8 <= buffer.Length)
            {
                ulong v = NextULong();
                buffer[i++] = (byte)v;
                buffer[i++] = (byte)(v >> 8);
                buffer[i++] = (byte)(v >> 16);
                buffer[i++] = (byte)(v >> 24);
                buffer[i++] = (byte)(v >> 32);
                buffer[i++] = (byte)(v >> 40);
                buffer[i++] = (byte)(v >> 48);
                buffer[i++] = (byte)(v >> 56);
            }

            if (i < buffer.Length)
            {
                ulong v = NextULong();
                while (i < buffer.Length)
                {
                    buffer[i++] = (byte)v;
                    v >>= 8;
                }
            }
        }
    }
}
