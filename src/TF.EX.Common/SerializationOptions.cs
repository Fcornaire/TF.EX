using MessagePack;

namespace TF.EX.Common
{
    public static class SerializationOptions
    {
        public static MessagePackSerializerOptions GetDefaultOptionWithCompression() =>
           MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        public static MessagePackSerializerOptions GetContractlessOptions() =>
           MessagePack.Resolvers.ContractlessStandardResolver.Options;
    }
}
