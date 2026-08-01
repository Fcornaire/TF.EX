using System.IO;
using System.Reflection;
using System.Text;
using Xunit;

namespace TF.EX.Utils
{
    public class ApiSurfaceTests
    {
        [Fact]
        public void ExStateApiSliceMatchesProvider()
            => AssertSubsetOf(typeof(global::TF.EX.Domain.Interop.ITfStateApi),
                              typeof(global::TF.State.Core.Api.TfStateApi));

        [Fact]
        public void ReplayStateApiSliceMatchesProvider()
            => AssertSubsetOf(typeof(global::TF.Replay.Domain.Interop.ITfStateApi),
                              typeof(global::TF.State.Core.Api.TfStateApi));

        [Fact]
        public void ReplayApiProviderImplementsItsOwnPort()
            => AssertSubsetOf(typeof(global::TF.Replay.Domain.Ports.ITfReplayApi),
                              typeof(global::TF.Replay.Domain.Api.TfReplayApi));

        [Fact]
        public void ExReplayApiSliceMatchesProvider()
            => AssertSubsetOf(typeof(global::TF.EX.Domain.Interop.ITfReplayApi),
                              typeof(global::TF.Replay.Domain.Api.TfReplayApi));

        private static void AssertSubsetOf(Type consumerInterface, Type provider)
        {
            var providerMethods = provider
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .ToLookup(m => m.Name);

            var missing = new List<string>();

            foreach (var wanted in consumerInterface.GetMethods())
            {
                var candidates = providerMethods[wanted.Name].ToList();

                if (candidates.Count == 0)
                {
                    missing.Add(wanted.Name + "(...)");
                    continue;
                }

                bool matched;

                try
                {
                    var wantedParams = wanted.GetParameters().Select(x => x.ParameterType).ToArray();

                    matched = candidates.Any(candidate =>
                        candidate.ReturnType == wanted.ReturnType &&
                        candidate.GetParameters().Select(x => x.ParameterType).SequenceEqual(wantedParams));
                }
                catch (FileNotFoundException)
                {
                    matched = candidates.Any(candidate =>
                        SafeParameterCount(candidate) == SafeParameterCount(wanted));
                }

                if (!matched)
                {
                    missing.Add(SafeDescribe(wanted));
                }
            }

            Assert.True(missing.Count == 0, Message(consumerInterface, provider, missing));
        }

        private static int SafeParameterCount(MethodBase m)
        {
            try
            {
                return m.GetParameters().Length;
            }
            catch (FileNotFoundException)
            {
                return -1;
            }
        }

        private static string SafeDescribe(MethodInfo m)
        {
            try
            {
                return Describe(m);
            }
            catch (FileNotFoundException)
            {
                return m.Name + "(<unresolvable signature>)";
            }
        }

        private static string Describe(MethodInfo m)
            => $"{m.ReturnType.Name} {m.Name}({string.Join(", ", m.GetParameters().Select(p => p.ParameterType.Name))})";

        private static string Message(Type consumer, Type provider, List<string> missing)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{consumer.FullName} declares members that {provider.FullName} does not provide.");
            sb.AppendLine("GetApi<T>() would throw at mod load. Add them to the provider or remove them here:");
            foreach (var m in missing)
            {
                sb.AppendLine("  " + m);
            }

            return sb.ToString();
        }
    }
}
