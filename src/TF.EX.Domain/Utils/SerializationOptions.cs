using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using TF.EX.Domain.Models;

namespace TF.EX.Domain.Utils
{
    public static class SerializationOptions
    {
        private static IFormatterResolver DefaultOptionsWithIgnore = MessagePack.Resolvers.CompositeResolver.Create(
                new IMessagePackFormatter[]
                {
                    new IgnoreFormatter<List<Record>>()
                },
                new IFormatterResolver[]
                {
                    StandardResolver.Instance
                }
            );

        public static MessagePackSerializerOptions GetDefaultOptionWithIgnore() =>
            MessagePackSerializerOptions.Standard
                .WithResolver(DefaultOptionsWithIgnore)
                .WithCompression(MessagePackCompression.Lz4BlockArray);
    }
}
