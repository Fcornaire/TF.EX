using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using TF.Replay.Domain.Models;

namespace TF.Replay.Domain
{
    public static class ReplaySerialization
    {
        public static readonly MessagePackSerializerOptions Default =
            MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        public static readonly MessagePackSerializerOptions WithoutRecords =
            MessagePackSerializerOptions.Standard
                .WithCompression(MessagePackCompression.Lz4BlockArray)
                .WithResolver(CompositeResolver.Create(
                    new IMessagePackFormatter[] { new IgnoreFormatter<List<Record>>() },
                    new IFormatterResolver[] { StandardResolver.Instance }));
    }
}
